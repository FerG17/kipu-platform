using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IBatchQueryService
{
    Task<IEnumerable<Batch>> Handle(GetAllBatchesByProductIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Batch>> Handle(GetAllBatchesByBusinessIdQuery query, CancellationToken cancellationToken);
}
