namespace Kipu.Platform.Sales.Domain.Model.Commands;

/// <summary>Only meaningful transition today is PAID → CANCELLED (see SaleStatus).</summary>
public record UpdateSaleStatusCommand(int SaleId, string Status);
