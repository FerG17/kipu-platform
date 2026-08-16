namespace Kipu.Platform.Products.Interfaces.Acl;

/// <summary>Lightweight projection used by Alerts' expiration sweep — see IProductContextFacade.</summary>
public record ActiveBatchInfo(int BatchId, int ProductId, string ProductName, int BusinessId, DateOnly? Expiration);
