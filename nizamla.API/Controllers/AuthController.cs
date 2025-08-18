using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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

        public AuthController(IUserRepository users, IJwtService jwt, PasswordHasher<User> passwordHasher)
        {
            _users = users;
            _jwt = jwt;
            _passwordHasher = passwordHasher;
        }
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
                Role = "User",
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

            await _users.CreateAsync(user);
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
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) != PasswordVerificationResult.Success)
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
    }
}
