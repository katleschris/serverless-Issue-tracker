using IssueTracker.Core.Models;

namespace IssueTracker.Core.Interfaces;

// This is a CONTRACT that says "whoever implements this must have these methods"
// Why? So we can swap DynamoDB for SQL Server later without changing business logic
public interface IIssueRepository
{
    // Create a new issue
    Task<Issue> CreateAsync(Issue issue, CancellationToken cancellationToken = default);
    
    // Get one issue by ID
    Task<Issue?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    // Get all issues, optionally filter by status
    Task<List<Issue>> GetAllAsync(IssueStatus? status = null, CancellationToken cancellationToken = default);
    
    // Update an existing issue
    Task<Issue?> UpdateAsync(string id, Issue issue, CancellationToken cancellationToken = default);
    
    // Delete an issue
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    
    // Check if issue exists
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}