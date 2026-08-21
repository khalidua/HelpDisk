using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Attachments;
using HelpDisk.Application.Features.Attachments.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

[Route("api/tickets/{ticketId:guid}/attachments")]
[ApiController]
[Authorize]
public sealed class AttachmentsController : ApiController
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(
        IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(
        Guid ticketId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A file is required.");
        }

        var request = new UploadAttachmentRequest(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await _attachmentService.UploadAsync(
            ticketId,
            request,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{attachmentId:guid}")]
    public async Task<IActionResult> Download(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await _attachmentService.DownloadAsync(
            ticketId,
            attachmentId,
            cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        return File(
            result.Value.File,
            result.Value.ContentType,
            result.Value.FileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await _attachmentService.DeleteAsync(
            ticketId,
            attachmentId,
            cancellationToken);

        return HandleResult(result);
    }
}