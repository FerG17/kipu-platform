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
            new { alertType = "BOGUS_TYPE", thresholdValue = 7, enabled = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     EXPIRED is a real, independently configurable rule type — distinct
    ///     from EXPIRATION (see AlertExpirationSweepJob.LoadExpirationRules).
    ///     Regression test: an earlier pass at this whitelist wrongly rejected it.
    /// </summary>
    [Fact]
    public async Task SettingAnAlertRule_WithExpiredType_Succeeds()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "EXPIRED", thresholdValue = 0, enabled = true });

        Assert.True(response.IsSuccessStatusCode);
    }

    /// <summary>X6 #7: INSTALLMENT_DUE is a real, independently configurable rule type, same as EXPIRATION/EXPIRED.</summary>
    [Fact]
    public async Task SettingAnAlertRule_WithInstallmentDueType_Succeeds()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "INSTALLMENT_DUE", thresholdValue = 7, enabled = true });

        Assert.True(response.IsSuccessStatusCode);
    }

    /// <summary>X6 #12: SUPPLIER_INSTALLMENT_DUE is a real, independently configurable rule type, same as INSTALLMENT_DUE.</summary>
    [Fact]
    public async Task SettingAnAlertRule_WithSupplierInstallmentDueType_Succeeds()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "SUPPLIER_INSTALLMENT_DUE", thresholdValue = 7, enabled = true });

        Assert.True(response.IsSuccessStatusCode);
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
