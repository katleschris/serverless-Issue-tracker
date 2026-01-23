using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace IssueTracker.Api.Handlers;

// ============================================
// LIST ISSUES HANDLER: GET /issues
// ============================================
public class ListIssuesHandler
{
    private readonly IssueService _issueService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ListIssuesHandler() : this(Startup.ServiceProvider) { }

    public ListIssuesHandler(IServiceProvider serviceProvider)
    {
        _issueService = serviceProvider.GetRequiredService<IssueService>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"ListIssues - RequestId: {context.RequestId}");

        try
        {
            // Check for optional ?status= query parameter
            IssueStatus? statusFilter = null;
            if (request.QueryStringParameters?.TryGetValue("status", out var statusParam) == true)
            {
                if (Enum.TryParse<IssueStatus>(statusParam, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }
                else
                {
                    return CreateResponse(400, ApiResponse<List<Issue>>.Fail(
                        "Invalid status value. Must be: Open, InProgress, or Done",
                        "INVALID_STATUS"));
                }
            }

            var result = await _issueService.GetAllIssuesAsync(statusFilter);
            return CreateResponse(200, result);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex}");
            return CreateResponse(500, ApiResponse<List<Issue>>.Fail(
                "Internal server error",
                "INTERNAL_ERROR"));
        }
    }

    private APIGatewayProxyResponse CreateResponse<T>(int statusCode, ApiResponse<T> body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body, _jsonOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*"
            }
        };
    }
}
