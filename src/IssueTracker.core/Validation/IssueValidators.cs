using FluentValidation;
using IssueTracker.Core.Models;

namespace IssueTracker.Core.Validation;

// Validates CreateIssueRequest before saving to database
public class CreateIssueRequestValidator : AbstractValidator<CreateIssueRequest>
{
    public CreateIssueRequestValidator()
    {
        // Title is required and max 200 characters
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");

        // Description is required and max 2000 characters
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters");

        // Priority must be valid enum value
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority value. Must be: Low, Medium, or High");
    }
}

// Validates UpdateIssueRequest
public class UpdateIssueRequestValidator : AbstractValidator<UpdateIssueRequest>
{
    public UpdateIssueRequestValidator()
    {
        // Only validate if title is provided (it's optional in updates)
        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title cannot be empty")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters");
        });

        // Only validate if description is provided
        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description cannot be empty")
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters");
        });

        // Validate status if provided
        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value. Must be: Open, InProgress, or Done");
        });

        // Validate priority if provided
        When(x => x.Priority.HasValue, () =>
        {
            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Invalid priority value. Must be: Low, Medium, or High");
        });

        // At least one field must be updated
        RuleFor(x => x)
            .Must(x => x.Title != null || x.Description != null || x.Status.HasValue || x.Priority.HasValue)
            .WithMessage("At least one field must be provided for update");
    }
}