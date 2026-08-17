using System.Text.Json.Serialization;

using HelpDisk.API.Extensions;
using HelpDisk.API.Middleware;
using HelpDisk.API.Services;

using HelpDisk.Application;
using HelpDisk.Application.Abstractions;

using HelpDisk.Infrastructure;

using Microsoft.OpenApi;

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
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
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

            // ---- JWT authentication -----------------------------------------
            // Swagger needs to know that this API uses Bearer tokens so it can
            // display the Authorize button and send the JWT in the
            // Authorization header when calling protected endpoints.
            //
            // This does NOT perform authentication itself. ASP.NET Core's
            // JwtBearer middleware, registered by Infrastructure, is still
            // responsible for validating the token.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
                options.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    "HelpDisk API v1");

                // Serve Swagger UI at the root so pressing F5 lands somewhere
                // useful instead of on a 404.
                options.RoutePrefix = string.Empty;
            });

            // Development only - see MigrationExtensions for why this must not
            // run in production.
            app.ApplyMigrations();
        }

        app.UseHttpsRedirection();

        // Authentication reads and validates the JWT and populates
        // HttpContext.User with its claims.
        app.UseAuthentication();

        // Authorization checks policies and roles such as [Authorize] and
        // [Authorize(Roles = "Admin")].
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}