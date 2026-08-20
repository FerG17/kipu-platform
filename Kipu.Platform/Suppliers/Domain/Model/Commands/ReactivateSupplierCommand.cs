namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

/// <summary>X4 M11: undoes DeactivateSupplierCommand.</summary>
public record ReactivateSupplierCommand(int SupplierId);
