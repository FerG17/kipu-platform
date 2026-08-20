using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>X4 S3: every collection GET now caps what a single request can return, instead of dumping the whole table.</summary>
[Collection(KipuApiCollection.Name)]
public class PaginationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ProductsPage_SlicesCorrectly_WithNoOverlapAcrossPages()
    {
        var client = await CreateBusinessAsync();
        var ids = new List<int>();
        for (var i = 0; i < 5; i++) ids.Add(await CreateProductAsync(client, name: $"Producto {i}"));

        async Task<List<int>> IdsOnPage(int page)
        {
            var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync($"/api/v1/products?page={page}&pageSize=2"));
            return envelope.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToList();
        }

        var page1Envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/products?page=1&pageSize=2"));
        Assert.Equal(1, page1Envelope.GetProperty("page").GetInt32());
        Assert.Equal(2, page1Envelope.GetProperty("pageSize").GetInt32());
        Assert.Equal(5, page1Envelope.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page1Envelope.GetProperty("totalPages").GetInt32());

        var page1Ids = await IdsOnPage(1);
        var page2Ids = await IdsOnPage(2);
        var page3Ids = await IdsOnPage(3);

        Assert.Equal(2, page1Ids.Count);
        Assert.Equal(2, page2Ids.Count);
        Assert.Single(page3Ids);

        var allPagedIds = page1Ids.Concat(page2Ids).Concat(page3Ids).ToList();
        Assert.Equal(allPagedIds.Count, allPagedIds.Distinct().Count());
        Assert.Equal(ids.OrderBy(id => id), allPagedIds.OrderBy(id => id));
    }

    [Fact]
    public async Task PageSize_DefaultsTo50_AndClampsAt200()
    {
        var client = await CreateBusinessAsync();
        await CreateProductAsync(client);

        var defaultPage = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/products"));
        Assert.Equal(1, defaultPage.GetProperty("page").GetInt32());
        Assert.Equal(50, defaultPage.GetProperty("pageSize").GetInt32());

        var oversizedPage = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/products?pageSize=9999"));
        Assert.Equal(200, oversizedPage.GetProperty("pageSize").GetInt32());

        var invalidPage = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/products?page=0&pageSize=-5"));
        Assert.Equal(1, invalidPage.GetProperty("page").GetInt32());
        Assert.Equal(50, invalidPage.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Sales_ReturnAPaginatedEnvelope()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, basePrice: 10m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, 10)).EnsureSuccessStatusCode();
        (await CreateSaleAsync(client, SaleLine(productId, 1, 10m))).EnsureSuccessStatusCode();

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/sales?pageSize=1"));
        Assert.Equal(1, envelope.GetProperty("pageSize").GetInt32());
        Assert.True(envelope.GetProperty("totalCount").GetInt32() >= 1);
        Assert.Single(envelope.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Customers_ReturnAPaginatedEnvelope()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente de prueba", documentNumber = "12345678", phoneNumber = "999888777", email = "cliente@test.local"
        })).EnsureSuccessStatusCode();

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/customers"));
        Assert.Equal(1, envelope.GetProperty("totalCount").GetInt32());
        Assert.Single(envelope.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Suppliers_ReturnAPaginatedEnvelope()
    {
        var client = await CreateBusinessAsync();
        await CreateSupplierAsync(client);

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/suppliers"));
        Assert.Equal(1, envelope.GetProperty("totalCount").GetInt32());
        Assert.Single(envelope.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task PurchaseOrders_ReturnAPaginatedEnvelope_BothUnfilteredAndBySupplier()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        (await CreatePurchaseOrderAsync(client, supplierId, productId, 10)).EnsureSuccessStatusCode();

        var all = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/purchases"));
        Assert.Equal(1, all.GetProperty("totalCount").GetInt32());

        var bySupplier = await ReadJsonEnvelopeAsync(await client.GetAsync($"/api/v1/purchases?supplierId={supplierId}"));
        Assert.Equal(1, bySupplier.GetProperty("totalCount").GetInt32());
        Assert.Single(bySupplier.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task StockMovements_ReturnAPaginatedEnvelope()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, 5)).EnsureSuccessStatusCode();

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/stock-movements"));
        Assert.True(envelope.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task AlertHistory_ReturnsAPaginatedEnvelope_EvenWhenEmpty()
    {
        var client = await CreateBusinessAsync();

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/alerts/history"));
        Assert.Equal(0, envelope.GetProperty("totalCount").GetInt32());
        Assert.Empty(envelope.GetProperty("items").EnumerateArray());
    }
}
