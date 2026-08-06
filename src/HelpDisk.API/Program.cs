using System.Text.Json.Serialization;
using HelpDisk.API.Extensions;
using HelpDisk.API.Middleware;
using HelpDisk.API.Services;
using HelpDisk.Application;
using HelpDisk.Application.Abstractions;
using HelpDisk.Infrastructure;

namespace HelpDisk.API;

/// <summary>
/// The composition root: the one place that knows about every layer.
/// </summary>
/// <remarks>
/// ============================================================================
/// READ THE THREE REGISTRATION LINES BELOW AND THE ARCHITECTURE IS VISIBLE.
/// ============================================================================
///
///     builder.Services.AddApplication();                          // use cases
///     builder.Services.AddInfrastructure(builder.Configuration);  // the details
///     builder.Services.AddScoped&lt;ICurrentUser, CurrentUser&gt;();    // this layer's
///                                                                 //   contribution
///
/// Each layer packages its own registrations, so this file does not need to
/// know that TicketService exists or that ITicketRepository is satisfied by EF
/// Core. Adding a feature means editing that layer's DependencyInjection.cs and
/// leaving this file alone.
///
/// This is also the ONLY file (with MigrationExtensions) permitted to reference
/// HelpDisk.Infrastructure - see the note in HelpDisk.API.csproj. Wiring is a
/// composition-root job; controllers stay on Application interfaces.
/// </remarks>
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ====================================================================
        // SERVICES
        // ====================================================================

        // ---- The layers -----------------------------------------------------
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // ICurrentUser is implemented in THIS project because it needs
        // HttpContext. See Services/CurrentUser.cs for why that is correct
        // rather than a compromise.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        // ---- Web -------------------------------------------------------------
        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Serialise enums as names, not numbers: "High" instead of 3.
                // Numeric enums in JSON are a long-running source of client
                // bugs - the meaning of 3 changes the day somebody inserts a
                // value, and no client notices until it is wrong.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "HelpDisk API",
                Version = "v1",
                Description =
                    "A Clean Architecture + DDD teaching template. " +
                    "Create a category first, then a ticket that references it."
            });

            // Surfaces the XML doc comments from the controllers in Swagger UI,
            // so the explanations in this codebase are visible while exploring
            // the API rather than only when reading the source.
            var xmlPath = Path.Combine(
                AppContext.BaseDirectory,
                $"{typeof(Program).Assembly.GetName().Name}.xml");

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // ---- Error handling --------------------------------------------------
        // Catches UNEXPECTED exceptions only. Expected failures travel as
        // Result and are turned into 4xx by ApiController.
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // ====================================================================
        // PIPELINE
        // ====================================================================

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpDisk API v1");

                // Serve Swagger UI at the root so pressing F5 lands somewhere
                // useful instead of on a 404.
                options.RoutePrefix = string.Empty;
            });

            // Development only - see MigrationExtensions for why this must not
            // run in production.
            app.ApplyMigrations();
        }

        app.UseHttpsRedirection();

        // No UseAuthentication/UseAuthorization: this template has no auth.
        // See Services/CurrentUser.cs for what adding it would involve.

        app.MapControllers();

        app.Run();
    }
}
