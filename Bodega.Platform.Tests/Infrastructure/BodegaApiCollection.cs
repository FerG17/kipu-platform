namespace Bodega.Platform.Tests.Infrastructure;

/// <summary>
///     Shares one application host across every test class. Per-class
///     fixtures would boot several hosts at once, and each host runs
///     Database.Migrate() on startup — they contend for the EF migration lock
///     and intermittently fail to start. One host is also simply faster;
///     isolation between tests comes from each one signing up its own
///     business, not from separate hosts.
/// </summary>
[CollectionDefinition(Name)]
public class BodegaApiCollection : ICollectionFixture<BodegaApiFactory>
{
    public const string Name = "bodega-api";
}
