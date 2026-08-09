namespace Bodega.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Updates profile fields only — deliberately excludes password, so this
///     can never silently wipe it (the historical PUT-replaces-everything bug).
/// </summary>
public record UpdateUserProfileCommand(int UserId, string Name, string LastName, string Phone);
