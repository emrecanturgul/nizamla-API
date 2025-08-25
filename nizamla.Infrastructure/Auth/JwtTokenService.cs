// nizamla.Infrastructure/Auth/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using nizamla.Application.Interfaces;
using nizamla.Core.Entities;
using nizamla.Domain.Entities;

namespace nizamla.Infrastructure.Auth
{
    public class JwtTokenService : IJwtService
    {   private readonly IConfiguration _config;
        private readonly JwtOptions _jwt;
        private readonly IRefreshTokenPolicy _refreshPolicy;
        private readonly IUserRepository _users;

        public JwtTokenService(
            IOptions<JwtOptions> jwtOptions,
            IRefreshTokenPolicy refreshPolicy,
            IUserRepository users, IConfiguration configuration)
        {
            _jwt = jwtOptions.Value;
            _refreshPolicy = refreshPolicy;
            _users = users;
            _config = configuration;
        }

        public (string token, DateTime expiresAt) CreateAccessToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            
            var keyString = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key not configured. Set the 'Jwt__Key' environment variable or user secret.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role ?? "User")
            };

            var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        public async Task<(string token, DateTime expiresAt)> CreateAndStoreRefreshTokenAsync(User user)
        {
            var rt = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_refreshPolicy.LifeSpan)
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
            if (rt is null || rt.RevokedAt != null) return;
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
}
