namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record CreateSupplierResource(
    string Name,
    string LastName,
    string Ruc,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    string Category);
