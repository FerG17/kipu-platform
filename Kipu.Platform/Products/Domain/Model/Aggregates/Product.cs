using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Domain.Model.Aggregates;

public static class ProductStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

/// <summary>
///     Whether a product is sold in whole units (a can, a bag) or by weight
///     (rice, cheese, sold "al peso"). Only Weight products may carry a
///     fractional Quantity in a sale/intake/adjustment — see
///     SaleCommandService and InventoryCommandService's whole-number guards
///     (X5 Bloque D).
/// </summary>
public static class ProductUnitOfSale
{
    public const string Unit = "UNIDAD";
    public const string Weight = "PESO";
}

/// <summary>
///     The product aggregate — a catalog item a business sells or stocks.
///     Category is a plain string (see Shared.ProductCategory): either one of
///     the fixed values, or a custom label the admin typed in when choosing
///     "Otros" — both are valid, equally-first-class category values.
/// </summary>
public class Product(int businessId, string name, string description, string category, decimal basePrice,
    string? barcode = null, string unitOfSale = ProductUnitOfSale.Unit)
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

    public string UnitOfSale { get; private set; } = unitOfSale;

    public bool IsActive => Status == ProductStatus.Active;
    public bool IsSoldByWeight => UnitOfSale == ProductUnitOfSale.Weight;

    public Product UpdateDetails(string name, string description, string category, decimal basePrice, string? barcode,
        string unitOfSale)
    {
        Name = name;
        Description = description;
        Category = category;
        BasePrice = basePrice;
        Barcode = barcode;
        UnitOfSale = unitOfSale;
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
