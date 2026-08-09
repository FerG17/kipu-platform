namespace Bodega.Platform.Sales.Domain.Model.Aggregates;

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
    public DateTimeOffset RegisteredAt { get; private set; } = DateTimeOffset.UtcNow;

    public Customer UpdateDetails(string fullName, string documentNumber, string phoneNumber, string email)
    {
        FullName = fullName;
        DocumentNumber = documentNumber;
        PhoneNumber = phoneNumber;
        Email = email;
        return this;
    }
}
