using FluentValidation;

using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Tickets;
using HelpDisk.Application.Features.Tickets.Dtos;
using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Repositories;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;

using Moq;

namespace HelpDisk.Application.Tests;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();
    private readonly Mock<ITicketNumberGenerator> _ticketNumberGenerator = new();
    private readonly Mock<IIdentityService> _identityService = new();

    private readonly Mock<IValidator<CreateTicketRequest>> _createValidator = new();
    private readonly Mock<IValidator<UpdateTicketRequest>> _updateValidator = new();
    private readonly Mock<IValidator<AssignTicketRequest>> _assignValidator = new();
    private readonly Mock<IValidator<AddCommentRequest>> _commentValidator = new();
    private readonly Mock<IValidator<TicketSearchRequest>> _searchValidator = new();

    private TicketService CreateService()
    {
        return new TicketService(
            _tickets.Object,
            _categories.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _dateTime.Object,
            _ticketNumberGenerator.Object,
            _createValidator.Object,
            _updateValidator.Object,
            _assignValidator.Object,
            _commentValidator.Object,
            _searchValidator.Object,
            _identityService.Object);
    }

    private static FluentValidation.Results.ValidationResult ValidValidation()
        => new();

    private static FluentValidation.Results.ValidationResult InvalidValidation(
        string message = "Invalid request")
        => new(new[]
        {
            new FluentValidation.Results.ValidationFailure(
                "Request",
                message)
        });

    private static Ticket CreateTicket()
    {
        return Ticket.Create(
            "HD-TEST-001",
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid(),
            "customer-1").Value;
    }

    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsValidationError()
    {
        _createValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<CreateTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(InvalidValidation());

        var service = CreateService();

        var request = new CreateTicketRequest(
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            Guid.NewGuid());

        var result = await service.CreateAsync(request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var categoryId = Guid.NewGuid();

        _createValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<CreateTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _categories
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var service = CreateService();

        var request = new CreateTicketRequest(
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            categoryId);

        var result = await service.CreateAsync(request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesAndSavesTicket()
    {
        var category = Category.Create(
            "Hardware",
            4).Value;

        _createValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<CreateTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _categories
            .Setup(x => x.GetByIdAsync(
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _currentUser
            .Setup(x => x.UserId)
            .Returns("customer-1");

        _ticketNumberGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("HD-00001");

        _dateTime
            .Setup(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        _tickets
            .Setup(x => x.AddAsync(
                It.IsAny<Ticket>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var request = new CreateTicketRequest(
            "Printer jammed",
            "Printer is not working",
            TicketPriority.High,
            category.Id);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsSuccess);

        _tickets.Verify(
            x => x.AddAsync(
                It.IsAny<Ticket>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // GET BY ID
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();

        _tickets
            .Setup(x => x.GetWithCommentsAndAttachmentsAsync(
                ticketId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var result = await service.GetByIdAsync(ticketId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerHasNoCompany_ReturnsNotFound()
    {
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetWithCommentsAndAttachmentsAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _currentUser
            .Setup(x => x.Role)
            .Returns("Customer");

        _currentUser
            .Setup(x => x.CompanyId)
            .Returns((Guid?)null);

        var service = CreateService();

        var result = await service.GetByIdAsync(ticket.Id);

        Assert.False(result.IsSuccess);
    }

    // ============================================================
    // SEARCH
    // ============================================================

    [Fact]
    public async Task SearchAsync_WhenRequestIsInvalid_ReturnsValidationError()
    {
        _searchValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<TicketSearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(InvalidValidation());

        var service = CreateService();

        var request = new TicketSearchRequest();

        var result = await service.SearchAsync(request);

        Assert.False(result.IsSuccess);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    [Fact]
    public async Task UpdateAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _updateValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<UpdateTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var request = new UpdateTicketRequest(
            "New title",
            "New description",
            TicketPriority.Low);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesAndSaves()
    {
        var ticket = CreateTicket();

        _updateValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<UpdateTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var request = new UpdateTicketRequest(
            "New title",
            "New description",
            TicketPriority.Low);

        var result = await service.UpdateAsync(
            ticket.Id,
            request);

        Assert.True(result.IsSuccess);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // ASSIGN
    // ============================================================

    [Fact]
    public async Task AssignAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _assignValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AssignTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var request = new AssignTicketRequest("agent-1");

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AssignAsync_WhenAssigneeIsNotAgentOrAdmin_ReturnsInvalidAssignee()
    {
        var ticket = CreateTicket();

        _assignValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AssignTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var customer = new UserInfo(
            "customer-1",
            "customer@test.com",
            "Test",
            "Customer",
            "Customer",
            null,
            true);

        _identityService
            .Setup(x => x.GetUserAsync(
                "customer-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(customer));

        var service = CreateService();

        var request = new AssignTicketRequest("customer-1");

        var result = await service.AssignAsync(
            ticket.Id,
            request);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TicketErrors.InvalidAssignee,
            result.Error);
    }

    [Fact]
    public async Task AssignAsync_WhenValid_SavesSuccessfully()
    {
        var ticket = CreateTicket();

        _assignValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AssignTicketRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _identityService
            .Setup(x => x.GetUserAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success(
                    new UserInfo(
                        "agent-1",
                        "agent@test.com",
                        "Test",
                        "Agent",
                        "Agent",
                        null,
                        true)));

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var request = new AssignTicketRequest("agent-1");

        var result = await service.AssignAsync(
            ticket.Id,
            request);

        Assert.True(result.IsSuccess);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // CLOSE
    // ============================================================

    [Fact]
    public async Task CloseAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _tickets
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var result = await service.CloseAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CloseAsync_WhenValid_ClosesAndSaves()
    {
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var result = await service.CloseAsync(ticket.Id);

        Assert.True(result.IsSuccess);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // REOPEN
    // ============================================================

    [Fact]
    public async Task ReopenAsync_WhenUserIsNotCustomer_ReturnsNotFound()
    {
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _currentUser
            .Setup(x => x.Role)
            .Returns("Agent");

        var service = CreateService();

        var result = await service.ReopenAsync(ticket.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ReopenAsync_WhenCustomerHasNoCompany_ReturnsNotFound()
    {
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _currentUser
            .Setup(x => x.Role)
            .Returns("Customer");

        _currentUser
            .Setup(x => x.CompanyId)
            .Returns((Guid?)null);

        var service = CreateService();

        var result = await service.ReopenAsync(ticket.Id);

        Assert.False(result.IsSuccess);
    }

    // ============================================================
    // ADD COMMENT
    // ============================================================

    [Fact]
    public async Task AddCommentAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _commentValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AddCommentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetWithCommentsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var request = new AddCommentRequest(
            "Test comment",
            false);

        var result = await service.AddCommentAsync(
            Guid.NewGuid(),
            request);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AddCommentAsync_WhenCustomerCreatesInternalComment_ReturnsError()
    {
        var ticket = CreateTicket();
        var companyId = Guid.NewGuid();

        _commentValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AddCommentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidValidation());

        _tickets
            .Setup(x => x.GetWithCommentsAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _currentUser
            .Setup(x => x.Role)
            .Returns("Customer");

        _currentUser
            .Setup(x => x.CompanyId)
            .Returns(companyId);

        _currentUser
            .Setup(x => x.UserId)
            .Returns(ticket.ReporterId);

        _identityService
            .Setup(x => x.GetUserAsync(
                ticket.ReporterId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success(
                    new UserInfo(
                        "agent-1",
                        "agent@test.com",
                        "Test",
                        "Agent",
                        "Agent",
                        null,
                        true)));

        var service = CreateService();

        var request = new AddCommentRequest(
            "Internal note",
            true);

        var result = await service.AddCommentAsync(
            ticket.Id,
            request);

        Assert.False(result.IsSuccess);
    }

    // ============================================================
    // GET COMMENTS
    // ============================================================

    [Fact]
    public async Task GetCommentsAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _tickets
            .Setup(x => x.GetWithCommentsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var result = await service.GetCommentsAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        _tickets
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenTicketExists_RemovesAndSaves()
    {
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var result = await service.DeleteAsync(ticket.Id);

        Assert.True(result.IsSuccess);

        _tickets.Verify(
            x => x.Remove(ticket),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}