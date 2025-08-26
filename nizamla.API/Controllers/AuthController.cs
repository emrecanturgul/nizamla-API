using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using nizamla.Application.dtos;
using nizamla.Application.dtos.auth;
using nizamla.Application.Exceptions;
using nizamla.Application.Interfaces;
using nizamla.Core.Entities;
using System.Net;
using System.Security.Claims;

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

        /// <summary>
        /// Yeni kullanýcý kaydý oluþturur.
        /// </summary>
        /// <remarks>
        /// Kullanýcý adý, e-posta ve þifre alýr. Baþarýlý olursa Access + Refresh token döner.  
        /// Örnek request:
        /// 
        ///     POST /api/auth/register
        ///     {
        ///        "username": "ahmet",
        ///        "email": "ahmet@example.com",
        ///        "password": "Password123!"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Kayýt için kullanýcý adý, e-posta ve þifre bilgileri</param>
        /// <response code="200">Kayýt baþarýlý, access ve refresh token döner.</response>
        /// <response code="400">Doðrulama hatasý. Eksik ya da yanlýþ bilgi girildi.</response>
        /// <response code="409">Kullanýcý adý veya e-posta zaten kayýtlý.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Geçersiz kayýt isteði.");

            _logger.LogInformation("Yeni kayýt denemesi: KullanýcýAdý={Username}, Eposta={Email}", req.Username, req.Email);

            var existingUsername = await _users.GetByUsernameAsync(req.Username);
            if (existingUsername != null)
                throw new HttpException(HttpStatusCode.Conflict, "Kullanýcý adý zaten kayýtlý.");

            var existingEmail = await _users.GetByEmailAsync(req.Email);
            if (existingEmail != null)
                throw new HttpException(HttpStatusCode.Conflict, "Bu e-posta adresi zaten kullanýlýyor.");

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

            _logger.LogInformation("Yeni kullanýcý baþarýyla kayýt oldu: {Username}", user.Username);

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

        /// <summary>
        /// Kullanýcý giriþi yapar.
        /// </summary>
        /// <remarks>
        /// Kullanýcý adý ve þifre doðrulanýr. Baþarýlý olursa Access + Refresh token döner.  
        /// Örnek request:
        /// 
        ///     POST /api/auth/login
        ///     {
        ///        "username": "ahmet",
        ///        "password": "Password123!"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Giriþ bilgileri (Username, Password)</param>
        /// <response code="200">Giriþ baþarýlý, access ve refresh token döner.</response>
        /// <response code="400">Doðrulama hatasý. Eksik ya da yanlýþ bilgi girildi.</response>
        /// <response code="401">Kullanýcý adý veya þifre yanlýþ.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Geçersiz giriþ isteði.");

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) != PasswordVerificationResult.Success)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogWarning("Geçersiz giriþ denemesi: KullanýcýAdý={Username}, IP={IP}", req.Username, ip);
                throw new HttpException(HttpStatusCode.Unauthorized, "Kullanýcý adý veya þifre hatalý.");
            }

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Kullanýcý giriþ yaptý: {Username}", user.Username);

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

        /// <summary>
        /// Refresh token yeniler.
        /// </summary>
        /// <remarks>
        /// Geçerli bir refresh token alýnarak yeni Access ve Refresh token üretilir.  
        /// Örnek request:
        /// 
        ///     POST /api/auth/refresh
        ///     {
        ///        "refreshToken": "string"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Refresh token bilgisi</param>
        /// <response code="200">Yeni tokenler üretildi.</response>
        /// <response code="400">Doðrulama hatasý. Eksik refresh token gönderildi.</response>
        /// <response code="401">Geçersiz refresh token.</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Refresh token isteði geçersiz.");

            var user = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
            if (user is null)
            {
                _logger.LogWarning("Geçersiz refresh token: {Token}", req.RefreshToken);
                throw new HttpException(HttpStatusCode.Unauthorized, "Refresh token geçersiz.");
            }

            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Refresh token baþarýyla yenilendi: {Username}", user.Username);

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

        /// <summary>
        /// Kullanýcý çýkýþ yapar.
        /// </summary>
        /// <remarks>
        /// Geçerli bir refresh token gönderilerek sistemden çýkýþ yapýlýr.  
        /// Örnek request:
        /// 
        ///     POST /api/auth/logout
        ///     {
        ///        "refreshToken": "string"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Refresh token bilgisi</param>
        /// <response code="204">Çýkýþ baþarýlý.</response>
        /// <response code="401">Yetkisiz eriþim. JWT token eksik ya da geçersiz.</response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            _logger.LogInformation("Kullanýcý çýkýþ yaptý: KullanýcýId={UserId}", userId);

            return NoContent();
        }
    }
}
