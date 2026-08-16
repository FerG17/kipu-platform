namespace Kipu.Platform.Iam.Domain.Model.Entities;

/// <summary>
///     Fixed role lookup (ADMIN, CASHIER, WAREHOUSE) — seeded data, not
///     user-creatable. A User references one via RoleId.
/// </summary>
public class Role(string position)
{
    public Role() : this(string.Empty)
    {
    }

    public int Id { get; }
    public string Position { get; private set; } = position;
}
