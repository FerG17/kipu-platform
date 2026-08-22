namespace Kipu.Platform.Shared.Infrastructure.Security;

/// <summary>
///     Fails startup when Cors:AllowedOrigins is empty in Production, instead
///     of letting the app boot normally and only fail later, per request —
///     sign-in still works (same-origin-exempt), so the app looks healthy
///     right up until the first write, which returns a bodyless 403.
///     Mirrors JwtSecretGuard/BootstrapKeyGuard's reasoning: a misconfigured
///     deployment must be a loud crash at startup, never a silent downgrade.
/// </summary>
public static class CorsAllowedOriginsGuard
{
    public static void EnsureUsable(string[] allowedOrigins, bool isProduction)
    {
        if (isProduction && allowedOrigins.Length == 0)
            throw new InvalidOperationException(
                "Cors:AllowedOrigins is empty in Production. Set it to the real frontend origin(s) " +
                "(Cors__AllowedOrigins__0=https://<project>.pages.dev, no trailing slash) before starting " +
                "— refusing to boot with every authenticated write silently returning 403.");
    }
}
