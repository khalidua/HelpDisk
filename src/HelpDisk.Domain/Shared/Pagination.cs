namespace HelpDisk.Domain.Shared;

/// <summary>
/// One page of results, plus enough metadata for a caller to render a pager.
/// </summary>
/// <remarks>
/// This lives in Domain rather than Application because ITicketRepository -
/// a Domain contract - returns it. If it lived in Application, Domain would
/// have to reference Application to declare the interface, and the dependency
/// arrow would point the wrong way.
///
/// A fair objection: is paging really a DOMAIN concept, or a presentation one?
/// Honest answer - it is a query concern that leaked inward because the
/// repository interface is declared in Domain. The purist fix is a separate
/// read model. That is a real technique, and it is also how a 4-project
/// teaching template turns into an 8-project one. We take the pragmatic
/// option and write down why.
/// </remarks>
public sealed class Pagination<T>
{
    public Pagination(int currentPage, int pageSize, int totalItems, IReadOnlyList<T> data)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        Data = data;
    }

    public int CurrentPage { get; }

    public int PageSize { get; }

    public int TotalPages { get; }

    public int TotalItems { get; }

    public IReadOnlyList<T> Data { get; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public static Pagination<T> Empty(int pageSize) =>
        new(currentPage: 1, pageSize, totalItems: 0, data: []);
}
