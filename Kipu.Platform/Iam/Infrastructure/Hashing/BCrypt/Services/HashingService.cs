using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

public class HashingService : IHashingService
{
    public string HashPassword(string password)
    {
        return global::BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return global::BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
