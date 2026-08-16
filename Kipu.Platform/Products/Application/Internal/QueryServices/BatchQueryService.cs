using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

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
