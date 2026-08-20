using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 M2 — PATCH /businesses/{id} had no validator at all: Name/Type/
///     Address/Ruc reached the database completely unchecked. Type is now
///     restricted to the one value the product actually supports (see
///     UpdateBusinessCommandValidator's doc comment on why "BODEGA" is the
///     whole set, not a placeholder).
/// </summary>
[Collection(KipuApiCollection.Name)]
public class BusinessProfileValidationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UpdateBusiness_WithAnEmptyName_IsRejected()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        var response = await admin.Client.PatchAsJsonAsync($"/api/v1/businesses/{admin.BusinessId}", new
        {
            name = "", type = "BODEGA", address = "", ruc = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBusiness_WithAnUnknownType_IsRejected()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        var response = await admin.Client.PatchAsJsonAsync($"/api/v1/businesses/{admin.BusinessId}", new
        {
            name = "Kipu de prueba", type = "FARMACIA", address = "", ruc = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBusiness_WithValidData_Succeeds()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        var response = await admin.Client.PatchAsJsonAsync($"/api/v1/businesses/{admin.BusinessId}", new
        {
            name = "Kipu Actualizada", type = "BODEGA", address = "Av. Siempre Viva 123", ruc = "20123456789"
        });

        response.EnsureSuccessStatusCode();
    }
}
