namespace HelpDisk.Domain.Categories;

/// <summary>
/// How the application reaches stored categories.
/// </summary>
/// <remarks>
/// Small, like the aggregate it serves. A lookup table needs three operations,
/// so it declares three - not the twenty a generic repository would hand it.
/// </remarks>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasTicketsAsync(Guid categoryId,CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Used to enforce unique names. Note this is a QUESTION about the whole
    /// collection, which no single Category instance can answer - so it belongs
    /// on the repository, not on the entity.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Remove(Category category);
}
