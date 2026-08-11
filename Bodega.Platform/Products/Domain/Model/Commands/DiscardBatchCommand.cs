namespace Bodega.Platform.Products.Domain.Model.Commands;

/// <summary>Retires a batch whose goods left the shelf — see Batch.Discard.</summary>
public record DiscardBatchCommand(int BatchId);
