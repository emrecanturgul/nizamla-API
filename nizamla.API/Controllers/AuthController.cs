using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.Interfaces;
using nizamla.Application.dtos.auth;
using nizamla.Core.Entities;
using System.Security.Cryptography;
using System.Text;

namespace nizamla.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IJwtService _jwt;

        public AuthController(IUserRepository users, IJwtService jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        // 📌 Kullanıcı kaydı
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _users.GetByUsernameAsync(req.Username);
            if (existing != null)
                return Conflict("Username already exists");

            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = Sha256(req.Password),
                Role = "User"
            };

            await _users.CreateAsync(user);

            // Token üret
            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            return Ok(new AuthResponse
            {
                AccessToken = access,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refresh,
                RefreshTokenExpiresAt = refreshExp,
                Username = user.Username,
                Role = user.Role
            });
        }

        // 📌 Giriş
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user is null || user.PasswordHash != Sha256(req.Password))
                return Unauthorized("Invalid credentials");

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            return Ok(new AuthResponse
            {
                AccessToken = access,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refresh,
                RefreshTokenExpiresAt = refreshExp,
                Username = user.Username,
                Role = user.Role
            });
        }

        // 📌 Access token yenileme
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
            if (user is null) return Unauthorized("Invalid refresh token");

            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            return Ok(new AuthResponse
            {
                AccessToken = access,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refresh,
                RefreshTokenExpiresAt = refreshExp,
                Username = user.Username,
                Role = user.Role
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);
            return NoContent();
        }

        // Basit SHA256 hash (test için)
        private static string Sha256(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(bytes);
        }
    }
}
