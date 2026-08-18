using System.Reflection;

using FluentValidation;

using HelpDisk.Application.Abstractions.Events;
using HelpDisk.Application.Features.Auth;
using HelpDisk.Application.Features.Categories;
using HelpDisk.Application.Features.Tickets;
using HelpDisk.Application.Features.Tickets.EventHandlers;

using Mapster;

using MapsterMapper;

using Microsoft.Extensions.DependencyInjection;

namespace HelpDisk.Application;

/// <summary>
/// Registers everything the Application layer provides.
/// </summary>
/// <remarks>
/// Each layer owns its own registration method and Program.cs calls them in
/// order. The alternative - one giant ConfigureServices in Program.cs that
/// knows every type in the system - makes the composition root the one file
/// that must change whenever anything anywhere changes.
///
/// Notice what is NOT registered here: no ITicketRepository, no IUnitOfWork, no
/// ICurrentUser. Those are interfaces this layer CONSUMES, and Infrastructure
/// registers the implementations. This method registers only what Application
/// itself supplies.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ---- Feature services ----------------------------------------------
        // Scoped: one instance per HTTP request, sharing the request's
        // DbContext. Singleton would be a bug - the DbContext would outlive the
        // request and start handing back stale, cross-request tracked entities.
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<TicketSlaService>();
        // ---- Validators ------------------------------------------------------
        // Scans for every AbstractValidator<T> in this assembly, so adding a
        // validator needs no change here. One of the few places where scanning
        // beats explicit registration: validators are numerous, uniform, and
        // have no configuration to get wrong.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // ---- Mapster ---------------------------------------------------------
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;

        // Finds every IRegister (e.g. TicketMappingConfig) and applies it.
        typeAdapterConfig.Scan(assembly);

        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // ---- Domain event handlers -------------------------------------------
        // Registered explicitly rather than scanned. There are few of them, and
        // "which code runs when a ticket is assigned?" should be answerable by
        // reading a list rather than by trusting a scanner - it is exactly the
        // kind of indirection that makes event-driven code hard to follow.
        services.AddScoped<
            IDomainEventHandler<Domain.Tickets.Events.TicketAssignedDomainEvent>,
            TicketAssignedLoggingHandler>();

        return services;
    }
}
