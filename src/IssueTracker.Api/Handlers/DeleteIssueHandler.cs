using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace IssueTracker.Api.Handlers;
// ============================================
// DELETE ISSUE HANDLER: DELETE /issues/{id}
// ============================================
public class DeleteIssueHandler
{
    private readonly IssueService _issueService;
    private readonly JsonSerializerOptions _jsonOptions;

    public DeleteIssueHandler() : this(Startup.ServiceProvider) { }

    public DeleteIssueHandler(IServiceProvider serviceProvider)
    {
        _issueService = serviceProvider.GetRequiredService<IssueService>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"DeleteIssue - RequestId: {context.RequestId}");

        try
        {
            if (!request.PathParameters.TryGetValue("id", out var issueId))
            {
                return CreateResponse(400, ApiResponse<bool>.Fail(
                    "Issue ID is required",
                    "MISSING_ID"));
            }

            var result = await _issueService.DeleteIssueAsync(issueId);

            return CreateResponse(
                result.Success ? 200 : (result.Error?.Code == "NOT_FOUND" ? 404 : 400),
                result);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex}");
            return CreateResponse(500, ApiResponse<bool>.Fail(
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