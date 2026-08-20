using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 M7 — POST /reports only checked Type for NotEmpty(). Any other
///     non-empty string used to slip through, get persisted, and then
///     silently fall into ReportQueryService.ExportReportAsExcel's Sales
///     default at export time instead of ever being rejected.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class ReportTypeValidationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GenerateReport_WithAnUnknownType_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new { type = "PROFIT_AND_LOSS" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("SALES")]
    [InlineData("INVENTORY")]
    [InlineData("STOCK_MOVEMENTS")]
    public async Task GenerateReport_WithAKnownType_Succeeds(string type)
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new { type });

        response.EnsureSuccessStatusCode();
    }
}
