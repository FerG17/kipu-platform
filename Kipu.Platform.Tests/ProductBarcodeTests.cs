using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Barcode field on Product — supports the frontend's progressive-learning
///     scanner flow (unknown code → manual registration → remembered from
///     then on). Optional and unique per business.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class ProductBarcodeTests(KipuApiFactory factory) : IntegrationTestBase(factory)
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

    /// <summary>
    ///     X3 minor item: barcode uniqueness now also has a DB-level unique
    ///     index (see AddProductActiveBarcodeUniqueIndex) as a safety net
    ///     behind the read-then-write check above — but that index is scoped
    ///     to ACTIVE products only (a generated column, NULL while a product
    ///     is INACTIVE), by design: deactivating a product must still free
    ///     its barcode for reuse, exactly like before the index existed.
    /// </summary>
    [Fact]
    public async Task CreateProduct_WithABarcodeFromADeactivatedProduct_IsAllowed()
    {
        var client = await CreateBusinessAsync();
        var firstResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto A", description = "", category = "ABARROTES", basePrice = 5m, barcode = "4444444444444"
        });
        firstResponse.EnsureSuccessStatusCode();
        var firstId = (await ReadJsonAsync(firstResponse)).GetProperty("id").GetInt32();

        (await client.DeleteAsync($"/api/v1/products/{firstId}")).EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto B", description = "", category = "ABARROTES", basePrice = 5m, barcode = "4444444444444"
        });

        secondResponse.EnsureSuccessStatusCode();
    }
}
