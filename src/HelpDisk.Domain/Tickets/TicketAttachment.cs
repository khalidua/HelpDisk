using HelpDisk.Domain.Primitives;

namespace HelpDisk.Domain.Tickets;

public sealed class TicketAttachment : Entity<Guid>
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int StorageKeyMaxLength = 500;

    private TicketAttachment()
    {
    }

    internal TicketAttachment(
        Guid id,
        Guid ticketId,
        string fileName,
        string contentType,
        long fileSize,
        string storageKey,
        string uploadedById)
        : base(id)
    {
        TicketId = ticketId;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        StorageKey = storageKey;
        UploadedById = uploadedById;
    }

    public Guid TicketId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public string UploadedById { get; private set; } = null!;
}