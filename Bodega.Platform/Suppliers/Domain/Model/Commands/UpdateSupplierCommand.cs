namespace Bodega.Platform.Suppliers.Domain.Model.Commands;

public record UpdateSupplierCommand(
    int SupplierId,
    string Name,
    string LastName,
    string Ruc,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    string Category);
