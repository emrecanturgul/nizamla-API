using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using nizamla.Application.Interfaces;
using nizamla.Application.dtos.auth;
using nizamla.Core.Entities;

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

            var existingUsername = await _users.GetByUsernameAsync(req.Username);
            if (existingUsername != null)
            {
                _logger.LogWarning("Registration failed: username {Username} already exists", req.Username);
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
                _logger.LogWarning("Invalid login attempt for user {Username}", req.Username);
                return Unauthorized("Invalid credentials");
            }

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("User {Username} logged in", user.Username);

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
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid refresh token request");
                return BadRequest(ModelState);
            }

            var user = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
            if (user is null)
            {
                _logger.LogWarning("Invalid refresh token");
                return Unauthorized("Invalid refresh token");
            }

            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Refreshed tokens for user {Username}", user.Username);

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
            _logger.LogInformation("User {Username} logged out", User.Identity?.Name);
            return NoContent();
        }
    }
}
