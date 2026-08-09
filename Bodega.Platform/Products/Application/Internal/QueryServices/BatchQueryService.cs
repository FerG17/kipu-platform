using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.QueryServices;

public class BatchQueryService(IBatchRepository batchRepository) : IBatchQueryService
{
    public async Task<IEnumerable<Batch>> Handle(GetAllBatchesByProductIdQuery query, CancellationToken cancellationToken)
    {
        return await batchRepository.FindAllByProductIdAsync(query.ProductId, cancellationToken);
    }

    public async Task<IEnumerable<Batch>> Handle(GetAllBatchesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await batchRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
