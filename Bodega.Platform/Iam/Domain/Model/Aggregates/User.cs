using System.Text.Json.Serialization;

namespace Bodega.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The user aggregate — an individual with access to a Business (tenant).
///     Password is never exposed on serialization; only PasswordHash is
///     persisted, set once at creation and only ever changed through
///     UpdatePasswordHash (BCrypt-hashed by the caller, never plain text).
/// </summary>
public class User(
    string email,
    string passwordHash,
    string name,
    string lastName,
    int businessId,
    int roleId,
    string phone = "")
{
    public User() : this(string.Empty, string.Empty, string.Empty, string.Empty, 0, 0)
    {
    }

    public int Id { get; }
    public string Email { get; private set; } = email;

    [JsonIgnore] public string PasswordHash { get; private set; } = passwordHash;

    public string Name { get; private set; } = name;
    public string LastName { get; private set; } = lastName;
    public int BusinessId { get; private set; } = businessId;
    public int RoleId { get; private set; } = roleId;
    public string Status { get; private set; } = "ACTIVE";
    public string Phone { get; private set; } = phone;

    public User UpdateProfile(string name, string lastName, string phone)
    {
        Name = name;
        LastName = lastName;
        Phone = phone;
        return this;
    }

    public User UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        return this;
    }

    /// <summary>
    ///     Links this user to a Business once it's created — used only during
    ///     the atomic sign-up flow (see UserCommandService.Handle(SignUpCommand)).
    /// </summary>
    public User LinkToBusiness(int businessId)
    {
        BusinessId = businessId;
        return this;
    }

    public User Deactivate()
    {
        Status = "INACTIVE";
        return this;
    }
}
