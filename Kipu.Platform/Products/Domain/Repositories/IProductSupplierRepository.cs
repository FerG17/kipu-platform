using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IProductSupplierRepository : IBaseRepository<ProductSupplier>
{
    /// <summary>The current set of supplier links for one product — used both to display them and to diff against a new set on update.</summary>
    Task<IReadOnlyCollection<ProductSupplier>> FindByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>Every link for the business in one query, grouped by ProductId — backs the product list, avoiding one query per product.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> FindSupplierIdsGroupedByProductAsync(int businessId,
        CancellationToken cancellationToken = default);
}
