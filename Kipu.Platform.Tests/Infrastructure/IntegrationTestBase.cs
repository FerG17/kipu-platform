using System.Net.Http.Json;
using System.Text.Json;

namespace Kipu.Platform.Tests.Infrastructure;

/// <summary>
///     Base for integration tests that drive the real HTTP API.
///
///     Isolation comes from multi-tenancy rather than from resetting the
///     database: every test signs up its own business, and the global
///     BusinessId query filter guarantees it can't see or be affected by any
///     other test's data. That keeps the suite fast and parallel-safe without
///     dropping tables between tests.
/// </summary>
public abstract class IntegrationTestBase(KipuApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected readonly HttpClient Client = factory.CreateClient();

    /// <summary>Role ids as seeded by the InitialCreate migration — see RoleNames.</summary>
    protected const int AdminRoleId = 1;

    protected const int CashierRoleId = 2;
    protected const int WarehouseRoleId = 3;

    protected const string ValidPassword = "Passw0rd!test";

    /// <summary>Signs up a brand-new business and returns a client authenticated as its ADMIN owner.</summary>
    protected async Task<HttpClient> CreateBusinessAsync()
    {
        return (await CreateBusinessWithOwnerAsync()).Client;
    }

    /// <summary>
    ///     Same as <see cref="CreateBusinessAsync" /> but also hands back the
    ///     owner's credentials, id and raw token — the security tests need
    ///     those to re-authenticate, tamper with a token, or invite members.
    /// </summary>
    protected async Task<(HttpClient Client, string Email, int UserId, int BusinessId, string Token)>
        CreateBusinessWithOwnerAsync()
    {
        var email = $"owner-{Guid.NewGuid():N}@test.local";

        var response = await PostSignUpAsync(Client, new
        {
            email,
            password = ValidPassword,
            name = "Test",
            lastName = "Owner",
            businessName = "Kipu de prueba",
            businessType = "RETAIL"
        });
        response.EnsureSuccessStatusCode();

        var body = await ReadJsonAsync(response);
        var token = body.GetProperty("token").GetString()!;

        return (AuthenticatedClient(token), email, body.GetProperty("id").GetInt32(),
            body.GetProperty("businessId").GetInt32(), token);
    }

    /// <summary>
    ///     Sign-up requires the platform-admin bootstrap key (see
    ///     AuthenticationController.SignUp) — every call site in the suite
    ///     goes through here instead of a bare PostAsJsonAsync so that header
    ///     lives in one place.
    /// </summary>
    protected static async Task<HttpResponseMessage> PostSignUpAsync(HttpClient client, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authentication/sign-up")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Bootstrap-Key", KipuApiFactory.TestBootstrapKey);
        return await client.SendAsync(request);
    }

    /// <summary>Invites a team member with the given role into the admin's business, and signs in as them.</summary>
    protected async Task<HttpClient> InviteAndSignInAsync(HttpClient adminClient, int roleId)
    {
        var email = await InviteMemberAsync(adminClient, roleId);
        return AuthenticatedClient(await SignInForTokenAsync(email, ValidPassword));
    }

    /// <summary>Invites a team member and returns their email (they can then be signed in, or deactivated, by the test).</summary>
    protected async Task<string> InviteMemberAsync(HttpClient adminClient, int roleId)
    {
        var email = $"member-{Guid.NewGuid():N}@test.local";

        var response = await adminClient.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = ValidPassword,
            name = "Team",
            lastName = "Member",
            roleId,
            phone = ""
        });
        response.EnsureSuccessStatusCode();

        return email;
    }

    /// <summary>Signs in and returns the raw JWT, so a test can inspect or tamper with it.</summary>
    protected async Task<string> SignInForTokenAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("token").GetString()!;
    }

    protected HttpClient AuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    protected static async Task<int> CreateSupplierAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            name = "Proveedor",
            lastName = "de prueba",
            ruc = "20123456789",
            email = "proveedor@test.local",
            phone = "999888777",
            address = "Av. Siempre Viva 742",
            contactPerson = "Contacto",
            category = "GRAINS"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    protected static async Task<HttpResponseMessage> CreatePurchaseOrderAsync(HttpClient client, int supplierId,
        int productId, int quantity, decimal unitPrice = 5m, decimal discount = 0m)
    {
        return await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            expectedDate = (DateOnly?)null,
            currency = "PEN",
            description = "orden de prueba",
            lines = new[] { new { productId, quantity, unitPrice, discount } }
        });
    }

    protected static async Task<int> CreateProductAsync(HttpClient client, decimal basePrice = 10m,
        string name = "Producto de prueba")
    {
        var response = await CreateProductResponseAsync(client, basePrice, name);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    /// <summary>The raw response, for tests that assert on how a bad product payload is rejected.</summary>
    protected static async Task<HttpResponseMessage> CreateProductResponseAsync(HttpClient client,
        decimal basePrice = 10m, string name = "Producto de prueba")
    {
        return await client.PostAsJsonAsync("/api/v1/products", new
        {
            name,
            description = "creado por un test",
            category = "ABARROTES",
            basePrice
        });
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
        return await CreateSaleAsync(client, paymentMethod: "CASH", lines);
    }

    /// <summary>X4: lets a test create a CREDIT sale (Sale.Status == Credit — required before a payment plan can be attached, see PaymentPlanCommandService) without every other CreateSaleAsync call site having to pass a payment method it doesn't care about.</summary>
    protected static async Task<HttpResponseMessage> CreateSaleAsync(HttpClient client, string paymentMethod, params object[] lines)
    {
        return await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId = (int?)null,
            paymentMethod,
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

    /// <summary>
    ///     Transparently unwraps a paginated collection envelope
    ///     ({ items, page, pageSize, totalCount, totalPages } — X4 S3) down
    ///     to its "items" array, so every existing test that calls
    ///     .EnumerateArray() on a GET-collection response keeps working
    ///     whether that endpoint got paginated or not. Single-resource GETs
    ///     (never shaped like { items: [...] }) pass through unchanged.
    /// </summary>
    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await ReadJsonEnvelopeAsync(response);
        return json.ValueKind == JsonValueKind.Object && json.TryGetProperty("items", out var items) &&
               items.ValueKind == JsonValueKind.Array
            ? items
            : json;
    }

    /// <summary>The raw response body, unwrapped — for tests that assert on a paginated envelope's own shape (page/pageSize/totalCount/totalPages), not just its items.</summary>
    protected static async Task<JsonElement> ReadJsonEnvelopeAsync(HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), JsonOptions);
    }
}
