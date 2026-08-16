using System.Net;
using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     A product can be tagged with more than one supplier — the owner
///     explicitly wants "same product, different supplier" representable
///     (e.g. the usual one can't deliver, a replacement is used instead),
///     unlike the single free-text "distributor" this replaces.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class ProductSupplierTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateProduct_WithMultipleSupplierIds_LinksAllOfThem()
    {
        var client = await CreateBusinessAsync();
        var supplierA = await CreateSupplierAsync(client);
        var supplierB = await CreateSupplierAsync(client);

        var response = await CreateProductResponseWithSuppliersAsync(client, [supplierA, supplierB]);
        response.EnsureSuccessStatusCode();

        var created = await ReadJsonAsync(response);
        var supplierIds = created.GetProperty("supplierIds").EnumerateArray().Select(id => id.GetInt32()).ToList();
        Assert.Equal([supplierA, supplierB], supplierIds.OrderBy(id => id));

        var fetched = await client.GetAsync($"/api/v1/products/{created.GetProperty("id").GetInt32()}");
        fetched.EnsureSuccessStatusCode();
        var fetchedSupplierIds = (await ReadJsonAsync(fetched)).GetProperty("supplierIds")
            .EnumerateArray().Select(id => id.GetInt32()).ToList();
        Assert.Equal([supplierA, supplierB], fetchedSupplierIds.OrderBy(id => id));
    }

    [Fact]
    public async Task CreateProduct_WithAnUnknownSupplierId_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseWithSuppliersAsync(client, [999999]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     ProductSupplier.SupplierId has no database FK to Supplier (cross
    ///     bounded-context soft reference — see ModelBuilderExtensions), so
    ///     this business-ownership check in ProductCommandService is the only
    ///     thing standing between "tagged" and "tagged with someone else's
    ///     supplier id".
    /// </summary>
    [Fact]
    public async Task CreateProduct_WithAnotherBusinessSupplierId_IsRejected()
    {
        var ownBusiness     = await CreateBusinessAsync();
        var otherBusiness   = await CreateBusinessAsync();
        var otherSupplierId = await CreateSupplierAsync(otherBusiness);

        var response = await CreateProductResponseWithSuppliersAsync(ownBusiness, [otherSupplierId]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>SupplierIds on update is the full desired set, not a delta — sending just B must drop A, not append to it.</summary>
    [Fact]
    public async Task UpdateProduct_ReplacesTheSupplierSet_RatherThanAppending()
    {
        var client = await CreateBusinessAsync();
        var supplierA = await CreateSupplierAsync(client);
        var supplierB = await CreateSupplierAsync(client);

        var created = await ReadJsonAsync(await CreateProductResponseWithSuppliersAsync(client, [supplierA]));
        var productId = created.GetProperty("id").GetInt32();

        var updateResponse = await client.PatchAsJsonAsync($"/api/v1/products/{productId}", new
        {
            name = "Producto de prueba",
            description = "",
            category = "ABARROTES",
            basePrice = 10m,
            supplierIds = new[] { supplierB }
        });
        updateResponse.EnsureSuccessStatusCode();

        var updatedSupplierIds = (await ReadJsonAsync(updateResponse)).GetProperty("supplierIds")
            .EnumerateArray().Select(id => id.GetInt32()).ToList();
        Assert.Equal([supplierB], updatedSupplierIds);
    }

    [Fact]
    public async Task CreateProduct_WithNoSupplierIds_SucceedsWithAnEmptySet()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseAsync(client);
        response.EnsureSuccessStatusCode();

        var supplierIds = (await ReadJsonAsync(response)).GetProperty("supplierIds").EnumerateArray().ToList();
        Assert.Empty(supplierIds);
    }

    private static async Task<HttpResponseMessage> CreateProductResponseWithSuppliersAsync(HttpClient client,
        int[] supplierIds)
    {
        return await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto de prueba",
            description = "",
            category = "ABARROTES",
            basePrice = 10m,
            supplierIds
        });
    }
}
