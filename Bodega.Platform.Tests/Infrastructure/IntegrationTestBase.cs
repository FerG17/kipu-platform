using System.Net.Http.Json;
using System.Text.Json;

namespace Bodega.Platform.Tests.Infrastructure;

/// <summary>
///     Base for integration tests that drive the real HTTP API.
///
///     Isolation comes from multi-tenancy rather than from resetting the
///     database: every test signs up its own business, and the global
///     BusinessId query filter guarantees it can't see or be affected by any
///     other test's data. That keeps the suite fast and parallel-safe without
///     dropping tables between tests.
/// </summary>
public abstract class IntegrationTestBase(BodegaApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected readonly HttpClient Client = factory.CreateClient();

    /// <summary>Signs up a brand-new business and returns a client authenticated as its ADMIN owner.</summary>
    protected async Task<HttpClient> CreateBusinessAsync()
    {
        var email = $"owner-{Guid.NewGuid():N}@test.local";
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email,
            password = "Passw0rd!test",
            name = "Test",
            lastName = "Owner",
            businessName = "Bodega de prueba",
            businessType = "RETAIL"
        });
        response.EnsureSuccessStatusCode();

        var token = (await ReadJsonAsync(response)).GetProperty("token").GetString();

        var authenticated = factory.CreateClient();
        authenticated.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return authenticated;
    }

    protected static async Task<int> CreateProductAsync(HttpClient client, decimal basePrice = 10m)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto de prueba",
            description = "creado por un test",
            category = "ABARROTES",
            basePrice
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    /// <summary>Every business gets an "Almacén Principal" created during sign-up.</summary>
    protected static async Task<int> GetDefaultWarehouseIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/warehouses");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response))[0].GetProperty("id").GetInt32();
    }

    protected static async Task<HttpResponseMessage> RegisterStockIntakeAsync(HttpClient client, int productId,
        int warehouseId, int quantity, DateOnly? expiration = null, decimal? purchasePrice = null,
        string? supplier = null, int? minimumStock = null)
    {
        return await client.PostAsJsonAsync($"/api/v1/products/{productId}/stock-intake", new
        {
            warehouseId,
            quantity,
            purchasePrice,
            expiration,
            supplier,
            note = (string?)null,
            minimumStock
        });
    }

    protected static async Task<HttpResponseMessage> CreateSaleAsync(HttpClient client, params object[] lines)
    {
        return await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines
        });
    }

    protected static object SaleLine(int productId, int quantity, decimal unitPrice, decimal discount = 0m)
    {
        return new { productId, quantity, unitPrice, discount };
    }

    /// <summary>Total units of a product across every warehouse.</summary>
    protected static async Task<int> GetTotalStockAsync(HttpClient client, int productId)
    {
        var response = await client.GetAsync($"/api/v1/inventories?productId={productId}");
        response.EnsureSuccessStatusCode();

        var total = 0;
        foreach (var item in (await ReadJsonAsync(response)).EnumerateArray())
            total += item.GetProperty("stockUnit").GetInt32();

        return total;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), JsonOptions);
    }
}
