namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

public record CreateSupplierCommand(
    int BusinessId,
    string Name,
    string LastName,
    string Ruc,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    string Category);
