using Bodega.Platform.Sales.Application.QueryServices;
using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Queries;
using Bodega.Platform.Sales.Domain.Repositories;

namespace Bodega.Platform.Sales.Application.Internal.QueryServices;

public class CustomerQueryService(ICustomerRepository customerRepository) : ICustomerQueryService
{
    public async Task<IEnumerable<Customer>> Handle(GetAllCustomersByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await customerRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<Customer?> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        return await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
    }
}
