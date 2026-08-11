using Bodega.Platform.Shared.Domain.Model.Entities;
using Bodega.Platform.Suppliers.Domain.Model.Entities;

namespace Bodega.Platform.Suppliers.Domain.Model.Aggregates;

public static class PurchaseOrderStatus
{
    public const string Pending = "PENDING";
    public const string Received = "RECEIVED";
    public const string Delayed = "DELAYED";
    public const string Cancelled = "CANCELLED";
}

/// <summary>
///     A purchase order placed with a supplier. Moving to RECEIVED is the
///     one status transition with a real side effect: it triggers an actual
///     stock intake for every line (see PurchaseOrderCommandService), distinct
///     from a manual inventory intake — this distinction already exists as
///     help text in the frontend and is only formalized here.
/// </summary>
public class PurchaseOrder : IVersionedEntity
{
    private readonly List<PurchaseOrderDetail> _details = [];

    public PurchaseOrder(int businessId, int supplierId, DateOnly date, DateOnly? expectedDate, string currency, string description)
    {
        BusinessId = businessId;
        SupplierId = supplierId;
        Date = date;
        ExpectedDate = expectedDate;
        Currency = currency;
        Description = description;
        Status = PurchaseOrderStatus.Pending;
    }

    public PurchaseOrder()
    {
        Currency = string.Empty;
        Description = string.Empty;
        Status = PurchaseOrderStatus.Pending;
    }

    public int Id { get; }
    public int BusinessId { get; private set; }
    public int SupplierId { get; private set; }
    public DateOnly Date { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public DateOnly? ReceivedDate { get; private set; }
    public string Status { get; private set; }
    public string Currency { get; private set; }
    public string Description { get; private set; }

    /// <summary>
    ///     Optimistic-concurrency token — what stops one delivery being booked
    ///     into inventory several times when RECEIVED is submitted twice at
    ///     once. See <see cref="IVersionedEntity" />.
    /// </summary>
    public int Version { get; set; }

    public IReadOnlyCollection<PurchaseOrderDetail> Details => _details.AsReadOnly();

    public PurchaseOrder AddLine(int productId, int quantity, decimal unitPrice, decimal discount)
    {
        _details.Add(new PurchaseOrderDetail(Id, productId, quantity, unitPrice, discount, string.Empty, string.Empty));
        return this;
    }

    public PurchaseOrder MarkReceived(DateOnly receivedDate)
    {
        Status = PurchaseOrderStatus.Received;
        ReceivedDate = receivedDate;
        return this;
    }

    public PurchaseOrder MarkDelayed()
    {
        Status = PurchaseOrderStatus.Delayed;
        return this;
    }

    public PurchaseOrder Cancel()
    {
        Status = PurchaseOrderStatus.Cancelled;
        return this;
    }
}
