using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CustomerRepository(AppDbContext context) : BaseRepository<Customer>(context), ICustomerRepository
{
    public async Task<IEnumerable<Customer>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Customer>().Where(customer => customer.BusinessId == businessId).ToListAsync(cancellationToken);
    }
}
