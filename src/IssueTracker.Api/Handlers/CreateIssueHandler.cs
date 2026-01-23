using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

// This tells Lambda which serializer to use
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace IssueTracker.Api.Handlers;

// Lambda Handler for: POST /issues
// Think of this like a Controller action in ASP.NET Core
public class CreateIssueHandler
{
    private readonly IssueService _issueService;
    private readonly JsonSerializerOptions _jsonOptions;

    // Default constructor (Lambda calls this)
    public CreateIssueHandler() : this(Startup.ServiceProvider)
    {
    }

    // Constructor for dependency injection
    public CreateIssueHandler(IServiceProvider serviceProvider)
    {
        _issueService = serviceProvider.GetRequiredService<IssueService>();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // This is the main method Lambda calls
    // request = incoming HTTP request from API Gateway
    // context = Lambda execution info (logging, etc.)
    public async Task<APIGatewayProxyResponse> HandleAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing CreateIssue - RequestId: {context.AwsRequestId}");

        try
        {
            // Step 1: Parse JSON body into CreateIssueRequest
            var createRequest = JsonSerializer.Deserialize<CreateIssueRequest>(
                request.Body,
                _jsonOptions);

            if (createRequest == null)
            {
                return CreateResponse(400, ApiResponse<Issue>.Fail(
                    "Invalid request body",
                    "INVALID_BODY"));
            }

            // Step 2: Call business logic
            var result = await _issueService.CreateIssueAsync(createRequest);

            // Step 3: Return response (201 = Created, 400 = Bad Request)
            return CreateResponse(
                result.Success ? 201 : 400,
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
            context.Logger.LogError($"Unexpected error: {ex}");
            return CreateResponse(500, ApiResponse<Issue>.Fail(
                "Internal server error",
                "INTERNAL_ERROR"));
        }
    }

    // Helper: Create HTTP response
    private APIGatewayProxyResponse CreateResponse<T>(int statusCode, ApiResponse<T> body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body, _jsonOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                // CORS headers (allow browser to call API)
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key",
                ["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS"
            }
        };
    }
}