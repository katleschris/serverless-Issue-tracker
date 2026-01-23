using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IssueTracker.Core.Interfaces;
using IssueTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace IssueTracker.Infrastructure.DynamoDb;

// This implements the IIssueRepository interface using DynamoDB
// IMPORTANT: This is the ONLY place that knows about DynamoDB!
public class DynamoDbIssueRepository : IIssueRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbIssueRepository> _logger;

    public DynamoDbIssueRepository(
        IAmazonDynamoDB dynamoDb,
        string tableName,
        ILogger<DynamoDbIssueRepository> logger)
    {
        _dynamoDb = dynamoDb;
        _tableName = tableName;
        _logger = logger;
    }

    // CREATE: Save new issue to DynamoDB
    public async Task<Issue> CreateAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        // Convert Issue object to DynamoDB format
        var item = IssueToItem(issue);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            // This prevents overwriting if ID already exists
            ConditionExpression = "attribute_not_exists(PK)"
        };

        try
        {
            await _dynamoDb.PutItemAsync(request, cancellationToken);
            _logger.LogInformation("Created issue {IssueId}", issue.Id);
            return issue;
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Issue {IssueId} already exists", issue.Id);
            throw new InvalidOperationException($"Issue with ID {issue.Id} already exists");
        }
    }

    // GET BY ID: Retrieve one issue
    public async Task<Issue?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                // PK = Primary Key, SK = Sort Key (DynamoDB terms)
                ["PK"] = new AttributeValue { S = $"ISSUE#{id}" },
                ["SK"] = new AttributeValue { S = "METADATA" }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, cancellationToken);

        // If no item found, return null
        if (!response.IsItemSet || response.Item.Count == 0)
        {
            _logger.LogInformation("Issue {IssueId} not found", id);
            return null;
        }

        // Convert DynamoDB format back to Issue object
        return ItemToIssue(response.Item);
    }

    // GET ALL: Retrieve all issues (optionally filter by status)
    public async Task<List<Issue>> GetAllAsync(
        IssueStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (status.HasValue)
        {
            // Use GSI (Global Secondary Index) to query by status
            return await GetByStatusAsync(status.Value, cancellationToken);
        }

        // Scan entire table (fine for small datasets, not for production scale)
        var request = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "ISSUE#" }
            }
        };

        var response = await _dynamoDb.ScanAsync(request, cancellationToken);
        var issues = response.Items.Select(ItemToIssue).ToList();

        _logger.LogInformation("Retrieved {Count} issues", issues.Count);
        return issues;
    }

    // UPDATE: Modify existing issue
    public async Task<Issue?> UpdateAsync(string id, Issue issue, CancellationToken cancellationToken = default)
    {
        // Check if exists first
        var exists = await ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            _logger.LogWarning("Cannot update non-existent issue {IssueId}", id);
            return null;
        }

        // Save updated issue
        var item = IssueToItem(issue);
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
        _logger.LogInformation("Updated issue {IssueId}", issue.Id);

        return issue;
    }

    // DELETE: Remove issue
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"ISSUE#{id}" },
                ["SK"] = new AttributeValue { S = "METADATA" }
            },
            ReturnValues = ReturnValue.ALL_OLD // Returns deleted item if successful
        };

        var response = await _dynamoDb.DeleteItemAsync(request, cancellationToken);
        var deleted = response.Attributes?.Count > 0;

        _logger.LogInformation("Delete issue {IssueId}: {Deleted}", id, deleted);
        return deleted;
    }

    // EXISTS: Check if issue exists
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var issue = await GetByIdAsync(id, cancellationToken);
        return issue != null;
    }

    // PRIVATE: Query by status using GSI
    private async Task<List<Issue>> GetByStatusAsync(
        IssueStatus status,
        CancellationToken cancellationToken)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1", // Our Global Secondary Index
            KeyConditionExpression = "GSI1PK = :gsi1pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gsi1pk"] = new AttributeValue { S = $"STATUS#{status}" }
            }
        };

        var response = await _dynamoDb.QueryAsync(request, cancellationToken);
        return response.Items.Select(ItemToIssue).ToList();
    }

    // PRIVATE: Convert Issue object to DynamoDB item format
    private Dictionary<string, AttributeValue> IssueToItem(Issue issue)
    {
        return new Dictionary<string, AttributeValue>
        {
            // Primary key structure
            ["PK"] = new AttributeValue { S = $"ISSUE#{issue.Id}" },
            ["SK"] = new AttributeValue { S = "METADATA" },
            
            // GSI keys for querying by status
            ["GSI1PK"] = new AttributeValue { S = $"STATUS#{issue.Status}" },
            ["GSI1SK"] = new AttributeValue { S = issue.CreatedAt.ToString("O") },
            
            // Actual data
            ["Id"] = new AttributeValue { S = issue.Id },
            ["Title"] = new AttributeValue { S = issue.Title },
            ["Description"] = new AttributeValue { S = issue.Description },
            ["Status"] = new AttributeValue { S = issue.Status.ToString() },
            ["Priority"] = new AttributeValue { S = issue.Priority.ToString() },
            ["CreatedAt"] = new AttributeValue { S = issue.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = issue.UpdatedAt.ToString("O") }
        };
    }

    // PRIVATE: Convert DynamoDB item back to Issue object
    private Issue ItemToIssue(Dictionary<string, AttributeValue> item)
    {
        return new Issue
        {
            Id = item["Id"].S,
            Title = item["Title"].S,
            Description = item["Description"].S,
            Status = Enum.Parse<IssueStatus>(item["Status"].S),
            Priority = Enum.Parse<IssuePriority>(item["Priority"].S),
            CreatedAt = DateTime.Parse(item["CreatedAt"].S),
            UpdatedAt = DateTime.Parse(item["UpdatedAt"].S)
        };
    }
}