using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Proves the business clock is actually wired into the request path,
///     not just correct in isolation. Time is frozen at 2026-08-11 02:00 UTC,
///     which is still the evening of 2026-08-10 in Lima — the window where
///     deriving dates from UTC gave the wrong day.
///
///     Shares the api collection so it never boots its own host alongside
///     another test class (concurrent hosts race for the EF migration lock).
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class TimeZoneBehaviourTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly DateTimeOffset LateEveningInLima = new(2026, 8, 11, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly LimaToday = new(2026, 8, 10);

    /// <summary>
    ///     Registering stock for a batch that expires today has to be allowed.
    ///     Reading "today" off UTC made it 2026-08-11, so a batch expiring on
    ///     the 10th looked already expired and every evening intake of
    ///     same-day goods was rejected as an invalid date.
    /// </summary>
    [Fact]
    public async Task StockIntake_LateInTheEvening_AcceptsABatchExpiringOnTheLocalToday()
    {
        await using var frozenFactory = new FrozenClockApiFactory(LateEveningInLima);
        var client = await CreateBusinessOnAsync(frozenFactory);
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 6,
            expiration: LimaToday);

        Assert.True(response.IsSuccessStatusCode,
            $"a batch expiring on the bodega's today ({LimaToday}) must be accepted, got {response.StatusCode}");
    }

    /// <summary>
    ///     Days-to-expiry must be counted from the local date too, or the API
    ///     reports one fewer day than the bodega actually has.
    /// </summary>
    [Fact]
    public async Task BatchDaysToExpiry_LateInTheEvening_IsCountedFromTheLocalDate()
    {
        await using var frozenFactory = new FrozenClockApiFactory(LateEveningInLima);
        var client = await CreateBusinessOnAsync(frozenFactory);
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 6,
            expiration: LimaToday.AddDays(3))).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/batches?productId={productId}");
        response.EnsureSuccessStatusCode();
        var batch = (await ReadJsonAsync(response))[0];

        Assert.Equal(3, batch.GetProperty("daysToExpiry").GetInt32());
    }

    /// <summary>Signs up a business against a specific factory instance rather than the shared one.</summary>
    private static async Task<HttpClient> CreateBusinessOnAsync(WebApplicationFactory<Program> apiFactory)
    {
        var client = apiFactory.CreateClient();
        var response = await PostSignUpAsync(client, new
        {
            email = $"owner-{Guid.NewGuid():N}@test.local",
            password = "Passw0rd!test",
            name = "Test",
            lastName = "Owner",
            businessName = "Bodega de prueba",
            businessType = "RETAIL"
        });
        response.EnsureSuccessStatusCode();

        var token = (await ReadJsonAsync(response)).GetProperty("token").GetString();
        var authenticated = apiFactory.CreateClient();
        authenticated.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return authenticated;
    }

    private sealed class FrozenClockApiFactory(DateTimeOffset frozenAt) : BodegaApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(frozenAt)));
        }
    }
}
