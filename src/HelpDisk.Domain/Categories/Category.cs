using HelpDisk.Domain.Primitives;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Domain.Categories;

/// <summary>
/// A bucket tickets are filed under - "Hardware", "Network", "Access Request".
/// </summary>
/// <remarks>
/// ============================================================================
/// THIS CLASS IS DELIBERATELY BORING. That is its lesson.
/// ============================================================================
///
/// Compare it with Ticket next door:
///
///                         Ticket                      Category
///   Base class            AggregateRoot<Guid>         Entity<Guid>
///   Domain events         yes                         none
///   Child entities        Comments                    none
///   State machine         New/InProgress/Closed       none
///   Behaviour methods     Assign, Close, Reopen...    Rename
///   Soft delete           yes                         no
///
/// A category is a lookup value. It has one rule - a name, not blank - and
/// nothing that can go wrong later. So it gets a factory to enforce that one
/// rule, and nothing else.
///
/// It is NOT an AggregateRoot because it raises no events and owns no children;
/// inheriting from AggregateRoot would add an unused event list to every row.
/// It is still referenced by id from Ticket.CategoryId, and never by a
/// navigation property - referencing by identity is the right habit whether or
/// not the target is a formal aggregate root.
///
/// WHY THIS MATTERS FOR TEACHING: the most common way DDD goes wrong is
/// applying the full ceremony to everything, so that a table of five lookup
/// values acquires a factory, three events, a specification and a repository
/// nobody needed. Then the team concludes DDD is bureaucracy, and they are
/// right - about what they built.
///
/// Match the machinery to the complexity of the rules. Most entities are
/// Categories. A few are Tickets. You will know which because the Tickets are
/// the ones people argue about in meetings.
/// </remarks>
public sealed class Category : Entity<Guid>
{
    public const int NameMaxLength = 100;

    /// <summary>Required by EF Core.</summary>
    private Category()
    {
    }

    private Category(Guid id, string name, int responseTimeTargetHours)
        : base(id)
    {
        Name = name;
        ResponseTimeTargetHours = responseTimeTargetHours;
    }

    public string Name { get; private set; } = null!;
    public int ResponseTimeTargetHours { get; private set; }
    public static Result<Category> Create(string name, int responseTimeTargetHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return CategoryErrors.NameTooLong;
        }

        if(responseTimeTargetHours < 0)
        {
            return CategoryErrors.InvalidResponseTimeTarget;
        }

        return new Category(Guid.NewGuid(), name.Trim(), responseTimeTargetHours);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return CategoryErrors.NameTooLong;
        }

        Name = name.Trim();

        return Result.Success();
    }

    public Result UpdateDetails(
    string name,
    int responseTimeTargetHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return CategoryErrors.NameTooLong;
        }

        if (responseTimeTargetHours < 0)
        {
            return CategoryErrors.InvalidResponseTimeTarget;
        }

        Name = name.Trim();
        ResponseTimeTargetHours = responseTimeTargetHours;

        return Result.Success();
    }
}
