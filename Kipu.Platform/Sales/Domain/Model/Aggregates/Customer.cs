namespace Kipu.Platform.Sales.Domain.Model.Aggregates;

public static class CustomerStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

public class Customer(int businessId, string fullName, string documentNumber, string phoneNumber, string email)
{
    public Customer() : this(0, string.Empty, string.Empty, string.Empty, string.Empty)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string FullName { get; private set; } = fullName;
    public string DocumentNumber { get; private set; } = documentNumber;
    public string PhoneNumber { get; private set; } = phoneNumber;
    public string Email { get; private set; } = email;
    public string Status { get; private set; } = CustomerStatus.Active;
    public DateTimeOffset RegisteredAt { get; private set; } = DateTimeOffset.UtcNow;

    public bool IsActive => Status == CustomerStatus.Active;

    public Customer UpdateDetails(string fullName, string documentNumber, string phoneNumber, string email)
    {
        FullName = fullName;
        DocumentNumber = documentNumber;
        PhoneNumber = phoneNumber;
        Email = email;
        return this;
    }

    /// <summary>
    ///     Soft delete (I31) — a physical DELETE let Sale.CustomerId silently
    ///     go to NULL (SetNull on delete), quietly severing a real sale's
    ///     "who bought this" attribution the moment its customer was removed.
    ///     Deactivating keeps the row, and with it every sale/payment plan
    ///     that already points at it, intact.
    /// </summary>
    public Customer Deactivate()
    {
        Status = CustomerStatus.Inactive;
        return this;
    }
}
