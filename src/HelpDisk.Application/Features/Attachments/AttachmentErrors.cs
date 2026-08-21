using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Attachments;

public static class AttachmentErrors
{
    public static readonly Error AttachmentNotFound = Error.NotFound(
        "Attachments.NotFound",
        "The specified attachment was not found.");

    public static readonly Error FileTooLarge = Error.Validation(
        "Attachments.FileTooLarge",
        "The file cannot be larger than 10 MB.");

    public static readonly Error MaximumAttachmentsReached = Error.Conflict(
        "Attachments.MaximumAttachmentsReached",
        "A ticket cannot have more than 5 attachments.");

    public static readonly Error FileTypeNotAllowed = Error.Validation(
        "Attachments.FileTypeNotAllowed",
        "The selected file type is not allowed.");

    public static readonly Error FileNameInvalid = Error.Validation(
        "Attachments.FileNameInvalid",
        "The file name is invalid.");

    public static readonly Error UploadFailed = Error.Validation(
        "Attachments.UploadFailed",
        "The attachment could not be uploaded.");

    public static readonly Error DeleteFailed = Error.Validation(
        "Attachments.DeleteFailed",
        "The attachment could not be deleted.");

    public static readonly Error TicketNotFound = Error.NotFound(
        "Attachments.TicketNotFound",
        "The specified ticket was not found.");
}