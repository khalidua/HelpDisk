using HelpDisk.Application.Features.Attachments.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Attachments;

public interface IAttachmentService
{
    Task<Result<Guid>> UploadAsync(
        Guid ticketId,
        UploadAttachmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AttachmentDownloadResponse>> DownloadAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}