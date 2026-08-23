using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace Kipu.Platform.Shared.Infrastructure.Security;

/// <summary>
///     Fails startup when the database connection string isn't actually
///     usable, instead of handing a malformed value to the MySQL driver and
///     surfacing three layers down as "The host name or IP address is
///     invalid" — a message that says nothing about which of the several
///     ways a %VAR% template can go wrong actually happened. Mirrors
///     JwtSecretGuard/BootstrapKeyGuard/CorsAllowedOriginsGuard's reasoning:
///     a misconfigured deployment must be a loud, specific crash at startup.
/// </summary>
public static partial class DatabaseConnectionStringGuard
{
    /// <summary>
    ///     Throws when the connection string is missing, still an unexpanded
    ///     %PLACEHOLDER%, not parseable by the driver, or parses with no
    ///     server host set. Deliberately never includes the raw string in the
    ///     exception message — it carries the database password, and startup
    ///     errors end up in logs and consoles.
    /// </summary>
    public static void EnsureUsable(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set the BODEGA_DB_CONNECTION_STRING " +
                "environment variable to a MySQL connection string " +
                "('server=...;port=...;user=...;password=...;database=...').");

        if (UnexpandedPlaceholderPattern().IsMatch(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection still contains an unexpanded %PLACEHOLDER%, which means the " +
                "BODEGA_DB_CONNECTION_STRING environment variable is not reaching this process. Refusing to start " +
                "rather than handing the driver a literal placeholder string.");

        MySqlConnectionStringBuilder parsed;
        try
        {
            parsed = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection could not be parsed as a MySQL connection string. Expected " +
                "the form 'server=...;port=...;user=...;password=...;database=...' (not a mysql:// URL, which " +
                $"this driver does not accept). Parser error: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(parsed.Server))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection parsed successfully but has no 'server' host set. Check the " +
                "connection string actually includes 'server=<host>;' — this is what silently produces the " +
                "\"host name or IP address is invalid\" error further down in the driver.");
    }

    [GeneratedRegex(@"%[A-Za-z_][A-Za-z0-9_]*%")]
    private static partial Regex UnexpandedPlaceholderPattern();
}
