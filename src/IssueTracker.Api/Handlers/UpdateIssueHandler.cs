using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace IssueTracker.Api.Handlers;

// ============================================
// UPDATE ISSUE HANDLER: PUT /issues/{id}
// ============================================
public class UpdateIssueHandler
{
    private readonly IssueService _issueService;
    private readonly JsonSerializerOptions _jsonOptions;

    public UpdateIssueHandler() : this(Startup.ServiceProvider) { }

    public UpdateIssueHandler(IServiceProvider serviceProvider)
    {
        _issueService = serviceProvider.GetRequiredService<IssueService>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"UpdateIssue - RequestId: {context.AwsRequestId}");

        try
        {
            // Get ID from URL
            if (!request.PathParameters.TryGetValue("id", out var issueId))
            {
                return CreateResponse(400, ApiResponse<Issue>.Fail(
                    "Issue ID is required",
                    "MISSING_ID"));
            }

            // Parse JSON body
            var updateRequest = JsonSerializer.Deserialize<UpdateIssueRequest>(
                request.Body,
                _jsonOptions);

            if (updateRequest == null)
            {
                return CreateResponse(400, ApiResponse<Issue>.Fail(
                    "Invalid request body",
                    "INVALID_BODY"));
            }

            var result = await _issueService.UpdateIssueAsync(issueId, updateRequest);

            return CreateResponse(
                result.Success ? 200 : (result.Error?.Code == "NOT_FOUND" ? 404 : 400),
                result);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"JSON error: {ex.Message}");
            return CreateResponse(400, ApiResponse<Issue>.Fail(
                "Invalid JSON format",
                "INVALID_JSON"));
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
                ["Access-Control-Allow-Origin"] = "*"
            }
        };
    }
}
