using HelpDisk.Application.Features.Tickets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelpDisk.Infrastructure.Services;

public sealed class TicketSlaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TicketSlaBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var slaService = scope.ServiceProvider
                .GetRequiredService<TicketSlaService>();

            await slaService.CheckExpiredSlaAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }
}