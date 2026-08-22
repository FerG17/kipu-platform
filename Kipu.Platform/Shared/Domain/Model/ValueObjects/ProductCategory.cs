namespace Kipu.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>
///     Shared category vocabulary — used by both Product (its own catalog
///     category) and Supplier (what kind of products it supplies), see
///     architecture doc §5.7. Persisted as a plain string column, not a DB
///     enum, because it also supports free-text custom categories: when
///     <see cref="Other"/> is chosen and a label is typed in, that label is
///     persisted as the real category instead of the literal "OTHER" —
///     replicating the frontend behavior exactly (see category-options.js).
/// </summary>
public static class ProductCategory
{
    public const string Dairy = "DAIRY";
    public const string Grains = "GRAINS";
    public const string Oils = "OILS";
    public const string Beverages = "BEVERAGES";
    public const string Cleaning = "CLEANING";
    public const string Medicine = "MEDICINE";
    public const string Other = "OTHER";

    private static readonly string[] FixedValues =
        [Dairy, Grains, Oils, Beverages, Cleaning, Medicine, Other];

    /// <summary>True when the value is one of the fixed categories (including Other), false when it's a custom label.</summary>
    public static bool IsFixed(string category)
    {
        return FixedValues.Contains(category);
    }
}
