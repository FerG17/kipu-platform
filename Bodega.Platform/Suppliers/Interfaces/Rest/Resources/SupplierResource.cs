namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record SupplierResource(
    int Id,
    int BusinessId,
    string Name,
    string LastName,
    string Ruc,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    string Category,
    string Status,
    DateOnly Since);
