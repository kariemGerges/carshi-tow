using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarshiTow.Infrastructure.Security;

public sealed class JwtAccessTokenValidator(IOptions<JwtSettings> jwtOptions) : IJwtAccessTokenValidator
{
    public bool TryGetUserId(string accessToken, out Guid userId)
    {
        userId = default;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var settings = jwtOptions.Value;
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken.Trim(), parameters, out _);
            var sub =
                principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out userId);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
    }
}
