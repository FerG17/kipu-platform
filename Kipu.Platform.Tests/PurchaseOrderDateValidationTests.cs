using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     A 2026-08-19 manual-QA finding: CreatePurchaseOrderCommand never validated
///     Date/ExpectedDate at all. A past expected date slipped straight through
///     (the frontend's :min hint is UI-only, not enforced on submit), and an
///     out-of-range year could hit MySQL's DATE column bounds, surfacing as a
///     raw 500 the frontend then showed as a generic "check your connection"
///     toast instead of a real validation message.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class PurchaseOrderDateValidationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatingAnOrder_WithAnExpectedDateBeforeTheOrderDate_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            expectedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5),
            currency = "PEN",
            description = "orden con fecha pasada",
            lines = new[] { new { productId, quantity = 10, unitPrice = 5m, discount = 0m } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAnOrder_WithAnImplausibleExpectedYear_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            expectedDate = new DateOnly(32, 5, 15),
            currency = "PEN",
            description = "orden con año inexistente",
            lines = new[] { new { productId, quantity = 10, unitPrice = 5m, discount = 0m } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAnOrder_WithAValidExpectedDate_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10);

        response.EnsureSuccessStatusCode();
    }
}
