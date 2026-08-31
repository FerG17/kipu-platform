namespace Kipu.Platform.Products.Domain.Model.Aggregates;

/// <summary>
///     A business's own catalog of product categories (X6 #5) — replaces the
///     old "type anything under Otros" escape hatch with real, listable rows
///     the admin builds up via a quick inline "+" in the product form.
///     Product.Category stays a plain string (see Shared.ProductCategory):
///     this table is the source of truth for which names are offered in the
///     picker, not a new foreign key on Product.
/// </summary>
public class Category(int businessId, string name)
{
    public Category() : this(0, string.Empty)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Name { get; private set; } = name;
}
