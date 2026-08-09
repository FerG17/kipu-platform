namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record UpdateSupplierResource(
    string Name,
    string LastName,
    string Ruc,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    string Category);
