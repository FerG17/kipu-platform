namespace Bodega.Platform.Products.Domain.Model.Entities;

public static class StockMovementType
{
    public const string Intake = "INTAKE";
    public const string Sale = "SALE";
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
    string note)
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
    public string Supplier { get; private set; } = supplier;
    public string Note { get; private set; } = note;
    public DateTimeOffset RegisteredAt { get; private set; } = DateTimeOffset.UtcNow;
}
