namespace Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

public record UpdateSupplierPaymentInstallmentResource(DateOnly DueDate, decimal Amount);
