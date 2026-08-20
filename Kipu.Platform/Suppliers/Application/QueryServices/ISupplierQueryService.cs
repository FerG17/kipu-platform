using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Queries;

namespace Kipu.Platform.Suppliers.Application.QueryServices;

public interface ISupplierQueryService
{
    Task<PagedResult<Supplier>> Handle(GetAllSuppliersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Supplier?> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken);
}
