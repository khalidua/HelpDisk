using HelpDisk.Application.Abstractions;
using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Companies;
using HelpDisk.Domain.Tickets;
using HelpDisk.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with realistic test data for every role and scenario.
/// </summary>
/// <remarks>
/// ============================================================================
/// WHAT THIS SEEDER COVERS
/// ============================================================================
///
/// After running docker compose up, the seeder creates:
///
///   Users (all passwords: "Test123!")
///   ─────────────────────────────────
///   agent1@helpdisk.com    – Agent
///   agent2@helpdisk.com    – Agent
///   customer1@helpdisk.com – Customer (TechCorp)
///   customer2@helpdisk.com – Customer (TechCorp)
///   customer3@helpdisk.com – Customer (Retail Ltd)
///
///   Categories
///   ──────────
///   Hardware (4h SLA), Network (2h SLA), Software (8h SLA),
///   Access Request (24h SLA), Security (1h SLA)
///
///   Companies
///   ─────────
///   TechCorp, Retail Ltd
///
///   Tickets  (15 across the full status/priority matrix)
///   ──────────────────────────────────────────────────
///   New, InProgress, Closed × Low/Normal/High/Urgent
///   Some with comments, some with SLA deadlines already set.
///
/// The seeder is IDEMPOTENT: every block checks before it inserts, so
/// running migrations a second time never duplicates data.
/// </remarks>
public static class DataSeeder
{
    // ── Public entry point ────────────────────────────────────────────────────

    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITicketNumberGenerator ticketNumberGenerator)
    {
        var companyIds  = await SeedCompaniesAsync(db);
        var categoryIds = await SeedCategoriesAsync(db);
        var userIds     = await SeedUsersAsync(userManager, companyIds);
        await SeedTicketsAsync(db, userIds, categoryIds, ticketNumberGenerator);
    }

    // ── Companies ─────────────────────────────────────────────────────────────

    private record CompanyIds(Guid TechCorpId, Guid RetailLtdId);

    private static async Task<CompanyIds> SeedCompaniesAsync(AppDbContext db)
    {
        // Idempotent: if companies already exist, read their IDs from the DB.
        var existing = await db.Set<Company>()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        if (existing.Count >= 2)
        {
            return new CompanyIds(
                existing.First(c => c.Name == "TechCorp").Id,
                existing.First(c => c.Name == "Retail Ltd").Id);
        }

        // Let the domain generate IDs naturally — no reflection tricks needed.
        var techCorp  = new Company("TechCorp");
        var retailLtd = new Company("Retail Ltd");

        db.Set<Company>().AddRange(techCorp, retailLtd);
        await db.SaveChangesAsync();

        return new CompanyIds(techCorp.Id, retailLtd.Id);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    private record CategoryIds(
        Guid Hardware,
        Guid Network,
        Guid Software,
        Guid Access,
        Guid Security);

    private static async Task<CategoryIds> SeedCategoriesAsync(AppDbContext db)
    {
        // Idempotent: if categories already exist, read their IDs from the DB.
        var existing = await db.Categories
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        if (existing.Count >= 5)
        {
            return new CategoryIds(
                existing.First(c => c.Name == "Hardware").Id,
                existing.First(c => c.Name == "Network").Id,
                existing.First(c => c.Name == "Software").Id,
                existing.First(c => c.Name == "Access Request").Id,
                existing.First(c => c.Name == "Security").Id);
        }

        // Let Category.Create() generate its own Guid — that is what it is for.
        var definitions = new[]
        {
            ("Hardware",       4),
            ("Network",         2),
            ("Software",        8),
            ("Access Request", 24),
            ("Security",        1),
        };

        var created = new List<Category>();
        foreach (var (name, sla) in definitions)
        {
            var result = Category.Create(name, sla);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed category '{name}': {result.Error}");
            }

            created.Add(result.Value);
            db.Categories.Add(result.Value);
        }

        await db.SaveChangesAsync();

        return new CategoryIds(
            created[0].Id,
            created[1].Id,
            created[2].Id,
            created[3].Id,
            created[4].Id);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    private record UserIds(
        string Agent1,
        string Agent2,
        string Customer1,
        string Customer2,
        string Customer3);

    private static async Task<UserIds> SeedUsersAsync(
        UserManager<AppUser> userManager,
        CompanyIds companyIds)
    {
        const string password = "Test123!";

        var agent1    = await EnsureUserAsync(userManager, "agent1@helpdisk.com",    "Alex",   "Morgan",  "Agent",    null,                  password);
        var agent2    = await EnsureUserAsync(userManager, "agent2@helpdisk.com",    "Sam",    "Rivera",  "Agent",    null,                  password);
        var customer1 = await EnsureUserAsync(userManager, "customer1@helpdisk.com", "Jordan", "Lee",     "Customer", companyIds.TechCorpId, password);
        var customer2 = await EnsureUserAsync(userManager, "customer2@helpdisk.com", "Taylor", "Smith",   "Customer", companyIds.TechCorpId, password);
        var customer3 = await EnsureUserAsync(userManager, "customer3@helpdisk.com", "Casey",  "Johnson", "Customer", companyIds.RetailLtdId, password);

        return new UserIds(agent1, agent2, customer1, customer2, customer3);
    }

    private static async Task<string> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string firstName,
        string lastName,
        string role,
        Guid? companyId,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing.Id;
        }

        var user = new AppUser
        {
            UserName       = email,
            Email          = email,
            EmailConfirmed = true,
            FirstName      = firstName,
            LastName       = lastName,
            CompanyId      = companyId,
            IsActive       = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user '{email}': " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign role '{role}' to '{email}': " +
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    // ── Tickets ───────────────────────────────────────────────────────────────

    private static async Task SeedTicketsAsync(
        AppDbContext db,
        UserIds ids,
        CategoryIds catIds,
        ITicketNumberGenerator ticketNumberGenerator)
    {
        if (await db.Tickets.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Helper to avoid repetitive boilerplate below.
        // The ticket number is generated by the application's sequence-backed
        // generator so seeded records appear in the real numbering stream.
        async Task Add(
            string title,
            string description,
            TicketPriority priority,
            Guid categoryId,
            string reporterId,
            string? assigneeId           = null,
            bool close                   = false,
            DateTime? deadline           = null,
            string? commentBody          = null,
            string? commentAuthorId      = null,
            bool commentInternal         = false)
        {
            var number = await ticketNumberGenerator.GenerateAsync();

            var result = Ticket.Create(number, title, description, priority, categoryId, reporterId);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed ticket '{number}': {result.Error}");
            }

            var ticket = result.Value;

            if (deadline.HasValue)
            {
                ticket.SetResponseDeadline(deadline.Value);
            }

            if (assigneeId is not null)
            {
                ticket.Assign(assigneeId);
            }

            if (close)
            {
                ticket.Close();
            }

            if (commentBody is not null && commentAuthorId is not null)
            {
                ticket.AddComment(commentBody, commentAuthorId, commentInternal);
            }

            db.Tickets.Add(ticket);
        }

        // ── NEW tickets (unassigned) ──────────────────────────────────────────
        // Each Add() call is awaited so the sequence increments in order.

        await Add(
            title:       "Laptop screen flickering",
            description: "My laptop screen starts flickering after 10 minutes of use. Hard to read anything. Model: Dell XPS 15.",
            priority:    TicketPriority.High,
            categoryId:  catIds.Hardware,
            reporterId:  ids.Customer1,
            deadline:    now.AddHours(4));

        await Add(
            title:       "Unable to connect to VPN",
            description: "VPN client throws 'authentication failed' since yesterday morning. Already reinstalled the client.",
            priority:    TicketPriority.Urgent,
            categoryId:  catIds.Network,
            reporterId:  ids.Customer2,
            deadline:    now.AddHours(2));

        await Add(
            title:       "Request access to SharePoint project site",
            description: "Need read access to the Q3 Planning SharePoint site for the upcoming audit. Manager approved via email.",
            priority:    TicketPriority.Normal,
            categoryId:  catIds.Access,
            reporterId:  ids.Customer3,
            deadline:    now.AddHours(24));

        await Add(
            title:       "Excel formula returning wrong result",
            description: "VLOOKUP in the monthly report sheet returns #N/A for items that definitely exist. File attached separately.",
            priority:    TicketPriority.Low,
            categoryId:  catIds.Software,
            reporterId:  ids.Customer1);

        // ── IN-PROGRESS tickets (assigned to agents) ──────────────────────────

        await Add(
            title:           "Printer not detected on 3rd floor",
            description:     "HP LaserJet on the 3rd floor is invisible to all workstations. Tried rebooting the print server.",
            priority:        TicketPriority.Normal,
            categoryId:      catIds.Hardware,
            reporterId:      ids.Customer2,
            assigneeId:      ids.Agent1,
            deadline:        now.AddHours(3),
            commentBody:     "Checked the print spooler service — it was stopped. Restarted it. Still not visible. Escalating to network check.",
            commentAuthorId: ids.Agent1,
            commentInternal: true);

        await Add(
            title:           "Network drive disconnecting randomly",
            description:     "The Z: drive disconnects every few hours. Auto-reconnect fails. Affects the whole Accounts team.",
            priority:        TicketPriority.High,
            categoryId:      catIds.Network,
            reporterId:      ids.Customer3,
            assigneeId:      ids.Agent1,
            deadline:        now.AddHours(2),
            commentBody:     "Investigating the SMB session timeout settings on the file server. Will update within 2 hours.",
            commentAuthorId: ids.Agent1);

        await Add(
            title:           "Outlook keeps asking for password",
            description:     "Outlook 365 prompts for credentials every reboot. Modern authentication is enabled on the tenant.",
            priority:        TicketPriority.High,
            categoryId:      catIds.Software,
            reporterId:      ids.Customer1,
            assigneeId:      ids.Agent2,
            commentBody:     "Reproduced locally. Likely stale credentials in Windows Credential Manager. Sending instructions now.",
            commentAuthorId: ids.Agent2);

        await Add(
            title:           "Suspicious login alert — possible account compromise",
            description:     "Received an alert for a login from an unrecognised country. The user did not travel.",
            priority:        TicketPriority.Urgent,
            categoryId:      catIds.Security,
            reporterId:      ids.Customer2,
            assigneeId:      ids.Agent2,
            deadline:        now.AddHours(1),
            commentBody:     "Session revoked and MFA enforced. Awaiting user confirmation before re-enabling the account.",
            commentAuthorId: ids.Agent2,
            commentInternal: true);

        await Add(
            title:       "Software licence expiry — Adobe Creative Cloud",
            description: "Adobe CC licences for the Design team expire in 3 days. Renewal submitted to procurement; IT enablement needed.",
            priority:    TicketPriority.Normal,
            categoryId:  catIds.Access,
            reporterId:  ids.Customer3,
            assigneeId:  ids.Agent1);

        await Add(
            title:       "Wi-Fi drops on floor 2",
            description: "Multiple staff on floor 2 report Wi-Fi dropping for 30–60 seconds every hour. Wired users unaffected.",
            priority:    TicketPriority.High,
            categoryId:  catIds.Network,
            reporterId:  ids.Customer1,
            assigneeId:  ids.Agent2,
            deadline:    now.AddHours(2));

        // ── CLOSED tickets ────────────────────────────────────────────────────

        await Add(
            title:           "Keyboard unresponsive after Windows update",
            description:     "After last Tuesday's update the keyboard driver stopped loading. Rolled back manually.",
            priority:        TicketPriority.Normal,
            categoryId:      catIds.Hardware,
            reporterId:      ids.Customer2,
            assigneeId:      ids.Agent1,
            close:           true,
            commentBody:     "Driver reinstalled via Device Manager. Issue resolved. Closing the ticket.",
            commentAuthorId: ids.Agent1);

        await Add(
            title:           "Email delivery failure to external domain",
            description:     "Emails to @partner.com bounce with 'SPF check failed'. No recent DNS changes.",
            priority:        TicketPriority.Urgent,
            categoryId:      catIds.Network,
            reporterId:      ids.Customer3,
            assigneeId:      ids.Agent2,
            close:           true,
            commentBody:     "SPF record was missing the new mail relay IP. Updated and propagated. Delivery confirmed.",
            commentAuthorId: ids.Agent2);

        await Add(
            title:       "New starter laptop setup",
            description: "Standard onboarding build for new starter starting Monday: Office 365, VPN, and badge printing software.",
            priority:    TicketPriority.Normal,
            categoryId:  catIds.Access,
            reporterId:  ids.Customer1,
            assigneeId:  ids.Agent1,
            close:       true);

        await Add(
            title:           "Monitor not powering on",
            description:     "Second monitor on desk 4B is completely dead. Power LED does not illuminate. Tried two power cables.",
            priority:        TicketPriority.Low,
            categoryId:      catIds.Hardware,
            reporterId:      ids.Customer2,
            assigneeId:      ids.Agent2,
            close:           true,
            commentBody:     "Replaced with a spare unit. Faulty monitor logged for disposal. Confirmed working.",
            commentAuthorId: ids.Agent2);

        await Add(
            title:           "Password reset — locked out account",
            description:     "Account locked after too many failed attempts. User cannot use self-service portal (MFA device lost).",
            priority:        TicketPriority.High,
            categoryId:      catIds.Security,
            reporterId:      ids.Customer3,
            assigneeId:      ids.Agent1,
            close:           true,
            commentBody:     "Identity verified via manager approval. Password reset and new MFA device enrolled. Account active.",
            commentAuthorId: ids.Agent1);

        await db.SaveChangesAsync();
    }
}
