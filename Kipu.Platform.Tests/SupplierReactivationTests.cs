using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>X4 M11: a deactivated supplier can be brought back, and stays out of the default listing (used as the picker for new products/orders) until it is.</summary>
[Collection(KipuApiCollection.Name)]
public class SupplierReactivationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DeactivatedSupplier_IsExcludedFromTheDefaultListing_ButVisibleWithIncludeInactive()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);

        (await client.DeleteAsync($"/api/v1/suppliers/{supplierId}")).EnsureSuccessStatusCode();

        var defaultListing = await client.GetAsync("/api/v1/suppliers");
        defaultListing.EnsureSuccessStatusCode();
        var defaultIds = (await ReadJsonAsync(defaultListing)).EnumerateArray().Select(s => s.GetProperty("id").GetInt32());
        Assert.DoesNotContain(supplierId, defaultIds);

        var fullListing = await client.GetAsync("/api/v1/suppliers?includeInactive=true");
        fullListing.EnsureSuccessStatusCode();
        var fullIds = (await ReadJsonAsync(fullListing)).EnumerateArray().Select(s => s.GetProperty("id").GetInt32());
        Assert.Contains(supplierId, fullIds);
    }

    [Fact]
    public async Task ReactivatingASupplier_BringsItBackIntoTheDefaultListing()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        (await client.DeleteAsync($"/api/v1/suppliers/{supplierId}")).EnsureSuccessStatusCode();

        var reactivated = await client.PatchAsync($"/api/v1/suppliers/{supplierId}/activate", null);
        reactivated.EnsureSuccessStatusCode();
        Assert.Equal("ACTIVE", (await ReadJsonAsync(reactivated)).GetProperty("status").GetString());

        var listing = await client.GetAsync("/api/v1/suppliers");
        listing.EnsureSuccessStatusCode();
        var ids = (await ReadJsonAsync(listing)).EnumerateArray().Select(s => s.GetProperty("id").GetInt32());
        Assert.Contains(supplierId, ids);
    }
}
