namespace Kipu.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The business aggregate — a tenant (a bodega or farmacia). Every other
///     bounded context scopes its data by BusinessId.
/// </summary>
public class Business(string name, string type, int userId)
{
    public Business() : this(string.Empty, string.Empty, 0)
    {
    }

    public int Id { get; }
    public string Name { get; private set; } = name;

    /// <summary>BODEGA or FARMACIA.</summary>
    public string Type { get; private set; } = type;

    public string Address { get; private set; } = string.Empty;
    public string Ruc { get; private set; } = string.Empty;

    /// <summary>
    ///     Id of the owning User (the account that created this business).
    ///     Deliberately NOT a database-enforced foreign key (see
    ///     ModelBuilderExtensions.ApplyIamConfiguration): User.BusinessId and
    ///     Business.UserId are circular, and a relational engine can't insert
    ///     either row first if both sides are hard FKs. User.BusinessId is the
    ///     one real FK (every other bounded context depends on it being
    ///     valid); this one is an application-level reference only, patched
    ///     in via LinkOwner once the User row exists — see
    ///     UserCommandService.Handle(SignUpCommand).
    /// </summary>
    public int UserId { get; private set; } = userId;

    public Business UpdateProfile(string name, string type, string address, string ruc)
    {
        Name = name;
        Type = type;
        Address = address;
        Ruc = ruc;
        return this;
    }

    public Business LinkOwner(int userId)
    {
        UserId = userId;
        return this;
    }
}
