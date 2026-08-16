using System.Text;
using System.Text.RegularExpressions;

namespace Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

/// <summary>
///     Fails startup when the JWT signing key isn't actually usable, instead
///     of letting the app boot and sign every token with a guessable key.
///
///     This exists because of a real incident: appsettings.json ships the
///     secret as a "%BODEGA_JWT_SECRET%" placeholder (same convention as the
///     connection string), but only the connection string was ever expanded.
///     In any environment without an override, the literal string
///     "%BODEGA_JWT_SECRET%" became the HMAC key — long enough that
///     SymmetricSignatureProvider accepted it without complaint, and public
///     in the repository. Anyone could have forged an admin token for any
///     business. Nothing detected it because Development has a real literal
///     secret in appsettings.Development.json, so every local test passed.
///
///     The lesson encoded here: a misconfigured signing key must be a loud
///     crash, never a silent downgrade.
/// </summary>
public static partial class JwtSecretGuard
{
    /// <summary>256-bit key, the standard for HMAC-SHA256. Microsoft's provider only enforces 128, which is how the 19-char placeholder slipped through.</summary>
    private const int MinimumSecretBytes = 32;

    /// <summary>
    ///     A burned key: it was committed in appsettings.Development.json while
    ///     this repository was public, so it is readable by anyone forever —
    ///     removing it from the working tree does not remove it from the git
    ///     history. It is long enough to satisfy every check above, which is
    ///     precisely the danger: nothing else would stop a deployment that
    ///     forgot ASPNETCORE_ENVIRONMENT from signing real tokens with it.
    ///
    ///     appsettings.Development.json is untracked now and its key has been
    ///     rotated, so no live secret is in version control. This constant
    ///     stays as a permanent tombstone for the value that leaked.
    /// </summary>
    private const string BurnedDevelopmentSecret = "bodega-local-dev-secret-do-not-use-in-production-1234567890";

    /// <summary>
    ///     Throws when the secret is missing, still an unexpanded placeholder,
    ///     too short to be a real key, or the one that leaked. Deliberately
    ///     never includes the secret itself in the exception message — startup
    ///     errors end up in logs and consoles.
    /// </summary>
    public static void EnsureUsable(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "TokenSettings:Secret is not configured. Set the BODEGA_JWT_SECRET environment variable " +
                "(or TokenSettings__Secret) to a random value of at least 32 bytes.");

        if (UnexpandedPlaceholderPattern().IsMatch(secret))
            throw new InvalidOperationException(
                "TokenSettings:Secret still contains an unexpanded %PLACEHOLDER%, which means its environment " +
                "variable is not set. Refusing to start rather than signing tokens with a publicly known key.");

        if (Encoding.UTF8.GetByteCount(secret) < MinimumSecretBytes)
            throw new InvalidOperationException(
                $"TokenSettings:Secret is shorter than the required {MinimumSecretBytes} bytes. " +
                "Use a random value of at least that length.");

        // The placeholder incident had a twin waiting to happen: the checks
        // above all pass for the old development secret, so a deployment that
        // simply forgot to set ASPNETCORE_ENVIRONMENT would boot signing real
        // tokens with a key published in the repository — the same total
        // compromise, reached by a different route.
        if (string.Equals(secret, BurnedDevelopmentSecret, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "TokenSettings:Secret is the development key that was once committed to this repository and is " +
                "therefore public. Generate a new one. Refusing to start rather than signing tokens with a " +
                "publicly known key.");
    }

    [GeneratedRegex(@"%[A-Za-z_][A-Za-z0-9_]*%")]
    private static partial Regex UnexpandedPlaceholderPattern();
}
