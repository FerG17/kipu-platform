using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X6 #5: a real per-business category catalog, replacing the old
///     "type anything under Otros" escape hatch. Covers the sign-up seeding
///     (IProductContextFacade.SeedDefaultCategories), the quick inline-create
///     endpoint, and its role/validation guards.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class CategoryTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly string[] FixedVocabulary =
        ["DAIRY", "GRAINS", "OILS", "BEVERAGES", "CLEANING", "MEDICINE", "OTHER"];

    [Fact]
    public async Task SigningUp_SeedsTheFixedCategoryVocabulary()
    {
        var admin = await CreateBusinessAsync();

        var response = await admin.GetAsync("/api/v1/categories");
        response.EnsureSuccessStatusCode();
        var categories = await ReadJsonAsync(response);

        var names = categories.EnumerateArray().Select(category => category.GetProperty("name").GetString()).ToList();
        Assert.Equal(FixedVocabulary.Length, names.Count);
        foreach (var fixedName in FixedVocabulary) Assert.Contains(fixedName, names);
    }

    [Fact]
    public async Task CreatingACategory_AsAdmin_AddsItToTheCatalog()
    {
        var admin = await CreateBusinessAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "Frutas y verduras" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        Assert.Equal("Frutas y verduras", created.GetProperty("name").GetString());

        var list = await ReadJsonAsync(await admin.GetAsync("/api/v1/categories"));
        Assert.Equal(FixedVocabulary.Length + 1, list.GetArrayLength());
    }

    [Fact]
    public async Task CreatingACategory_WithADuplicateName_IsRejected()
    {
        var admin = await CreateBusinessAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "DAIRY" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACategory_WithADuplicateNameAfterTrimming_IsRejected()
    {
        var admin = await CreateBusinessAsync();
        (await admin.PostAsJsonAsync("/api/v1/categories", new { name = "Frutas" })).EnsureSuccessStatusCode();

        var response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "  Frutas  " });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACategory_WithAnEmptyName_IsRejected()
    {
        var admin = await CreateBusinessAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACategory_AsCashier_IsForbidden()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.PostAsJsonAsync("/api/v1/categories", new { name = "Frutas" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListingCategories_AsCashier_IsAllowed()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.GetAsync("/api/v1/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EachBusiness_HasItsOwnCategoryCatalog()
    {
        var businessA = await CreateBusinessAsync();
        var businessB = await CreateBusinessAsync();
        (await businessA.PostAsJsonAsync("/api/v1/categories", new { name = "Solo en A" })).EnsureSuccessStatusCode();

        var listB = await ReadJsonAsync(await businessB.GetAsync("/api/v1/categories"));

        Assert.DoesNotContain(listB.EnumerateArray(), category => category.GetProperty("name").GetString() == "Solo en A");
    }
}
