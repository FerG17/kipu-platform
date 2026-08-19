using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;
using System.Linq;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 audit I39 — several commands accepted string fields with no
///     MaximumLength check (letting a value wider than its DB column reach
///     the database) and no format/whitelist check (PaymentMethod/Currency
///     were free text, silently mixed into the same totals as every other
///     value).
/// </summary>
[Collection(KipuApiCollection.Name)]
public class InputValidationHardeningTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task InvitingAUser_WithAMalformedEmail_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email = "not-an-email",
            password = "Password123",
            name = "Team",
            lastName = "Member",
            roleId = CashierRoleId,
            phone = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvitingAUser_WithAnOverlongName_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"member-{Guid.NewGuid():N}@test.local",
            password = "Password123",
            name = new string('a', 101),
            lastName = "Member",
            roleId = CashierRoleId,
            phone = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatingAUserProfile_WithAnOverlongName_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var email = await InviteMemberAsync(client, CashierRoleId);
        var users = await client.GetAsync("/api/v1/users");
        var userId = (await ReadJsonAsync(users)).EnumerateArray()
            .First(user => user.GetProperty("email").GetString() == email)
            .GetProperty("id").GetInt32();

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{userId}", new
        {
            name = new string('a', 101),
            lastName = "Member",
            phone = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingASale_WithAnUnrecognizedPaymentMethod_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "BITCOIN",
            currency = "PEN",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 1, unitPrice: 10m) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingASale_WithAnUnrecognizedCurrency_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "USD",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 1, unitPrice: 10m) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>X3 minor item: a sale had no upper bound on how many lines it could carry.</summary>
    [Fact]
    public async Task CreatingASale_WithMoreThanFiftyLines_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 100)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines = Enumerable.Range(0, 51).Select(_ => SaleLine(productId, quantity: 1, unitPrice: 10m)).ToArray()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>X3 minor item: a single line had no upper bound on quantity either.</summary>
    [Fact]
    public async Task CreatingASale_WithMoreThanAThousandUnitsInOneLine_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 2000)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 1001, unitPrice: 10m) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAPurchaseOrder_WithAnUnrecognizedCurrency_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            expectedDate = (DateOnly?)null,
            currency = "USD",
            description = "orden de prueba",
            lines = new[] { new { productId, quantity = 5, unitPrice = 10m, discount = 0m } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
