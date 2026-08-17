using FluentValidation;
using HelpDisk.Application.Features.Tickets.Dtos;
using HelpDisk.Domain.Tickets;

namespace HelpDisk.Application.Features.Tickets.Validators;

/*
 * ============================================================================
 * SHAPE VALIDATION vs BUSINESS INVARIANTS - the distinction to teach here.
 * ============================================================================
 *
 * These validators and Ticket.Create check overlapping things, and students
 * always ask why both exist. The answer is that they answer different
 * questions:
 *
 *   VALIDATOR (this file, Application layer)
 *     "Is this request well-formed?"
 *     Title present, not too long. Priority a real enum value. Page >= 1.
 *     Runs FIRST and reports EVERY problem at once, so the user fixes their
 *     form in one go instead of one field per round trip.
 *     Only protects callers who come through this service.
 *
 *   AGGREGATE (Ticket.Create, Domain layer)
 *     "Is this a legal Ticket?"
 *     Guarantees the rule for EVERY caller, forever - the seeder, an import
 *     job, a future gRPC endpoint, a unit test, code not written yet.
 *     Returns on the FIRST failure, because it is a guard, not a form.
 *
 * If you delete the validator, the system stays correct - errors just arrive
 * one at a time and less politely. If you delete the checks in Ticket.Create,
 * the system is one forgotten call site away from invalid data forever.
 *
 * So: the validator is a convenience. The aggregate is the guarantee. When they
 * disagree, the aggregate wins.
 *
 * ---------------------------------------------------------------------------
 * WHERE DOES THIS RUN?
 *
 * There is no MediatR in this template, so there is no ValidationPipelineBehavior
 * to hook. TicketService calls the validator explicitly as its first statement.
 * See TicketService.CreateAsync.
 *
 * The alternative is an MVC action filter that validates automatically. It is
 * less typing and it is what many teams do. It was rejected here because
 * "where did this 400 come from?" is a bad first question for someone learning
 * the layers.
 * ---------------------------------------------------------------------------
 */

public sealed class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(Ticket.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(Ticket.DescriptionMaxLength);

        // IsInEnum matters more than it looks. Without it, priority=99 binds
        // happily to a TicketPriority and you store a value with no meaning.
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be Low, Normal, High or Urgent.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }
}

public sealed class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Ticket.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(Ticket.DescriptionMaxLength);

        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class AssignTicketRequestValidator : AbstractValidator<AssignTicketRequest>
{
    public AssignTicketRequestValidator()
    {
        RuleFor(x => x.AssigneeId).NotEmpty().WithMessage("AssigneeId is required.");
    }
}

public sealed class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("A comment cannot be empty.")
            .MaximumLength(TicketComment.BodyMaxLength);
    }
}

/// <summary>
/// Guards the pager. PageSize is capped because "give me everything" is how a
/// list endpoint becomes an outage.
/// </summary>
public sealed class TicketSearchRequestValidator : AbstractValidator<TicketSearchRequest>
{
    public const int MaxPageSize = 100;

    public TicketSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be earlier than or equal to ToDate.");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("SortBy must be CreatedOn, Priority or Status.");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        return sortBy is null ||
               sortBy.Equals("CreatedOn", StringComparison.OrdinalIgnoreCase) ||
               sortBy.Equals("Priority", StringComparison.OrdinalIgnoreCase) ||
               sortBy.Equals("Status", StringComparison.OrdinalIgnoreCase);
    }
}
