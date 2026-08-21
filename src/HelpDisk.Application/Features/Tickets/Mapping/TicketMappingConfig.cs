using HelpDisk.Application.Features.Tickets.Dtos;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;
using Mapster;

namespace HelpDisk.Application.Features.Tickets.Mapping;

/// <summary>
/// Converts Ticket entities into the DTOs the API returns.
/// </summary>
/// <remarks>
/// ============================================================================
/// NOTICE WHAT IS MISSING: there is no CreateTicketRequest -> Ticket mapping.
/// The mapping in this solution goes ONE WAY ONLY, entity -> DTO.
/// ============================================================================
///
/// That asymmetry is a direct consequence of the rich-aggregate design, and it
/// is the thing to point at when somebody asks what rich aggregates actually
/// change day to day.
///
/// The MOJ reference codebase maps inward, like this:
///
///     var title = _mapper.Map&lt;CourseTitle&gt;(request);   // entity built by reflection
///     await _repository.AddAsync(title);
///
/// A mapper builds entities by setting properties reflectively. It walks around
/// the front door. Every rule in Ticket.Create - title not blank, category
/// present, status starts at New, TicketCreatedDomainEvent raised - is skipped
/// in silence. You end up with an object of type Ticket that the domain would
/// have refused to create, and nothing anywhere reports a problem.
///
/// Here, entities are born exactly one way:
///
///     var result = Ticket.Create(request.Title, request.Description, ...);
///     if (result.IsFailure) return result.Error;
///
/// More typing per field. In exchange, an invalid Ticket cannot exist.
///
/// Mapping OUT is safe and stays, because a DTO has no invariants to protect -
/// it is a data shape, and that is exactly the job mappers are good at.
/// </remarks>
public sealed class TicketMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TicketComment, TicketCommentResponse>();

        config.NewConfig<TicketAttachment, TicketAttachmentResponse>();

        config.NewConfig<Ticket, TicketListItemResponse>();

        config.NewConfig<Ticket, TicketResponse>()
            .Map(
                dest => dest.Comments,
                src => src.Comments
                    .Select(c => c.Adapt<TicketCommentResponse>())
                    .ToList())
            .Map(
                dest => dest.Attachments,
                src => src.Attachments
                    .Select(a => a.Adapt<TicketAttachmentResponse>())
                    .ToList());
    }
}

/// <summary>
/// Turns a Domain <see cref="Pagination{T}"/> into the API's
/// <see cref="PagedResponse{T}"/>.
/// </summary>
/// <remarks>
/// Hand-written rather than configured in Mapster because it changes the
/// element type as well as the container, and one readable extension method
/// beats a generic mapping rule that people have to take on faith.
/// </remarks>
public static class PaginationMappingExtensions
{
    public static PagedResponse<TDestination> ToPagedResponse<TSource, TDestination>(
        this Pagination<TSource> page,
        Func<TSource, TDestination> project) =>
        new(
            Data: page.Data.Select(project).ToList(),
            CurrentPage: page.CurrentPage,
            PageSize: page.PageSize,
            TotalPages: page.TotalPages,
            TotalItems: page.TotalItems,
            HasPreviousPage: page.HasPreviousPage,
            HasNextPage: page.HasNextPage);
}

public static class TicketMappingExtensions
{
    public static TicketResponse ToResponse(
        this Ticket ticket,
        string currentUserRole)
    {
        var response = ticket.Adapt<TicketResponse>();

        if (currentUserRole == "Customer")
        {
            response = response with
            {
                Comments = response.Comments
                    .Where(c => !c.IsInternal)
                    .ToList()
            };
        }

        return response;
    }
}
