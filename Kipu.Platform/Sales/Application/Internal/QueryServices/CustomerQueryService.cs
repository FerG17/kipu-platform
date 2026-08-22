using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Sales.Application.Internal.QueryServices;

public class CustomerQueryService(ICustomerRepository customerRepository) : ICustomerQueryService
{
    public async Task<PagedResult<Customer>> Handle(GetAllCustomersByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await customerRepository.FindAllByBusinessIdAsync(query.BusinessId, query.Page, cancellationToken);
    }

    public async Task<Customer?> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        return await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
    }
}
