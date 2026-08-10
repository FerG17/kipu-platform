namespace Bodega.Platform.Iam.Domain.Model.Entities;

/// <summary>
///     String constants matching the seeded Role.Position values (see the
///     InitialCreate migration's InsertData) — used wherever code needs to
///     compare against a role by name instead of its numeric Id, most
///     importantly [Authorize(Roles = ...)] on controllers.
/// </summary>
public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string Cashier = "CASHIER";
    public const string Warehouse = "WAREHOUSE";
}
