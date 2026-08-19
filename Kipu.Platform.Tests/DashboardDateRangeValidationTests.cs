using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 audit I35 — neither GET /dashboard/sales-by-day nor POST /reports
///     capped how wide a date range could be. GetSalesByDay walks the range
///     one day at a time to build its series, so an unbounded span (e.g. a
///     mistyped year) isn't just a slow query — it's an unbounded loop.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class DashboardDateRangeValidationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetSalesByDay_WithARangeOverAYear_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.GetAsync(
            $"/api/v1/dashboard/sales-by-day?dateFrom={DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3):yyyy-MM-dd}&dateTo={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSalesByDay_WithAWeekRange_Succeeds()
    {
        var client = await CreateBusinessAsync();

        var response = await client.GetAsync(
            $"/api/v1/dashboard/sales-by-day?dateFrom={DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6):yyyy-MM-dd}&dateTo={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GenerateReport_WithARangeOverAYear_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new
        {
            type = "INVENTORY",
            dateFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3),
            dateTo = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
