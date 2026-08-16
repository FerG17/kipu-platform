using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.QueryServices;

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
