using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 audit I40 — CreateSaleCommand had no idempotency key, so a retry
///     after a dropped connection or an F5 mid-request (the response never
///     reaches the client, but the sale already went through) had no way to
///     tell "this already happened" from "this is a new sale" and could sell
///     the same cart twice.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class SaleIdempotencyTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatingASale_TwiceWithTheSameIdempotencyKey_ReturnsTheSameSale_WithoutDoubleSelling()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var payload = new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 3, unitPrice: 10m) },
            idempotencyKey = "checkout-attempt-1"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/sales", payload);
        firstResponse.EnsureSuccessStatusCode();
        var firstSaleId = (await ReadJsonAsync(firstResponse)).GetProperty("id").GetInt32();

        var secondResponse = await client.PostAsJsonAsync("/api/v1/sales", payload);
        secondResponse.EnsureSuccessStatusCode();
        var secondSaleId = (await ReadJsonAsync(secondResponse)).GetProperty("id").GetInt32();

        Assert.Equal(firstSaleId, secondSaleId);

        var inventoryResponse = await client.GetAsync($"/api/v1/inventories?productId={productId}");
        inventoryResponse.EnsureSuccessStatusCode();
        var stock = (await ReadJsonAsync(inventoryResponse)).EnumerateArray().Single().GetProperty("stockUnit").GetDecimal();
        Assert.Equal(7, stock);
    }

    [Fact]
    public async Task CreatingTwoSales_WithDifferentIdempotencyKeys_CreatesTwoSeparateSales()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        Task<HttpResponseMessage> Sell(string key) => client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 2, unitPrice: 10m) },
            idempotencyKey = key
        });

        var firstResponse = await Sell("checkout-attempt-a");
        firstResponse.EnsureSuccessStatusCode();
        var secondResponse = await Sell("checkout-attempt-b");
        secondResponse.EnsureSuccessStatusCode();

        var firstSaleId = (await ReadJsonAsync(firstResponse)).GetProperty("id").GetInt32();
        var secondSaleId = (await ReadJsonAsync(secondResponse)).GetProperty("id").GetInt32();
        Assert.NotEqual(firstSaleId, secondSaleId);
    }
}
