using FluentValidation;
using IssueTracker.Core.Interfaces;
using IssueTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace IssueTracker.Core.Services;

// This is where the BUSINESS LOGIC lives
// Think of this as the "brain" - it validates, coordinates, makes decisions
public class IssueService
{
    private readonly IIssueRepository _repository;
    private readonly IValidator<CreateIssueRequest> _createValidator;
    private readonly IValidator<UpdateIssueRequest> _updateValidator;
    private readonly ILogger<IssueService> _logger;

    // Constructor - we get these injected automatically
    public IssueService(
        IIssueRepository repository,
        IValidator<CreateIssueRequest> createValidator,
        IValidator<UpdateIssueRequest> updateValidator,
        ILogger<IssueService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    // CREATE: Make a new issue
    public async Task<ApiResponse<Issue>> CreateIssueAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate the request
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed: {Errors}", errors);
            return ApiResponse<Issue>.Fail(errors, "VALIDATION_ERROR");
        }

        // Step 2: Create the Issue object
        var now = DateTime.UtcNow;
        var issue = new Issue
        {
            Id = Guid.NewGuid().ToString(), // Generate unique ID
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = IssueStatus.Open, // New issues always start as Open
            CreatedAt = now,
            UpdatedAt = now
        };

        // Step 3: Save to database
        try
        {
            var created = await _repository.CreateAsync(issue, cancellationToken);
            _logger.LogInformation("Created issue {IssueId}", created.Id);
            return ApiResponse<Issue>.Ok(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create issue");
            return ApiResponse<Issue>.Fail("Failed to create issue", "CREATE_ERROR");
        }
    }

    // GET: Retrieve one issue by ID
    public async Task<ApiResponse<Issue>> GetIssueAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Validate ID
        if (string.IsNullOrWhiteSpace(id))
        {
            return ApiResponse<Issue>.Fail("Issue ID is required", "INVALID_ID");
        }

        try
        {
            var issue = await _repository.GetByIdAsync(id, cancellationToken);
            
            if (issue == null)
            {
                _logger.LogInformation("Issue {IssueId} not found", id);
                return ApiResponse<Issue>.Fail($"Issue {id} not found", "NOT_FOUND");
            }

            return ApiResponse<Issue>.Ok(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get issue {IssueId}", id);
            return ApiResponse<Issue>.Fail("Failed to retrieve issue", "GET_ERROR");
        }
    }

    // LIST: Get all issues (optionally filter by status)
    public async Task<ApiResponse<List<Issue>>> GetAllIssuesAsync(
        IssueStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = await _repository.GetAllAsync(status, cancellationToken);
            _logger.LogInformation("Retrieved {Count} issues", issues.Count);
            return ApiResponse<List<Issue>>.Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get issues");
            return ApiResponse<List<Issue>>.Fail("Failed to retrieve issues", "LIST_ERROR");
        }
    }

    // UPDATE: Modify an existing issue
    public async Task<ApiResponse<Issue>> UpdateIssueAsync(
        string id,
        UpdateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate ID
        if (string.IsNullOrWhiteSpace(id))
        {
            return ApiResponse<Issue>.Fail("Issue ID is required", "INVALID_ID");
        }

        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed for update: {Errors}", errors);
            return ApiResponse<Issue>.Fail(errors, "VALIDATION_ERROR");
        }

        try
        {
            // Get existing issue
            var existing = await _repository.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return ApiResponse<Issue>.Fail($"Issue {id} not found", "NOT_FOUND");
            }

            // Apply updates (only update fields that were provided)
            existing.Title = request.Title ?? existing.Title;
            existing.Description = request.Description ?? existing.Description;
            existing.Status = request.Status ?? existing.Status;
            existing.Priority = request.Priority ?? existing.Priority;
            existing.UpdatedAt = DateTime.UtcNow;

            // Save changes
            var updated = await _repository.UpdateAsync(id, existing, cancellationToken);
            _logger.LogInformation("Updated issue {IssueId}", id);
            return ApiResponse<Issue>.Ok(updated!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update issue {IssueId}", id);
            return ApiResponse<Issue>.Fail("Failed to update issue", "UPDATE_ERROR");
        }
    }

    // DELETE: Remove an issue
    public async Task<ApiResponse<bool>> DeleteIssueAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ApiResponse<bool>.Fail("Issue ID is required", "INVALID_ID");
        }

        try
        {
            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            
            if (!deleted)
            {
                return ApiResponse<bool>.Fail($"Issue {id} not found", "NOT_FOUND");
            }

            _logger.LogInformation("Deleted issue {IssueId}", id);
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete issue {IssueId}", id);
            return ApiResponse<bool>.Fail("Failed to delete issue", "DELETE_ERROR");
        }
    }
}