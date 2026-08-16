namespace Bodega.Platform.Products.Domain.Model.Entities;

/// <summary>
///     Links a Product to a Supplier it can be sourced from. A product may
///     have more than one — the owner explicitly wants "same product,
///     different supplier" representable (e.g. the usual supplier can't
///     deliver, so a replacement is tagged instead), so this is a real N:M,
///     not the single free-text "distributor" that used to be typed into
///     Product.Description.
///
///     SupplierId is a soft reference (no FK), same treatment as
///     StockMovement.SupplierId — Suppliers is a separate bounded context;
///     existence and business-ownership are checked in ProductCommandService
///     via ISupplierContextFacade before a link is ever created.
/// </summary>
public class ProductSupplier(int productId, int supplierId, int businessId)
{
    public ProductSupplier() : this(0, 0, 0)
    {
    }

    public int Id { get; }
    public int ProductId { get; private set; } = productId;
    public int SupplierId { get; private set; } = supplierId;
    public int BusinessId { get; private set; } = businessId;
}
