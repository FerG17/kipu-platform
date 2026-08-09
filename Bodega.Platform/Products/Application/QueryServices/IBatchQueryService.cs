using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;

namespace Bodega.Platform.Products.Application.QueryServices;

public interface IBatchQueryService
{
    Task<IEnumerable<Batch>> Handle(GetAllBatchesByProductIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Batch>> Handle(GetAllBatchesByBusinessIdQuery query, CancellationToken cancellationToken);
}
