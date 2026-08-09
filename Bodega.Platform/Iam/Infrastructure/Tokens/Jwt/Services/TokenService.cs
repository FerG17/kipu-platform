using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Bodega.Platform.Iam.Application.Internal.OutboundServices;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

namespace Bodega.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

/// <summary>
///     Generates and validates JWTs. Claim types are the contract with
///     Shared.Infrastructure.Security.CurrentUserAccessor ("business_id") and
///     with the standard ClaimsPrincipal conventions (NameIdentifier, Role).
/// </summary>
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private const string BusinessIdClaimType = "business_id";
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(BusinessIdClaimType, user.BusinessId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            ]),
            Expires = DateTime.UtcNow.AddDays(_tokenSettings.ExpirationDays),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
