namespace Kipu.Platform.Iam.Domain.Model.Entities;

/// <summary>
///     A short-lived 6-digit code emailed to a user who forgot their
///     password. Only the BCrypt hash of the code is ever persisted — same
///     treatment as User.PasswordHash, since this is just as much a secret
///     that grants account access while it's valid.
///
///     One row per outstanding reset: requesting a new code replaces
///     whatever row already existed for that user (see
///     UserCommandService.Handle(RequestPasswordResetCommand)), so there is
///     never more than one code a user could try at a time.
/// </summary>
public class PasswordResetCode(int userId, string codeHash, DateTimeOffset expiresAt)
{
    public PasswordResetCode() : this(0, string.Empty, DateTimeOffset.MinValue)
    {
    }

    public int Id { get; }
    public int UserId { get; private set; } = userId;
    public string CodeHash { get; private set; } = codeHash;
    public DateTimeOffset ExpiresAt { get; private set; } = expiresAt;
    public int AttemptCount { get; private set; }
    public bool IsVerified { get; private set; }

    private const int MaxAttempts = 5;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool HasExceededAttempts() => AttemptCount >= MaxAttempts;

    /// <summary>Whether this code can still be checked against a guess — expired, already-exhausted, or already-verified codes can't.</summary>
    public bool CanBeAttempted(DateTimeOffset now) => !IsExpired(now) && !HasExceededAttempts() && !IsVerified;

    public void RegisterFailedAttempt()
    {
        AttemptCount++;
    }

    public void MarkVerified()
    {
        IsVerified = true;
    }
}
