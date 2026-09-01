namespace Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

public record CreateSupplierPaymentPlanResource(int PurchaseOrderId, IReadOnlyList<SupplierInstallmentScheduleLineResource> Schedule);

public record SupplierInstallmentScheduleLineResource(DateOnly DueDate, decimal Amount);
