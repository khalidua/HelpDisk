using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Attachments.Dtos;
using HelpDisk.Domain.Repositories;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;

namespace HelpDisk.Application.Features.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    private readonly ITicketRepository _tickets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public AttachmentService(
        ITicketRepository tickets,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Guid>> UploadAsync(
    Guid ticketId,
    UploadAttachmentRequest request,
    CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetWithCommentsAndAttachmentsAsync(
            ticketId,
            cancellationToken);

        if (ticket is null)
        {
            return AttachmentErrors.TicketNotFound;
        }

        if (_currentUser.Role == "Customer")
        {
            if (!_currentUser.CompanyId.HasValue)
            {
                return AttachmentErrors.TicketNotFound;
            }

            var reporter = await _identityService.GetUserAsync(
                ticket.ReporterId,
                cancellationToken);

            if (reporter.IsFailure)
            {
                return AttachmentErrors.TicketNotFound;
            }

            if (ticket.ReporterId != _currentUser.UserId ||
                reporter.Value.CompanyId != _currentUser.CompanyId)
            {
                return AttachmentErrors.TicketNotFound;
            }
        }

        if (request.FileSize > 10 * 1024 * 1024)
        {
            return AttachmentErrors.FileTooLarge;
        }

        if (ticket.Attachments.Count >= 5)
        {
            return AttachmentErrors.MaximumAttachmentsReached;
        }

        var allowedContentTypes = new[]
        {
        "image/jpeg",
        "image/png",
        "application/pdf",
        "text/plain",
        "application/zip",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

        if (!allowedContentTypes.Contains(
            request.ContentType,
            StringComparer.OrdinalIgnoreCase))
        {
            return AttachmentErrors.FileTypeNotAllowed;
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return AttachmentErrors.FileNameInvalid;
        }

        var storageResult = await _fileStorage.SaveAsync(
            request.File,
            request.FileName,
            cancellationToken);

        if (storageResult.IsFailure)
        {
            return storageResult.Error;
        }

        var attachmentResult = ticket.AddAttachment(
            request.FileName,
            request.ContentType,
            request.FileSize,
            storageResult.Value,
            _currentUser.UserId);

        if (attachmentResult.IsFailure)
        {
            await _fileStorage.DeleteAsync(
                storageResult.Value,
                cancellationToken);

            return attachmentResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attachmentResult.Value.Id;
    }

    public async Task<Result<AttachmentDownloadResponse>> DownloadAsync(
    Guid ticketId,
    Guid attachmentId,
    CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetWithCommentsAndAttachmentsAsync(
            ticketId,
            cancellationToken);

        if (ticket is null)
        {
            return AttachmentErrors.TicketNotFound;
        }

        if (_currentUser.Role == "Customer")
        {
            if (!_currentUser.CompanyId.HasValue)
            {
                return AttachmentErrors.TicketNotFound;
            }

            var reporter = await _identityService.GetUserAsync(
                ticket.ReporterId,
                cancellationToken);

            if (reporter.IsFailure ||
                ticket.ReporterId != _currentUser.UserId ||
                reporter.Value.CompanyId != _currentUser.CompanyId)
            {
                return AttachmentErrors.TicketNotFound;
            }
        }

        var attachment = ticket.Attachments
            .FirstOrDefault(a => a.Id == attachmentId);

        if (attachment is null)
        {
            return AttachmentErrors.AttachmentNotFound;
        }

        var fileResult = await _fileStorage.GetAsync(
            attachment.StorageKey,
            cancellationToken);

        if (fileResult.IsFailure)
        {
            return fileResult.Error;
        }

        return new AttachmentDownloadResponse(
            fileResult.Value,
            attachment.FileName,
            attachment.ContentType);
    }

    public async Task<Result> DeleteAsync(
    Guid ticketId,
    Guid attachmentId,
    CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetWithCommentsAndAttachmentsAsync(
            ticketId,
            cancellationToken);

        if (ticket is null)
        {
            return AttachmentErrors.TicketNotFound;
        }

        if (_currentUser.Role == "Customer")
        {
            if (!_currentUser.CompanyId.HasValue)
            {
                return AttachmentErrors.TicketNotFound;
            }

            var reporter = await _identityService.GetUserAsync(
                ticket.ReporterId,
                cancellationToken);

            if (reporter.IsFailure ||
                ticket.ReporterId != _currentUser.UserId ||
                reporter.Value.CompanyId != _currentUser.CompanyId)
            {
                return AttachmentErrors.TicketNotFound;
            }
        }

        var attachment = ticket.Attachments
            .FirstOrDefault(a => a.Id == attachmentId);

        if (attachment is null)
        {
            return AttachmentErrors.AttachmentNotFound;
        }

        var storageResult = await _fileStorage.DeleteAsync(
            attachment.StorageKey,
            cancellationToken);

        if (storageResult.IsFailure)
        {
            return storageResult.Error;
        }

        var result = ticket.RemoveAttachment(attachmentId);

        if (result.IsFailure)
        {
            return result.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}