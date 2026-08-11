namespace Bodega.Platform.Tests.Infrastructure;

/// <summary>
///     Reads local development values without any of them living in source.
///
///     The test database password used to be a literal in BodegaApiFactory,
///     in a public repository. It comes from the environment now, falling back
///     to the untracked <c>.env</c> at the repository root — the same file
///     Docker Compose reads, so <c>docker compose up</c> and <c>dotnet test</c>
///     stay in step and neither needs anything else set up.
/// </summary>
public static class LocalEnvironment
{
    public static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value)) return value;

        value = ReadFromDotEnv(name);
        if (!string.IsNullOrWhiteSpace(value)) return value;

        throw new InvalidOperationException(
            $"{name} is not set. Copy .env.example to .env at the repository root and fill it in, " +
            $"or export {name} in your shell.");
    }

    private static string? ReadFromDotEnv(string name)
    {
        var file = FindDotEnv();
        if (file == null) return null;

        foreach (var line in File.ReadAllLines(file))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0) continue;

            if (trimmed[..separator].Trim() == name)
                return trimmed[(separator + 1)..].Trim().Trim('"');
        }

        return null;
    }

    /// <summary>Walks up from the test binaries until it finds the repository root that holds .env.</summary>
    private static string? FindDotEnv()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        return null;
    }
}
