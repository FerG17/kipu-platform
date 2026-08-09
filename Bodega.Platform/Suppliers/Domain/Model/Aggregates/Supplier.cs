namespace Bodega.Platform.Suppliers.Domain.Model.Aggregates;

public static class SupplierStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

/// <summary>
///     A supplier is classified by the same Shared.ProductCategory vocabulary
///     Product uses (a supplier supplies a kind of product) — there is
///     deliberately no separate SupplierCategory enum (see architecture doc
///     §6.6: the frontend's original SupplierCategory was removed in favor
///     of this unification).
/// </summary>
public class Supplier(
    int businessId,
    string name,
    string lastName,
    string ruc,
    string email,
    string phone,
    string address,
    string contactPerson,
    string category,
    DateOnly since)
{
    public Supplier() : this(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, DateOnly.FromDateTime(DateTime.UtcNow))
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Name { get; private set; } = name;
    public string LastName { get; private set; } = lastName;
    public string Ruc { get; private set; } = ruc;
    public string Email { get; private set; } = email;
    public string Phone { get; private set; } = phone;
    public string Address { get; private set; } = address;
    public string ContactPerson { get; private set; } = contactPerson;
    public string Category { get; private set; } = category;
    public string Status { get; private set; } = SupplierStatus.Active;
    public DateOnly Since { get; private set; } = since;

    public Supplier UpdateDetails(string name, string lastName, string ruc, string email, string phone, string address,
        string contactPerson, string category)
    {
        Name = name;
        LastName = lastName;
        Ruc = ruc;
        Email = email;
        Phone = phone;
        Address = address;
        ContactPerson = contactPerson;
        Category = category;
        return this;
    }

    public Supplier Deactivate()
    {
        Status = SupplierStatus.Inactive;
        return this;
    }
}
