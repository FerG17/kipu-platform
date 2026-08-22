namespace Kipu.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Configurable so the integration suite can zero it out — every test
///     that requests two codes back-to-back would otherwise collide with a
///     real cooldown that has no clock injection to fast-forward past (see
///     KipuApiFactory). Production keeps the real value from appsettings.json.
/// </summary>
public class PasswordResetSettings
{
    public int RequestCooldownSeconds { get; set; } = 60;
}
