using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Queries;

namespace Bodega.Platform.Suppliers.Application.QueryServices;

public interface ISupplierQueryService
{
    Task<IEnumerable<Supplier>> Handle(GetAllSuppliersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Supplier?> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken);
}
