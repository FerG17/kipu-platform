using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 minor item: DocumentNumber (DNI/RUC) had a length check (I39) but
///     no uniqueness check — two customer rows could carry the same document
///     within one business, even though customer.entity.js on the frontend
///     already documented the intent that it must be unique.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class CustomerDocumentUniquenessTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatingACustomer_WithADocumentAlreadyInUse_IsRejected()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Primer cliente",
            documentNumber = "87654321",
            phoneNumber = "999111222",
            email = ""
        })).EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Segundo cliente",
            documentNumber = "87654321",
            phoneNumber = "999333444",
            email = ""
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreatingACustomer_WithABlankDocument_DoesNotCollideWithOthers()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente sin documento 1",
            documentNumber = "",
            phoneNumber = "999111222",
            email = ""
        })).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente sin documento 2",
            documentNumber = "",
            phoneNumber = "999333444",
            email = ""
        });

        Assert.True(second.IsSuccessStatusCode);
    }

    [Fact]
    public async Task UpdatingACustomer_WithAnotherCustomersDocument_IsRejected()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente A",
            documentNumber = "11223344",
            phoneNumber = "999111222",
            email = ""
        })).EnsureSuccessStatusCode();

        var customerBResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente B",
            documentNumber = "55667788",
            phoneNumber = "999333444",
            email = ""
        });
        var customerBId = (await ReadJsonAsync(customerBResponse)).GetProperty("id").GetInt32();

        var update = await client.PatchAsJsonAsync($"/api/v1/customers/{customerBId}", new
        {
            fullName = "Cliente B",
            documentNumber = "11223344",
            phoneNumber = "999333444",
            email = ""
        });

        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    [Fact]
    public async Task UpdatingACustomer_KeepingItsOwnDocument_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente",
            documentNumber = "99887766",
            phoneNumber = "999111222",
            email = ""
        });
        var customerId = (await ReadJsonAsync(response)).GetProperty("id").GetInt32();

        var update = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", new
        {
            fullName = "Cliente actualizado",
            documentNumber = "99887766",
            phoneNumber = "999111222",
            email = ""
        });

        Assert.True(update.IsSuccessStatusCode);
    }
}
