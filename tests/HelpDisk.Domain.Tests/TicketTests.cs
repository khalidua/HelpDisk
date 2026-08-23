using HelpDisk.Domain.Tickets;

namespace HelpDisk.Domain.Tests;

public class TicketTests
{
    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public void Create_WithValidData_CreatesTicket()
    {
        var categoryId = Guid.NewGuid();

        var result = Ticket.Create(
            "HD-00001",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            categoryId,
            "user-1");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("HD-00001", result.Value.TicketNumber);
        Assert.Equal("Printer jammed", result.Value.Title);
        Assert.Equal(TicketStatus.New, result.Value.Status);
        Assert.Equal(TicketSlaStatus.Pending, result.Value.SlaStatus);
        Assert.Equal(categoryId, result.Value.CategoryId);
        Assert.Equal("user-1", result.Value.ReporterId);
    }

    [Fact]
    public void Create_WithEmptyTicketNumber_ReturnsTicketNumberRequired()
    {
        var result = Ticket.Create(
            "",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.TicketNumberRequired, result.Error);
    }

    [Fact]
    public void Create_WithLongTicketNumber_ReturnsTicketNumberTooLong()
    {
        var result = Ticket.Create(
            new string('A', Ticket.TicketNumberMaxLength + 1),
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.TicketNumberTooLong, result.Error);
    }

    [Fact]
    public void Create_WithEmptyTitle_ReturnsTitleRequired()
    {
        var result = Ticket.Create(
            "HD-00001",
            "",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.TitleRequired, result.Error);
    }

    [Fact]
    public void Create_WithLongTitle_ReturnsTitleTooLong()
    {
        var result = Ticket.Create(
            "HD-00001",
            new string('A', Ticket.TitleMaxLength + 1),
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.TitleTooLong, result.Error);
    }

    [Fact]
    public void Create_WithEmptyDescription_ReturnsDescriptionRequired()
    {
        var result = Ticket.Create(
            "HD-00001",
            "Printer jammed",
            "",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.DescriptionRequired, result.Error);
    }

    [Fact]
    public void Create_WithLongDescription_ReturnsDescriptionTooLong()
    {
        var result = Ticket.Create(
            "HD-00001",
            "Printer jammed",
            new string('A', Ticket.DescriptionMaxLength + 1),
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void Create_WithEmptyCategoryId_ReturnsCategoryRequired()
    {
        var result = Ticket.Create(
            "HD-00001",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.Empty,
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.CategoryRequired, result.Error);
    }

    [Fact]
    public void Create_WithEmptyReporterId_ReturnsReporterRequired()
    {
        var result = Ticket.Create(
            "HD-00001",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.ReporterRequired, result.Error);
    }


    // ============================================================
    // ASSIGN
    // ============================================================

    [Fact]
    public void Assign_WhenTicketIsOpen_AssignsSuccessfully()
    {
        var ticket = CreateTicket();

        var result = ticket.Assign("agent-7");

        Assert.True(result.IsSuccess);
        Assert.Equal("agent-7", ticket.AssigneeId);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Fact]
    public void Assign_WhenTicketIsClosed_ReturnsCannotAssignClosedTicket()
    {
        var ticket = CreateTicket();
        ticket.Close();

        var result = ticket.Assign("agent-7");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.CannotAssignClosedTicket, result.Error);
    }

    [Fact]
    public void Assign_WithEmptyAssigneeId_ReturnsAssigneeRequired()
    {
        var ticket = CreateTicket();

        var result = ticket.Assign("");

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.AssigneeRequired, result.Error);
    }


    // ============================================================
    // CLOSE
    // ============================================================

    [Fact]
    public void Close_WhenTicketIsOpen_ClosesTicket()
    {
        var ticket = CreateTicket();

        var result = ticket.Close();

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.NotNull(ticket.ClosedOnUtc);
    }

