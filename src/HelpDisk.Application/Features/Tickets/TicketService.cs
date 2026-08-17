using FluentValidation;
using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Tickets.Dtos;
using HelpDisk.Application.Features.Tickets.Mapping;
using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Repositories;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;
using Mapster;

namespace HelpDisk.Application.Features.Tickets;

/// <summary>
/// All business logic for the Ticket feature.
/// </summary>
/// <remarks>
/// ============================================================================
/// WHAT AN APPLICATION SERVICE IS FOR - and what it is not for.
/// ============================================================================
///
/// This class ORCHESTRATES. It does not decide business rules; it arranges for
/// the domain to decide them. Read any method below and you will find the same
/// five beats:
///
///     1. validate the request shape          (FluentValidation)
///     2. load what is needed                 (repository)
///     3. ask the domain to do the thing      (ticket.Assign(...))
///     4. persist                             (unit of work)
///     5. shape the answer                    (DTO)
///
/// Step 3 is where the business rule lives, and it is one line. If a method
/// here ever grows an "if (ticket.Status == ...)", that rule has escaped the
/// aggregate and belongs back inside Ticket - because a rule in a service is a
/// rule only for callers who remember to use that service.
///
/// The rule of thumb: this layer knows the ORDER of things. The domain knows
/// the TRUTH of things.
///
/// ---------------------------------------------------------------------------
/// Note the constructor. Every dependency is an interface, and not one of them
/// is implemented in this project:
///
///     ITicketRepository, ICategoryRepository, IUnitOfWork -> declared in Domain
///     ICurrentUser                                        -> declared in Application
///     IValidator<T>                                       -> FluentValidation
///
/// This class cannot see EF Core, SQL Server, or HttpContext. That is not an
/// accident of what it happens to need - it is what the project references make
/// possible. Try to type "DbContext" here; it will not compile.
/// ---------------------------------------------------------------------------
/// </remarks>
public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateTicketRequest> _createValidator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<AssignTicketRequest> _assignValidator;
    private readonly IValidator<AddCommentRequest> _commentValidator;
    private readonly IValidator<TicketSearchRequest> _searchValidator;



    public TicketService(
        ITicketRepository tickets,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateTicketRequest> createValidator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<AssignTicketRequest> assignValidator,
        IValidator<AddCommentRequest> commentValidator,
        IValidator<TicketSearchRequest> searchValidator,
        IIdentityService identityService)
    {
        _tickets = tickets;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
        _commentValidator = commentValidator;
        _searchValidator = searchValidator;
        _identityService = identityService;
    }

    /// <summary>
    /// Creates a ticket. The fullest example in the template - read this one
    /// first.
    /// </summary>
    public async Task<Result<Guid>> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        // ---- 1. Is the request well-formed? --------------------------------
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationError(validation);
        }

        // ---- 2. Is the referenced aggregate real? --------------------------
        // This check belongs HERE and not in Ticket.Create. A ticket cannot
        // answer "does this category exist?" - that requires going to storage,
        // and an aggregate that reaches for a repository is no longer testable
        // without one. Cross-aggregate existence checks are an orchestration
        // concern, so the orchestrator does them.
        if (!await _categories.ExistsAsync(request.CategoryId, cancellationToken))
        {
            return CategoryErrors.NotFound(request.CategoryId);
        }

        // ---- 3. Let the domain build it ------------------------------------
        // Note ICurrentUser supplying the reporter. The client never sends it.
        var ticketResult = Ticket.Create(
            request.Title,
            request.Description,
            request.Priority,
            request.CategoryId,
            _currentUser.UserId);

        if (ticketResult.IsFailure)
        {
            return ticketResult.Error;
        }

        var ticket = ticketResult.Value;

        // ---- 4. Persist ----------------------------------------------------
        // AddAsync only starts tracking. SaveChangesAsync is what writes - and
        // is also what stamps CreatedOnUtc and dispatches
        // TicketCreatedDomainEvent. See DomainEventsInterceptor.
        await _tickets.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ---- 5. Answer -----------------------------------------------------
        return ticket.Id;
    }

    public async Task<Result<TicketResponse>> GetByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        // Comments are wanted here, so use the method that loads them.
        var ticket = await _tickets.GetWithCommentsAsync(ticketId, cancellationToken);

        if (ticket is null)
        {
            // "Not found" is an ordinary outcome, not an exception. It becomes
            // a 404 in ApiController - but this layer never says "404".
            return TicketErrors.NotFound(ticketId);
        }

        if(_currentUser.Role == "Customer" && ticket.ReporterId != _currentUser.UserId)
        {
            return TicketErrors.NotFound(ticketId);
        }

        return ticket.ToResponse(_currentUser.Role);
    }

    public async Task<Result<PagedResponse<TicketListItemResponse>>> SearchAsync(
        TicketSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationError(validation);
        }

        string? reporterId = null;

        if (_currentUser.Role == "Customer")
        {
            reporterId = _currentUser.UserId;
        }

        var page = await _tickets.SearchAsync(
            request.Keyword,
            request.Status,
            request.CategoryId,
            reporterId,
            request.Page,
            request.PageSize,
            cancellationToken);
        // Pagination<Ticket> -> PagedResponse<TicketListItemResponse>.
        // The entities stop here; only DTOs go out.
        return page.ToPagedResponse(t => t.Adapt<TicketListItemResponse>());
    }

    public async Task<Result> UpdateAsync(
        Guid ticketId,
        UpdateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationError(validation);
        }

        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }

        // The "cannot edit a closed ticket" rule is inside UpdateDetails, not
        // here. This method only decides what to do with the answer.
        var result = ticket.UpdateDetails(request.Title, request.Description, request.Priority);
        if (result.IsFailure)
        {
            return result;
        }

        // No repository Update call. EF tracks the entity we loaded, so it
        // already knows what changed - this is the unit-of-work pattern doing
        // its job.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AssignAsync(
        Guid ticketId,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationError(validation);
        }

        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }
        var assigneeResult = await _identityService.GetUserAsync(request.AssigneeId, cancellationToken);

        if (assigneeResult.IsFailure)
        {
            return assigneeResult.Error;
        }

        if (assigneeResult.Value.Role != "Agent" &&
            assigneeResult.Value.Role != "Admin")
        {
            return TicketErrors.InvalidAssignee;
        }

        // One line of business logic. Everything around it is plumbing - which
        // is exactly the ratio you want in an application service.
        var result = ticket.Assign(request.AssigneeId);
        if (result.IsFailure)
        {
            return result;
        }

        // TicketAssignedDomainEvent was raised inside Assign. It is dispatched
        // here, after the save succeeds - never before.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CloseAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }

        var result = ticket.Close();
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReopenAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }

        if(_currentUser.Role != "Customer" || ticket.ReporterId != _currentUser.UserId)
        {
            return TicketErrors.NotFound(ticketId);
        }

        var result = ticket.Reopen();

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Adds a comment to a ticket.
    /// </summary>
    /// <remarks>
    /// The aggregate boundary in practice. There is no comment repository and
    /// no comment service - a comment is reached THROUGH its ticket, which is
    /// what lets Ticket enforce "no comments on a closed ticket". Note the
    /// GetWithCommentsAsync call: the collection must be loaded before adding
    /// to it, or EF will not track the new child.
    /// </remarks>
    public async Task<Result<Guid>> AddCommentAsync(
        Guid ticketId,
        AddCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _commentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationError(validation);
        }

        var ticket = await _tickets.GetWithCommentsAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }

        if(_currentUser.Role == "Customer" && ticket.ReporterId != _currentUser.UserId)
        {
            return TicketErrors.NotFound(ticketId);
        }
        if (_currentUser.Role == "Customer" && request.IsInternal)
        {
            return TicketErrors.InternalCommentNotAllowed;
        }

        var commentResult = ticket.AddComment(request.Body, _currentUser.UserId, request.IsInternal);
        if (commentResult.IsFailure)
        {
            return commentResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return commentResult.Value.Id;
    }

    /// <summary>
    /// Deletes a ticket - softly, though nothing here says so.
    /// </summary>
    /// <remarks>
    /// This method calls Remove and means it. That the row survives with
    /// IsDeleted = true is a decision made entirely by SoftDeleteInterceptor in
    /// Infrastructure. Change your mind about soft delete and this code does
    /// not move.
    /// </remarks>
    public async Task<Result> DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketErrors.NotFound(ticketId);
        }

        _tickets.Remove(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Folds FluentValidation's failure list into a single domain
    /// <see cref="Error"/>.
    /// </summary>
    /// <remarks>
    /// A translation between two vocabularies. FluentValidation speaks in
    /// ValidationResult; everything inward of here speaks in Result and Error.
    /// Converting at the boundary means the rest of the layer never learns
    /// which validation library is in use - swap FluentValidation for something
    /// else and only this method changes.
    ///
    /// All messages are joined into one string. A production API would return
    /// them per-field so a form can highlight the offending inputs; that would
    /// mean adding a field-keyed dictionary to Error, which is a worthwhile
    /// exercise but more machinery than the lesson needs.
    /// </remarks>
    private static Error ToValidationError(FluentValidation.Results.ValidationResult validation) =>
        Error.Validation(
            "Validation.Failed",
            string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
}
