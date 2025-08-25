using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.dtos.auth;
using nizamla.Application.Interfaces;
using nizamla.Core.Entities;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace nizamla.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IJwtService _jwt;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository users, IJwtService jwt, PasswordHasher<User> passwordHasher, ILogger<AuthController> logger)
        {
            _users = users;
            _jwt = jwt;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration request");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Yeni kayýt denemesi: Username={Username}, Email={Email}", req.Username, req.Email);

            var existingUsername = await _users.GetByUsernameAsync(req.Username);
            if (existingUsername != null)
            {
                _logger.LogWarning("Kayýt baþarýsýz. Username zaten mevcut: {Username}", req.Username);
                return Conflict("Username already exists");
            }

            var existingEmail = await _users.GetByEmailAsync(req.Email);
            if (existingEmail != null)
            {
                _logger.LogWarning("Registration failed: email {Email} already exists", req.Email);
                return Conflict("Email already exists");
            }

            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                Role = "User"
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);
            await _users.CreateAsync(user);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("User {Username} registered successfully", user.Username);

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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request");
                return BadRequest(ModelState);
            }

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) != PasswordVerificationResult.Success)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogWarning("Invalid login attempt for user {Username} from IP {IP}", req.Username, ip);
                return Unauthorized("Invalid credentials");
            }

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Kullanýcý giriþ yaptý: Username={Username}, UserId={UserId}", user.Username, user.Id);

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

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
            if (user is null)
            {
                _logger.LogWarning("Geçersiz refresh token denemesi: {RefreshToken}", req.RefreshToken);
                return Unauthorized("Invalid refresh token");
            }

            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Refresh token yenilendi: UserId={UserId}, Username={Username}", user.Id, user.Username);

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            _logger.LogInformation("Kullanýcý çýkýþ yaptý: UserId={UserId}, RefreshToken={RefreshToken}", userId, req.RefreshToken);

            return NoContent();
        }
    }
}
