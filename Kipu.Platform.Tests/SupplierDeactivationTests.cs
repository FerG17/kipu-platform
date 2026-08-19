using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     I30 — deactivating a supplier with pending/actionable purchase orders
///     used to be a client-only rule, dependent on a parallel load that
///     might not have resolved yet. The backend now enforces it too.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class SupplierDeactivationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DeactivatingASupplier_WithAPendingPurchaseOrder_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        (await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/v1/suppliers/{supplierId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatingASupplier_WithADelayedPurchaseOrder_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        var orderResponse = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 5);
        orderResponse.EnsureSuccessStatusCode();
        var orderId = (await ReadJsonAsync(orderResponse)).GetProperty("id").GetInt32();
        (await client.PatchAsJsonAsync($"/api/v1/purchases/{orderId}", new { status = "DELAYED" })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/v1/suppliers/{supplierId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatingASupplier_WhoseOrdersAreAllReceivedOrCancelled_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        var orderResponse = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 5);
        orderResponse.EnsureSuccessStatusCode();
        var orderId = (await ReadJsonAsync(orderResponse)).GetProperty("id").GetInt32();
        (await client.PatchAsJsonAsync($"/api/v1/purchases/{orderId}", new { status = "RECEIVED" })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/v1/suppliers/{supplierId}");

        response.EnsureSuccessStatusCode();
    }
}
