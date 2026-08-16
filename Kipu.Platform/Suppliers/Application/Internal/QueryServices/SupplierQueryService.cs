using Kipu.Platform.Suppliers.Application.QueryServices;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Queries;
using Kipu.Platform.Suppliers.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Application.Internal.QueryServices;

public class SupplierQueryService(ISupplierRepository supplierRepository) : ISupplierQueryService
{
    public async Task<IEnumerable<Supplier>> Handle(GetAllSuppliersByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await supplierRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<Supplier?> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        return await supplierRepository.FindByIdAsync(query.SupplierId, cancellationToken);
    }
}
