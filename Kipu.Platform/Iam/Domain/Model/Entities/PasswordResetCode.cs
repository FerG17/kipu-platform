using Kipu.Platform.Shared.Domain.Model.Entities;

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
public class PasswordResetCode(int userId, string codeHash, DateTimeOffset requestedAt, DateTimeOffset expiresAt,
    int carriedOverAttemptCount = 0)
    : IVersionedEntity
{
    public PasswordResetCode() : this(0, string.Empty, DateTimeOffset.MinValue, DateTimeOffset.MinValue)
    {
    }

    public int Id { get; }
    public int UserId { get; private set; } = userId;
    public string CodeHash { get; private set; } = codeHash;

    /// <summary>When this code was issued — used for the resend cooldown, kept separate from ExpiresAt so the two can't drift if the lifetime constant ever changes.</summary>
    public DateTimeOffset RequestedAt { get; private set; } = requestedAt;

    public DateTimeOffset ExpiresAt { get; private set; } = expiresAt;

    /// <summary>
    ///     Failed guesses against this "incident" — carried over from the
    ///     previous code when a new one is requested before the old one
    ///     naturally expired (see UserCommandService.Handle
    ///     (RequestPasswordResetCommand)). Without this, an attacker could
    ///     reset the 5-guess budget at will just by requesting a fresh code,
    ///     turning "5 attempts" into "5 attempts per resend".
    /// </summary>
    public int AttemptCount { get; private set; } = carriedOverAttemptCount;

    public bool IsVerified { get; private set; }

    /// <summary>Optimistic-concurrency token — closes a real race where two concurrent ResetPassword calls against the same verified code could both succeed before either write committed.</summary>
    public int Version { get; set; }

    public const int MaxAttempts = 5;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool HasExceededAttempts() => AttemptCount >= MaxAttempts;

    /// <summary>Whether this code can still be checked against a guess — expired, already-exhausted, or already-verified codes can't.</summary>
    public bool CanBeAttempted(DateTimeOffset now) => !IsExpired(now) && !HasExceededAttempts() && !IsVerified;

    /// <summary>Minimum time between two "send me a new code" requests for the same account, independent of the per-IP rate limit — stops someone from resetting their own attempt budget by spamming resend.</summary>
    public bool IsWithinCooldown(DateTimeOffset now, TimeSpan cooldown) => now < RequestedAt + cooldown;

    public void RegisterFailedAttempt()
    {
        AttemptCount++;
    }

    public void MarkVerified()
    {
        IsVerified = true;
    }
}
