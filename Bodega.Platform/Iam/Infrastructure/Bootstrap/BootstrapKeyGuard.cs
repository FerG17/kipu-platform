using System.Text;
using System.Text.RegularExpressions;

namespace Bodega.Platform.Iam.Infrastructure.Bootstrap;

/// <summary>
///     Fails startup when Bootstrap:Key isn't actually set, instead of
///     letting the app boot with public sign-up wide open. Mirrors
///     JwtSecretGuard's reasoning: a misconfigured secret must be a loud
///     crash, never a silent downgrade to "anyone can call this endpoint".
/// </summary>
public static partial class BootstrapKeyGuard
{
    private const int MinimumKeyBytes = 20;

    public static void EnsureUsable(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Bootstrap:Key is not configured. Set the BODEGA_BOOTSTRAP_KEY environment variable " +
                "(or Bootstrap__Key) to a random value of at least 20 bytes — public sign-up is unusable " +
                "without it, by design.");

        if (UnexpandedPlaceholderPattern().IsMatch(key))
            throw new InvalidOperationException(
                "Bootstrap:Key still contains an unexpanded %PLACEHOLDER%, which means its environment " +
                "variable is not set. Refusing to start rather than leaving sign-up open to anyone.");

        if (Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
            throw new InvalidOperationException(
                $"Bootstrap:Key is shorter than the required {MinimumKeyBytes} bytes. " +
                "Use a random value of at least that length.");
    }

    [GeneratedRegex(@"%[A-Za-z_][A-Za-z0-9_]*%")]
    private static partial Regex UnexpandedPlaceholderPattern();
}
