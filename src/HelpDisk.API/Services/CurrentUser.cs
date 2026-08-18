using System.Security.Claims;
using HelpDisk.Application.Abstractions;
namespace HelpDisk.API.Services;

/// <summary>
/// Reads the acting user from the current HTTP request.
/// </summary>
/// <remarks>
/// ============================================================================
/// WHY IS THIS IN THE API PROJECT AND NOT IN INFRASTRUCTURE?
/// ============================================================================
///
/// Because it needs IHttpContextAccessor, and HttpContext is an ASP.NET Core
/// concept. Putting this class in Infrastructure would drag the entire
/// ASP.NET Core framework reference into a project whose job is talking to
/// databases.
///
/// The useful generalisation for students: "Infrastructure implements the
/// interfaces" is a rule of thumb, not a law. The real rule is that an
/// interface declared in an inner layer is implemented by whichever OUTER layer
/// naturally owns the dependency. Storage concerns land in Infrastructure.
/// Request-scoped concerns land in the API. Both are outside Application, which
/// is all the dependency rule actually requires.
///
/// ============================================================================
/// NO AUTHENTICATION IN THIS TEMPLATE
/// ============================================================================
///
/// There is no login endpoint, no token validation, no identity provider. When
/// no user is authenticated this returns a fixed demo identity so every
/// endpoint works the moment you press F5.
///
/// THIS IS NOT PRODUCTION BEHAVIOUR. In a real system an unauthenticated
/// request must be rejected by the authentication middleware long before it
/// reaches here, and this class should throw rather than invent a user.
///
/// To make it real: add JWT bearer authentication in Program.cs, put
/// [Authorize] on the controllers, and delete the fallback below. That is the
/// entire change. Nothing in Application or Domain moves - which is the point
/// being demonstrated. TicketService asks "who is acting?" and does not care
/// how the answer is obtained.
/// </remarks>
public sealed class CurrentUser : ICurrentUser
{
    /// <summary>Used when no authenticated user is present.</summary>

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public string UserName =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? throw new UnauthorizedAccessException();

    public string Role =>
    _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
    ?? throw new UnauthorizedAccessException();

    public Guid? CompanyId => throw new NotImplementedException();
}
