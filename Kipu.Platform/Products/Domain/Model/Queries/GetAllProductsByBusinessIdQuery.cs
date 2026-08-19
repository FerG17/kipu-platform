namespace Kipu.Platform.Products.Domain.Model.Queries;

public record GetAllProductsByBusinessIdQuery(int BusinessId, string? Category = null, bool IncludeInactive = false);
