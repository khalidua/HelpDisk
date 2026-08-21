namespace HelpDisk.Application.Features.Attachments.Dtos;

public sealed record AttachmentDownloadResponse(
    Stream File,
    string FileName,
    string ContentType);