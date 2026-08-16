using System.Text.Json.Serialization;

namespace Kipu.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The values <see cref="User.Status" /> can hold. Only ACTIVE may sign in
///     or carry a usable token — checked both at sign-in
///     (UserCommandService) and on every request
///     (RequestAuthorizationMiddleware).
/// </summary>
public static class UserStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

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
    public string Status { get; private set; } = UserStatus.Active;
    public string Phone { get; private set; } = phone;

    /// <summary>
    ///     Bumped every time the password changes (and available for a future
    ///     explicit "log out of all devices" action). Embedded in every JWT
    ///     issued for this user; RequestAuthorizationMiddleware rejects any
    ///     token whose TokenVersion claim doesn't match the current value —
    ///     this is what makes a password change actually invalidate tokens
    ///     issued before it, instead of leaving them valid until they expire
    ///     on their own.
    /// </summary>
    public int TokenVersion { get; private set; }

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
        TokenVersion++;
        return this;
    }

    /// <summary>Invalidates every token issued for this user so far, without changing the password.</summary>
    public User RevokeAllSessions()
    {
        TokenVersion++;
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
        Status = UserStatus.Inactive;
        TokenVersion++;
        return this;
    }
}
