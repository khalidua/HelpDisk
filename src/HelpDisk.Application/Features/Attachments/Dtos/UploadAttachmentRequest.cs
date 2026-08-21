namespace HelpDisk.Application.Features.Attachments.Dtos;

public sealed record UploadAttachmentRequest(
    Stream File,
    string FileName,
    string ContentType,
    long FileSize);