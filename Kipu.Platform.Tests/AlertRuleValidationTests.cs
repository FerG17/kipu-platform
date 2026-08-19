using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 minor item: CreateOrUpdateAlertRuleCommand accepted any string as
///     AlertType (free text, only length-capped) and had no upper bound on
///     ThresholdValue.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class AlertRuleValidationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task SettingAnAlertRule_WithAnUnrecognizedType_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "EXPIRED", thresholdValue = 7, enabled = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SettingAnAlertRule_WithAThresholdOverAYear_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "EXPIRATION", thresholdValue = 366, enabled = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SettingAnAlertRule_WithARecognizedTypeAndValidThreshold_Succeeds()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "EXPIRATION", thresholdValue = 365, enabled = true });

        Assert.True(response.IsSuccessStatusCode);
    }
}
