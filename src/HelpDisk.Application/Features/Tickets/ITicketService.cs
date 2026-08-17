using HelpDisk.Application.Features.Tickets.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Tickets;

/// <summary>
/// Every use case the Ticket feature supports.
/// </summary>
/// <remarks>
/// ============================================================================
/// FEATURE SLICING WITHOUT CQRS
/// ============================================================================
///
/// This template groups code by FEATURE (Features/Tickets/, Features/Categories/)
/// rather than by technical kind (Services/, Dtos/, Validators/). Everything a
/// ticket does is in one folder. Delete the folder and the feature is gone, with
/// nothing left behind in six other directories.
///
/// Inside the slice, there is ONE service class per feature instead of a
/// command/query pair per operation. The MOJ reference does the opposite - the
/// CourseTitle feature alone is 19 files across Commands/ and Queries/, each
/// operation getting its own Command, Handler, and Response.
///
/// The trade, stated fairly:
///
///   ONE SERVICE (here)      Fewer files. You can read every ticket operation
///                           on one screen and see how they relate. Navigation
///                           is a single F12 from controller to logic. Cost:
///                           the class grows with the feature, and there is no
///                           pipeline to hang cross-cutting behaviour on - so
///                           validation is called explicitly in each method.
///
///   CQRS + MediatR (MOJ)    Each operation is isolated and independently
///                           testable; IPipelineBehavior gives you validation,
///                           logging and caching for free across every handler;
///                           reads and writes can use different models. Cost: a
///                           lot of files, and indirection - the controller
///                           sends a message and you must go looking for who
///                           handles it.
///
/// Neither is wrong. CQRS earns its cost on large systems, especially when
/// reads and writes genuinely diverge. This template is for teaching the LAYERS,
/// and one service per feature keeps the layers visible instead of burying them
/// under a messaging pattern.
///
/// ---------------------------------------------------------------------------
/// Two things to notice about every signature below:
///
///   1. They return Result or Result<T>, never bare values and never void. A
///      caller cannot forget that "not found" is possible.
///   2. They speak in DTOs, never entities. No Ticket ever leaves this layer.
/// ---------------------------------------------------------------------------
/// </remarks>
public interface ITicketService
{
    Task<Result<Guid>> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);

    Task<Result<TicketResponse>> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<TicketListItemResponse>>> SearchAsync(
        TicketSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid ticketId, UpdateTicketRequest request, CancellationToken cancellationToken = default);

    Task<Result> AssignAsync(Guid ticketId, AssignTicketRequest request, CancellationToken cancellationToken = default);

    Task<Result> CloseAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<Result> ReopenAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> AddCommentAsync(Guid ticketId, AddCommentRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TicketCommentResponse>>> GetCommentsAsync(
    Guid ticketId,
    CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
