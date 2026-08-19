namespace Kipu.Platform.Products.Domain.Model.Entities;

public static class StockMovementType
{
    public const string Intake = "INTAKE";
    public const string Sale = "SALE";

    /// <summary>
    ///     Goods coming back from a cancelled sale. Deliberately distinct
    ///     from Intake — booking a return as a purchase would show phantom
    ///     merchandise arriving in the entradas/salidas report.
    /// </summary>
    public const string Return = "RETURN";

    /// <summary>
    ///     A manual stock correction not tied to a sale — shrinkage, breakage,
    ///     theft, or fixing a physical count (I25). Signed: unlike every other
    ///     type here, Quantity itself carries the direction (negative removes
    ///     units, positive adds them) instead of the type alone implying it.
    /// </summary>
    public const string Adjustment = "ADJUSTMENT";

    /// <summary>Whether a movement added stock (true) or removed it (false) — Intake/Return always add, Sale always removes, Adjustment depends on its signed quantity.</summary>
    public static bool IsInbound(string type, int quantity)
    {
        return type switch
        {
            Sale => false,
            Adjustment => quantity >= 0,
            _ => true
        };
    }
}

/// <summary>
///     Append-only audit trail entry — created every time stock is added or
///     removed, never updated or deleted, so "why did stock change" always
///     has a real answer.
/// </summary>
public class StockMovement(
    int productId,
    int businessId,
    int warehouseId,
    int quantity,
    string type,
    string supplier,
    string note,
    int? supplierId = null)
{
    public StockMovement() : this(0, 0, 0, 0, StockMovementType.Intake, string.Empty, string.Empty)
    {
    }

    public int Id { get; }
    public int ProductId { get; private set; } = productId;
    public int BusinessId { get; private set; } = businessId;
    public int WarehouseId { get; private set; } = warehouseId;
    public int Quantity { get; private set; } = quantity;
    public string Type { get; private set; } = type;

    /// <summary>
    ///     The supplier's name as it stood when the goods arrived — kept for
    ///     display, so a historical movement still reads correctly even after
    ///     the supplier is renamed. Never use it to filter; that is what
    ///     SupplierId is for.
    /// </summary>
    public string Supplier { get; private set; } = supplier;

    /// <summary>
    ///     Stable reference to the supplier, for filtering reports.
    ///
    ///     Reports used to filter by matching the Supplier text above against
    ///     the supplier's current name, so renaming a supplier orphaned every
    ///     movement it had already produced and the report came back silently
    ///     empty. Null for movements with no supplier (sales, returns, manual
    ///     intakes) and for rows created before this column existed.
    /// </summary>
    public int? SupplierId { get; private set; } = supplierId;

    public string Note { get; private set; } = note;
    public DateTimeOffset RegisteredAt { get; private set; } = DateTimeOffset.UtcNow;
}
