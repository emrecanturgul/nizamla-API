using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using nizamla.Core.Entities;
using nizamla.Domain.Entities;
using nizamla.Application.Interfaces;
namespace nizamla.Api.Services;

public class JwtTokenService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly IUserRepository _users;
    public JwtTokenService(IConfiguration config, IUserRepository users)
    {
        _config = config;
        _users = users;
    }
    public (string token, DateTime expiresAt) CreateAccessToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role ?? "User")
        };
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["AccessTokenMinutes"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
        public async Task<(string token, DateTime expiresAt)> CreateAndStoreRefreshTokenAsync(User user)
        {
        var days = int.Parse(_config.GetSection("Jwt")["RefreshTokenDays"]!);
        var rt = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        };
        await _users.AddRefreshTokenAsync(rt);
        return (rt.Token, rt.ExpiresAt);
        }
        public async Task<User?> ValidateRefreshTokenAsync(string refreshToken)
        {
        var rt = await _users.GetRefreshTokenAsync(refreshToken);
        if (rt is null) return null;
        if (rt.RevokedAt != null) return null;
        if (rt.ExpiresAt <= DateTime.UtcNow) return null;
        return rt.User;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var rt = await _users.GetRefreshTokenAsync(refreshToken);
            if (rt is null) return;
            if (rt.RevokedAt != null) return;

            rt.RevokedAt = DateTime.UtcNow;
            await _users.SaveChangesAsync();
        }
        private static string GenerateSecureToken()
        {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
        }
}
