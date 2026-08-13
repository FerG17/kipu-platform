using System.Net;
using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Barcode field on Product — supports the frontend's progressive-learning
///     scanner flow (unknown code → manual registration → remembered from
///     then on). Optional and unique per business.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class ProductBarcodeTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateProduct_WithABarcode_ReturnsItOnTheResource()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Arroz Costeño",
            description = "creado por un test",
            category = "ABARROTES",
            basePrice = 5.5m,
            barcode = "7751271001019"
        });
        response.EnsureSuccessStatusCode();

        var body = await ReadJsonAsync(response);
        Assert.Equal("7751271001019", body.GetProperty("barcode").GetString());
    }

    [Fact]
    public async Task CreateProduct_WithABarcodeAlreadyUsedByAnotherActiveProduct_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var first = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto A",
            description = "",
            category = "ABARROTES",
            basePrice = 5m,
            barcode = "1111111111111"
        });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto B",
            description = "",
            category = "ABARROTES",
            basePrice = 5m,
            barcode = "1111111111111"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    /// <summary>
    ///     Barcodes are scoped to the business, not global — two unrelated
    ///     bodegas can legitimately stock the same manufacturer barcode.
    /// </summary>
    [Fact]
    public async Task CreateProduct_WithABarcodeUsedByAnotherBusiness_IsAllowed()
    {
        var clientA = await CreateBusinessAsync();
        var clientB = await CreateBusinessAsync();

        (await clientA.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto A", description = "", category = "ABARROTES", basePrice = 5m, barcode = "2222222222222"
        })).EnsureSuccessStatusCode();

        var responseB = await clientB.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto B", description = "", category = "ABARROTES", basePrice = 5m, barcode = "2222222222222"
        });

        responseB.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateProduct_WithAnotherProductsBarcode_IsRejected()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto A", description = "", category = "ABARROTES", basePrice = 5m, barcode = "3333333333333"
        })).EnsureSuccessStatusCode();

        var productBId = await CreateProductAsync(client, name: "Producto B");

        var update = await client.PatchAsJsonAsync($"/api/v1/products/{productBId}", new
        {
            name = "Producto B",
            description = "",
            category = "ABARROTES",
            basePrice = 5m,
            barcode = "3333333333333"
        });

        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }
}
