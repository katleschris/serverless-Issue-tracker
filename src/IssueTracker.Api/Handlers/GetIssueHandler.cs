using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace IssueTracker.Api.Handlers;

// Handler for: GET /issues/{id}
public class GetIssueHandler
{
    private readonly IssueService _issueService;
    private readonly JsonSerializerOptions _jsonOptions;

    public GetIssueHandler() : this(Startup.ServiceProvider) { }

    public GetIssueHandler(IServiceProvider serviceProvider)
    {
        _issueService = serviceProvider.GetRequiredService<IssueService>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"GetIssue - RequestId: {context.AwsRequestId}");

        try
        {
            // Get ID from URL path parameters
            if (!request.PathParameters.TryGetValue("id", out var issueId))
            {
                return CreateResponse(400, ApiResponse<Issue>.Fail(
                    "Issue ID is required",
                    "MISSING_ID"));
            }

            var result = await _issueService.GetIssueAsync(issueId);

            // 200 = OK, 404 = Not Found, 400 = Bad Request
            return CreateResponse(
                result.Success ? 200 : (result.Error?.Code == "NOT_FOUND" ? 404 : 400),
                result);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex}");
            return CreateResponse(500, ApiResponse<Issue>.Fail(
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
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key",
                ["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS"
            }
        };
    }
}