namespace HelpDisk.Application.Abstractions;

/// <summary>
/// Who is performing the current operation.
/// </summary>
/// <remarks>
/// TicketService needs to know who is acting - a ticket records its reporter,
/// a comment records its author. The obvious way to get that is
/// HttpContext.User.Claims. The obvious way is wrong here, because it would
/// make Application depend on ASP.NET Core, and then the layer could only ever
/// run inside a web request.
///
/// So Application declares WHAT it needs (an id and a name) and lets
/// Infrastructure decide WHERE that comes from. The web app reads claims. A
/// background job could return a system account. A test returns whatever the
/// test wants, with no HttpContext in sight.
///
/// This is the same inversion as ITicketRepository, applied to an ambient value
/// instead of storage - and it is the more commonly botched of the two. Reading
/// HttpContext deep inside a service is one of the most common ways a codebase
/// becomes untestable without anybody deciding to make it so.
///
/// NOTE FOR THIS TEMPLATE: there is no authentication here (no login endpoint,
/// no token validation - see docs/architecture.md). CurrentUser in
/// Infrastructure reads claims if they exist and otherwise returns a fixed demo
/// user, so every endpoint works out of the box. Wiring up real JWT auth means
/// changing that ONE class. Nothing in Application or Domain changes at all -
/// which is the point being demonstrated.
/// </remarks>
public interface ICurrentUser
{
    string UserId { get; }

    string UserName { get; }
}
