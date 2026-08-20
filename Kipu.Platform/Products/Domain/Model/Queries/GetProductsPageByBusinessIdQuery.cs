using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Products.Domain.Model.Queries;

/// <summary>
///     Backs the GetProducts collection endpoint only (X4 S3). Distinct from
///     GetAllProductsByBusinessIdQuery, which stays unpaged for internal
///     callers (ProductContextFacade) that genuinely need the whole catalog.
/// </summary>
public record GetProductsPageByBusinessIdQuery(int BusinessId, string? Category, bool IncludeInactive, PageRequest Page);
