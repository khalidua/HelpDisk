namespace HelpDisk.Application.Abstractions;

public interface ITicketNumberGenerator
{
    Task<string> GenerateAsync(
        CancellationToken cancellationToken = default);
}