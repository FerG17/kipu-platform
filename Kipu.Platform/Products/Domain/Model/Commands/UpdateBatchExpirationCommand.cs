namespace Kipu.Platform.Products.Domain.Model.Commands;

public record UpdateBatchExpirationCommand(int BatchId, DateOnly? Expiration);
