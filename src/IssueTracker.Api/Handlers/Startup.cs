using Amazon.DynamoDBv2;
using FluentValidation;
using IssueTracker.Core.Interfaces;
using IssueTracker.Core.Models;
using IssueTracker.Core.Services;
using IssueTracker.Core.Validation;
using IssueTracker.Infrastructure.DynamoDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IssueTracker.Api;

// This sets up DEPENDENCY INJECTION
// It's like telling .NET: "When someone asks for X, give them Y"
public static class Startup
{
    private static IServiceProvider? _serviceProvider;

    // This gets called when Lambda starts up
    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();
            }
            return _serviceProvider;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. AWS DynamoDB Client
        // Singleton = created once and reused (good for performance)
        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            return new AmazonDynamoDBClient();
        });

        // 2. Get table name from environment variable
        // AWS SAM will set this for us when deploying
        var tableName = Environment.GetEnvironmentVariable("ISSUES_TABLE_NAME") ?? "IssueTrackerTable";

        // 3. Repository (Database layer)
        services.AddSingleton<IIssueRepository>(sp =>
        {
            var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
            var logger = sp.GetRequiredService<ILogger<DynamoDbIssueRepository>>();
            return new DynamoDbIssueRepository(dynamoDb, tableName, logger);
        });

        // 4. Validators
        // Scoped = created once per request
        services.AddScoped<IValidator<CreateIssueRequest>, CreateIssueRequestValidator>();
        services.AddScoped<IValidator<UpdateIssueRequest>, UpdateIssueRequestValidator>();

        // 5. Business logic service
        services.AddScoped<IssueService>();

        // 6. Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}