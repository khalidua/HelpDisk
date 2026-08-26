using System.Text;

using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Abstractions.Events;
using HelpDisk.Application.Features.Tickets;
using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Repositories;
using HelpDisk.Domain.Tickets;
using HelpDisk.Infrastructure.Identity;
using HelpDisk.Infrastructure.Persistence;
using HelpDisk.Infrastructure.Persistence.Interceptors;
using HelpDisk.Infrastructure.Persistence.Repositories;
using HelpDisk.Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace HelpDisk.Infrastructure;

/// <summary>
/// Registers the concrete implementations of the interfaces declared further
/// in.
/// </summary>
/// <remarks>
/// Read the AddScoped lines as sentences and the architecture states itself:
///
///     "When something asks for ITicketRepository (a Domain contract),
///      give it TicketRepository (an EF Core class)."
///
/// This method is the ONLY place where an inner interface is bound to an outer
/// implementation. That is what makes the swap real rather than theoretical -
/// to move to Dapper you change the right-hand sides here and nothing else
/// compiles differently.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not found. Check appsettings.json.");


        Console.WriteLine($"DEBUG CONNECTION STRING: [{connectionString}]");
        // Fail loudly and immediately on a missing connection string, rather
        // than letting the app start and fail on the first request. A
        // misconfigured app that refuses to boot is far easier to diagnose than
        // one that boots and then 500s.

        // ---- Interceptors ----------------------------------------------------
        // DomainEventsInterceptor is Scoped because it depends on the scoped
        // dispatcher, which resolves scoped handlers. The other two are
        // stateless, but must be registered as Scoped as well so they can be
        // resolved from the same provider used to build the DbContext options.
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventsInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);

            // ORDER IS SIGNIFICANT. EF runs interceptors in the order added:
            //
            //   1. SoftDelete  - rewrites Deleted -> Modified
            //   2. Auditable   - stamps ModifiedOnUtc, and therefore must run
            //                    AFTER the rewrite so it sees the new state
            //   3. DomainEvents - runs after the save completes
            //
            // Swap 1 and 2 and soft-deleted rows keep a stale ModifiedOnUtc.
            options.AddInterceptors(
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<DomainEventsInterceptor>());
        });

        // ---- Identity ---------------------------------------------------------
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();

        // ---- Repositories and unit of work -----------------------------------
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<HelpDisk.Domain.Companies.ICompanyRepository, CompanyRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ---- Services --------------------------------------------------------
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();

        services.AddHostedService<TicketSlaBackgroundService>();
        // NOTE: ICurrentUser is NOT registered here. Its implementation needs
        // IHttpContextAccessor, so it lives in the API layer - see
        // HelpDisk.API/Services/CurrentUser.cs. Infrastructure is not the only
        // layer allowed to satisfy an Application interface; whichever outer
        // layer naturally owns the dependency should.

        services.Configure<JwtOptions>(
        configuration.GetSection(JwtOptions.SectionName));

        services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtOptions = configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
                ?? throw new InvalidOperationException(
                    "JWT configuration was not found.");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };
        });

        return services;
    }
}
