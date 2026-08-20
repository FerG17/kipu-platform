using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CustomerRepository(AppDbContext context) : BaseRepository<Customer>(context), ICustomerRepository
{
    /// <summary>Excludes deactivated customers (see Customer.Deactivate, I31) — a "deleted" customer stops showing up in the picker, even though the row stays for its existing sales/payment plans to resolve against.</summary>
    public async Task<IEnumerable<Customer>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Customer>()
            .Where(customer => customer.BusinessId == businessId && customer.Status == CustomerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> FindByBusinessIdAndDocumentNumberAsync(int businessId, string documentNumber,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Customer>()
            .FirstOrDefaultAsync(customer => customer.BusinessId == businessId && customer.DocumentNumber == documentNumber,
                cancellationToken);
    }
}