    [Fact]
    public void Close_WhenTicketIsAlreadyClosed_ReturnsAlreadyClosed()
    {
        var ticket = CreateTicket();

        ticket.Close();

        var result = ticket.Close();

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.AlreadyClosed, result.Error);
    }


    // ============================================================
    // REOPEN
    // ============================================================

    [Fact]
    public void Reopen_WhenTicketIsNotClosed_ReturnsNotClosed()
    {
        var ticket = CreateTicket();

        var result = ticket.Reopen();

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.NotClosed, result.Error);
    }


    // ============================================================
    // UPDATE DETAILS
    // ============================================================

    [Fact]
    public void UpdateDetails_WhenTicketIsOpen_UpdatesDetails()
    {
        var ticket = CreateTicket();

        var result = ticket.UpdateDetails(
            "New title",
            "New description",
            TicketPriority.Low);

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", ticket.Title);
        Assert.Equal("New description", ticket.Description);
        Assert.Equal(TicketPriority.Low, ticket.Priority);
    }

    [Fact]
    public void UpdateDetails_WhenTicketIsClosed_ReturnsCannotEditClosedTicket()
    {
        var ticket = CreateTicket();
        ticket.Close();

        var result = ticket.UpdateDetails(
            "New title",
            "New description",
            TicketPriority.Low);

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.CannotEditClosedTicket, result.Error);
    }


    // ============================================================
    // COMMENTS
    // ============================================================

    [Fact]
    public void AddComment_WithValidData_AddsComment()
    {
        var ticket = CreateTicket();

        var result = ticket.AddComment(
            "I restarted the printer.",
            "agent-7",
            false);

        Assert.True(result.IsSuccess);
        Assert.Single(ticket.Comments);
        Assert.Equal("I restarted the printer.", result.Value.Body);
    }

    [Fact]
    public void AddComment_WhenTicketIsClosed_ReturnsCannotCommentOnClosedTicket()
    {
        var ticket = CreateTicket();
        ticket.Close();

        var result = ticket.AddComment(
            "Comment",
            "agent-7",
            false);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TicketErrors.CannotCommentOnClosedTicket,
            result.Error);
    }

    [Fact]
    public void AddComment_WithEmptyBody_ReturnsCommentBodyRequired()
    {
        var ticket = CreateTicket();

        var result = ticket.AddComment(
            "",
            "agent-7",
            false);

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.CommentBodyRequired, result.Error);
    }


    // ============================================================
    // ATTACHMENTS
    // ============================================================

    [Fact]
    public void AddAttachment_WithValidData_AddsAttachment()
    {
        var ticket = CreateTicket();

        var result = ticket.AddAttachment(
            "error.png",
            "image/png",
            1024,
            "tickets/error.png",
            "user-1");

        Assert.True(result.IsSuccess);
        Assert.Single(ticket.Attachments);
        Assert.Equal("error.png", result.Value.FileName);
    }

    [Fact]
    public void AddAttachment_WhenTicketIsClosed_ReturnsCannotAddAttachmentToClosedTicket()
    {
        var ticket = CreateTicket();
        ticket.Close();

        var result = ticket.AddAttachment(
            "error.png",
            "image/png",
            1024,
            "tickets/error.png",
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TicketErrors.CannotAddAttachmentToClosedTicket,
            result.Error);
    }

    [Fact]
    public void AddAttachment_WhenMaximumReached_ReturnsMaximumAttachmentsReached()
    {
        var ticket = CreateTicket();

        for (var i = 0; i < 5; i++)
        {
            ticket.AddAttachment(
                $"file{i}.txt",
                "text/plain",
                100,
                $"files/file{i}.txt",
                "user-1");
        }

        var result = ticket.AddAttachment(
            "sixth.txt",
            "text/plain",
            100,
            "files/sixth.txt",
            "user-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TicketErrors.MaximumAttachmentsReached,
            result.Error);
    }

    [Fact]
    public void RemoveAttachment_WhenAttachmentExists_RemovesIt()
    {
        var ticket = CreateTicket();

        var attachment = ticket.AddAttachment(
            "error.png",
            "image/png",
            1024,
            "tickets/error.png",
            "user-1").Value;

        var result = ticket.RemoveAttachment(attachment.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(ticket.Attachments);
    }

    [Fact]
    public void RemoveAttachment_WhenAttachmentDoesNotExist_ReturnsAttachmentNotFound()
    {
        var ticket = CreateTicket();

        var result = ticket.RemoveAttachment(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.AttachmentNotFound, result.Error);
    }


    // ============================================================
    // SLA
    // ============================================================

    [Fact]
    public void SetResponseDeadline_WhenDeadlineIsAfterCreation_Succeeds()
    {
        var ticket = CreateTicket();

        var deadline = DateTime.UtcNow.AddHours(2);

        var result = ticket.SetResponseDeadline(deadline);

        Assert.True(result.IsSuccess);
        Assert.Equal(deadline, ticket.ResponseDeadlineUtc);
    }

    [Fact]
    public void SetResponseDeadline_WhenDeadlineIsBeforeCreation_ReturnsInvalidResponseDeadline()
    {
        var ticket = CreateTicket();

        ticket.CreatedOnUtc = new DateTime(2026, 1, 1);

        var deadline = new DateTime(2025, 1, 1);

        var result = ticket.SetResponseDeadline(deadline);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TicketErrors.InvalidResponseDeadline,
            result.Error);
    }

    [Fact]
    public void MarkSlaMet_WhenPending_MarksAsMet()
    {
        var ticket = CreateTicket();

        var result = ticket.MarkSlaMet();

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketSlaStatus.Met, ticket.SlaStatus);
    }

    [Fact]
    public void MarkSlaMet_WhenAlreadyMet_ReturnsSlaAlreadyResolved()
    {
        var ticket = CreateTicket();

        ticket.MarkSlaMet();

        var result = ticket.MarkSlaMet();

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.SlaAlreadyResolved, result.Error);
    }

    [Fact]
    public void MarkSlaBreached_WhenPending_MarksAsBreached()
    {
        var ticket = CreateTicket();

        var result = ticket.MarkSlaBreached();

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketSlaStatus.Breached, ticket.SlaStatus);
    }

    [Fact]
    public void MarkSlaBreached_WhenAlreadyBreached_ReturnsSlaAlreadyResolved()
    {
        var ticket = CreateTicket();

        ticket.MarkSlaBreached();

        var result = ticket.MarkSlaBreached();

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketErrors.SlaAlreadyResolved, result.Error);
    }


    // ============================================================
    // TEST HELPER
    // ============================================================

    private static Ticket CreateTicket()
    {
        return Ticket.Create(
            "HD-TEST-001",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "user-1").Value;
    }
}