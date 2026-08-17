using HelpDisk.Domain.Shared;

namespace HelpDisk.Domain.Categories;

/// <summary>
/// Every failure the Category feature can produce.
/// </summary>
public static class CategoryErrors
{
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "Category.NotFound",
        $"No category was found with id '{categoryId}'.");

    public static readonly Error NameRequired = Error.Validation(
        "Category.NameRequired",
        "A category must have a name.");

    public static readonly Error NameTooLong = Error.Validation(
        "Category.NameTooLong",
        $"A category name cannot exceed {Category.NameMaxLength} characters.");

    public static readonly Error NameAlreadyExists = Error.Conflict(
        "Category.NameAlreadyExists",
        "A category with this name already exists.");

    public static readonly Error CannotDeleteWithTickets = Error.Conflict(
    "Category.CannotDeleteWithTickets",
    "A category cannot be deleted while tickets are assigned to it.");
    public static readonly Error InvalidResponseTimeTarget = Error.Validation(
    "Category.InvalidResponseTimeTarget",
    "Response time target must be at least 1 hour.");
}


