using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Domain.Model.Aggregates;

public static class ProductStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

/// <summary>
///     The product aggregate — a catalog item a business sells or stocks.
///     Category is a plain string (see Shared.ProductCategory): either one of
///     the fixed values, or a custom label the admin typed in when choosing
///     "Otros" — both are valid, equally-first-class category values.
/// </summary>
public class Product(int businessId, string name, string description, string category, decimal basePrice,
    string? barcode = null)
{
    public Product() : this(0, string.Empty, string.Empty, ProductCategory.Other, 0)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public string Category { get; private set; } = category;
    public decimal BasePrice { get; private set; } = basePrice;
    public string Status { get; private set; } = ProductStatus.Active;

    /// <summary>
    ///     Optional — most of the catalog is still registered manually. Set the
    ///     first time a physical scan doesn't match any product (progressive
    ///     learning: unknown code → manual registration → remembered from then
    ///     on). Unique per business when present (see ProductCommandService).
    /// </summary>
    public string? Barcode { get; private set; } = barcode;

    public bool IsActive => Status == ProductStatus.Active;

    public Product UpdateDetails(string name, string description, string category, decimal basePrice, string? barcode)
    {
        Name = name;
        Description = description;
        Category = category;
        BasePrice = basePrice;
        Barcode = barcode;
        return this;
    }

    public Product Deactivate()
    {
        Status = ProductStatus.Inactive;
        return this;
    }

    public Product Activate()
    {
        Status = ProductStatus.Active;
        return this;
    }
}
