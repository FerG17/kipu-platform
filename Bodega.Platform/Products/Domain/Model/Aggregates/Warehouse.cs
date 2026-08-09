namespace Bodega.Platform.Products.Domain.Model.Aggregates;

public static class WarehouseStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

public static class WarehouseCapacity
{
    public const string Small = "SMALL";
    public const string Medium = "MEDIUM";
    public const string Large = "LARGE";
}

/// <summary>
///     The warehouse aggregate — a storage location belonging to a business.
///     Every product must be assigned to a real warehouse; there is no
///     "unassigned" state (see InventoryItem).
/// </summary>
public class Warehouse(int businessId, string name, string code, string address, string capacity)
{
    public Warehouse() : this(0, string.Empty, string.Empty, string.Empty, WarehouseCapacity.Medium)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Name { get; private set; } = name;
    public string Code { get; private set; } = code;
    public string Address { get; private set; } = address;
    public string Status { get; private set; } = WarehouseStatus.Active;
    public string Capacity { get; private set; } = capacity;

    public Warehouse UpdateDetails(string name, string code, string address, string capacity)
    {
        Name = name;
        Code = code;
        Address = address;
        Capacity = capacity;
        return this;
    }

    public Warehouse Deactivate()
    {
        Status = WarehouseStatus.Inactive;
        return this;
    }

    public Warehouse Activate()
    {
        Status = WarehouseStatus.Active;
        return this;
    }
}
