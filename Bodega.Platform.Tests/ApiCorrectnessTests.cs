using System.Net;
using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Endpoints that accept a request and then do something other than what
///     it asked for. Each test maps to a defect found in the 2026-08-10
///     independent audit.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class ApiCorrectnessTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     UpdateSaleStatus never reads the status it was handed and
    ///     unconditionally cancels, so asking to mark a sale PAID silently
    ///     cancelled it — and cancelling does not put the stock back.
    /// </summary>
    [Fact]
    public async Task UpdateSaleStatus_AskingForPaid_DoesNotSilentlyCancelTheSale()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var saleResponse = await CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 10m));
        saleResponse.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(saleResponse)).GetProperty("id").GetInt32();

        var response = await client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "PAID" });

        // Either the request is honoured or it is rejected — but a sale must
        // never come back cancelled from a call that asked for PAID.
        if (response.IsSuccessStatusCode)
            Assert.NotEqual("CANCELLED", (await ReadJsonAsync(response)).GetProperty("status").GetString());
        else
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Marking a purchase order RECEIVED registers a stock intake per
    ///     line. Nothing guarded against doing it twice, so a double click
    ///     booked the delivery into inventory twice over.
    /// </summary>
    [Fact]
    public async Task ReceivingAPurchaseOrderTwice_DoesNotBookTheStockTwice()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);
        var purchaseOrderId = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 12);

        var first = await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "RECEIVED" });
        first.EnsureSuccessStatusCode();
        var stockAfterFirst = await GetTotalStockAsync(client, productId);

        await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "RECEIVED" });

        Assert.Equal(stockAfterFirst, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     RoleId went straight into the User row with no check, so a
    ///     nonexistent role hit the foreign key and surfaced as a 500 instead
    ///     of a validation error.
    /// </summary>
    [Fact]
    public async Task InviteUser_WithARoleThatDoesNotExist_IsRejectedWithoutAServerError()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"invited-{Guid.NewGuid():N}@test.local",
            password = "Passw0rd!test",
            name = "Invitado",
            lastName = "Sin rol",
            roleId = 99,
            phone = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<int> CreateSupplierAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            name = "Distribuidora",
            lastName = "de prueba",
            ruc = "20123456789",
            email = "proveedor@test.local",
            phone = "999999999",
            address = "Av. Siempre Viva",
            contactPerson = "Contacto",
            category = "ABARROTES"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CreatePurchaseOrderAsync(HttpClient client, int supplierId, int productId, int quantity)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = today,
            expectedDate = today.AddDays(7),
            currency = "PEN",
            description = "orden de prueba",
            lines = new[] { new { productId, quantity, unitPrice = 5m, discount = 0m } }
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }
}
