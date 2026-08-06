using HelpDisk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.API.Extensions;

/// <summary>
/// Applies pending EF Core migrations at startup.
/// </summary>
/// <remarks>
/// ============================================================================
/// A DELIBERATE DEPARTURE FROM THE MOJ REFERENCE - worth understanding.
/// ============================================================================
///
/// MOJ does this from inside its service-registration method
/// (Bootstrap.EnsureDatabaseInitializationAndUpToDate), roughly:
///
///     using var serviceProvider = services.BuildServiceProvider();   // (1)
///     var dbContext = serviceProvider.GetRequiredService&lt;AppDbContext&gt;();
///     try { ... dbContext.Database.Migrate(); }
///     catch (Exception ex) { logger.LogError(ex.Message); }          // (2)
///
/// Two real problems:
///
///   (1) BuildServiceProvider() during registration builds a SECOND, throwaway
///       container. Every singleton registered so far is instantiated twice -
///       once here, once in the real container - so any singleton holding state
///       or a connection now exists in duplicate. The ASP.NET Core team
///       considers this enough of a trap to ship an analyzer warning for it
///       (ASP0000).
///
///   (2) Swallowing the exception means a failed migration is a logged line and
///       nothing else. The application starts, reports healthy, serves traffic,
///       and fails on the first query against a table that was never created.
///       Loud failure at startup is strictly better than quiet failure later.
///
/// The version below runs AFTER the app is built, from a proper scope, and lets
/// exceptions propagate - a broken database means the app does not start.
///
/// ============================================================================
/// AND A WARNING ABOUT THIS PATTERN GENERALLY
/// ============================================================================
///
/// Auto-migrating at startup is a CONVENIENCE FOR DEVELOPMENT, which is why
/// Program.cs only calls it when the environment is Development.
///
/// Do not do this in production. Two instances starting at once will both try
/// to migrate, and there is no way to review or roll back a migration that has
/// already run. In production, migrations belong in a controlled deployment
/// step - `dotnet ef database update` from a pipeline, or a generated SQL
/// script a DBA has read.
/// </remarks>
public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        // A scope is required because AppDbContext is registered Scoped and the
        // root provider has no scope of its own.
        using var scope = app.ApplicationServices.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Creates the database if missing, then applies every pending
        // migration. No try/catch: if this fails, startup should fail.
        dbContext.Database.Migrate();
    }
}
