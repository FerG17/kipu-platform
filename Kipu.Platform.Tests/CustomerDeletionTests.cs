using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     I31 — DELETE /customers used to physically remove the row. Sale.CustomerId
///     is SetNull on delete, so that silently severed a real sale's "who
///     bought this" attribution instead of failing or being blocked. Now a
///     soft delete, restricted to Admin.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class CustomerDeletionTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DeletingACustomer_RemovesThemFromTheDefaultList()
    {
        var client = await CreateBusinessAsync();
        var customerId = await CreateCustomerAsync(client);

        (await client.DeleteAsync($"/api/v1/customers/{customerId}")).EnsureSuccessStatusCode();

        Assert.DoesNotContain((await ReadJsonAsync(await client.GetAsync("/api/v1/customers"))).EnumerateArray(),
            customer => customer.GetProperty("id").GetInt32() == customerId);
    }

    [Fact]
    public async Task DeletingACustomer_DoesNotOrphanAnExistingSale()
    {
        var client = await CreateBusinessAsync();
        var customerId = await CreateCustomerAsync(client);
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta con cliente",
            lines = new[] { SaleLine(productId, quantity: 1, unitPrice: 10m) }
        });
        saleResponse.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(saleResponse)).GetProperty("id").GetInt32();

        (await client.DeleteAsync($"/api/v1/customers/{customerId}")).EnsureSuccessStatusCode();

        var reloadedSale = await ReadJsonAsync(await client.GetAsync($"/api/v1/sales/{saleId}"));
        Assert.Equal(customerId, reloadedSale.GetProperty("customerId").GetInt32());
    }

    /// <summary>
    ///     X3 QA fix: sales-history.vue/payment-plans-list.vue resolve a
    ///     customer's name via GET /customers/{id} when it's missing from
    ///     the (active-only) bulk list — that only works if this endpoint
    ///     doesn't filter by Status. A payment plan with money still owed
    ///     needs to keep showing who owes it, even after "deleting" them.
    /// </summary>
    [Fact]
    public async Task GettingADeletedCustomerById_StillReturnsThemAsInactive()
    {
        var client = await CreateBusinessAsync();
        var customerId = await CreateCustomerAsync(client);

        (await client.DeleteAsync($"/api/v1/customers/{customerId}")).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/customers/{customerId}");
        response.EnsureSuccessStatusCode();
        var customer = await ReadJsonAsync(response);
        Assert.Equal(customerId, customer.GetProperty("id").GetInt32());
        Assert.False(customer.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task DeletingACustomer_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var customerId = await CreateCustomerAsync(admin);
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        var response = await cashier.DeleteAsync($"/api/v1/customers/{customerId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<int> CreateCustomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente de prueba",
            documentNumber = "12345678",
            phoneNumber = "999888777",
            email = "cliente@test.local"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }
}
