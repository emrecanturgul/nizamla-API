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
        /// Yeni kullan�c� kayd� olu�turur.
        /// </summary>
        /// <remarks>
        /// Kullan�c� ad�, e-posta ve �ifre al�r. Ba�ar�l� olursa Access + Refresh token d�ner.  
        /// �rnek request:
        /// 
        ///     POST /api/auth/register
        ///     {
        ///        "username": "ahmet",
        ///        "email": "ahmet@example.com",
        ///        "password": "Password123!"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Kay�t i�in kullan�c� ad�, e-posta ve �ifre bilgileri</param>
        /// <response code="200">Kay�t ba�ar�l�, access ve refresh token d�ner.</response>
        /// <response code="400">Do�rulama hatas�. Eksik ya da yanl�� bilgi girildi.</response>
        /// <response code="409">Kullan�c� ad� veya e-posta zaten kay�tl�.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Ge�ersiz kay�t iste�i.");

            _logger.LogInformation("Yeni kay�t denemesi: Kullan�c�Ad�={Username}, Eposta={Email}", req.Username, req.Email);

            var existingUsername = await _users.GetByUsernameAsync(req.Username);
            if (existingUsername != null)
                throw new HttpException(HttpStatusCode.Conflict, "Kullan�c� ad� zaten kay�tl�.");

            var existingEmail = await _users.GetByEmailAsync(req.Email);
            if (existingEmail != null)
                throw new HttpException(HttpStatusCode.Conflict, "Bu e-posta adresi zaten kullan�l�yor.");

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

            _logger.LogInformation("Yeni kullan�c� ba�ar�yla kay�t oldu: {Username}", user.Username);

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
        /// Kullan�c� giri�i yapar.
        /// </summary>
        /// <remarks>
        /// Kullan�c� ad� ve �ifre do�rulan�r. Ba�ar�l� olursa Access + Refresh token d�ner.  
        /// �rnek request:
        /// 
        ///     POST /api/auth/login
        ///     {
        ///        "username": "ahmet",
        ///        "password": "Password123!"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Giri� bilgileri (Username, Password)</param>
        /// <response code="200">Giri� ba�ar�l�, access ve refresh token d�ner.</response>
        /// <response code="400">Do�rulama hatas�. Eksik ya da yanl�� bilgi girildi.</response>
        /// <response code="401">Kullan�c� ad� veya �ifre yanl��.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Ge�ersiz giri� iste�i.");

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) != PasswordVerificationResult.Success)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogWarning("Ge�ersiz giri� denemesi: Kullan�c�Ad�={Username}, IP={IP}", req.Username, ip);
                throw new HttpException(HttpStatusCode.Unauthorized, "Kullan�c� ad� veya �ifre hatal�.");
            }

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Kullan�c� giri� yapt�: {Username}", user.Username);

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
        /// Ge�erli bir refresh token al�narak yeni Access ve Refresh token �retilir.  
        /// �rnek request:
        /// 
        ///     POST /api/auth/refresh
        ///     {
        ///        "refreshToken": "string"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Refresh token bilgisi</param>
        /// <response code="200">Yeni tokenler �retildi.</response>
        /// <response code="400">Do�rulama hatas�. Eksik refresh token g�nderildi.</response>
        /// <response code="401">Ge�ersiz refresh token.</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Refresh token iste�i ge�ersiz.");

            var user = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
            if (user is null)
            {
                _logger.LogWarning("Ge�ersiz refresh token: {Token}", req.RefreshToken);
                throw new HttpException(HttpStatusCode.Unauthorized, "Refresh token ge�ersiz.");
            }

            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            var (access, accessExp) = _jwt.CreateAccessToken(user);
            var (refresh, refreshExp) = await _jwt.CreateAndStoreRefreshTokenAsync(user);

            _logger.LogInformation("Refresh token ba�ar�yla yenilendi: {Username}", user.Username);

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
        /// Kullan�c� ��k�� yapar.
        /// </summary>
        /// <remarks>
        /// Ge�erli bir refresh token g�nderilerek sistemden ��k�� yap�l�r.  
        /// �rnek request:
        /// 
        ///     POST /api/auth/logout
        ///     {
        ///        "refreshToken": "string"
        ///     }
        /// 
        /// </remarks>
        /// <param name="req">Refresh token bilgisi</param>
        /// <response code="204">��k�� ba�ar�l�.</response>
        /// <response code="401">Yetkisiz eri�im. JWT token eksik ya da ge�ersiz.</response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);

            _logger.LogInformation("Kullan�c� ��k�� yapt�: Kullan�c�Id={UserId}", userId);

            return NoContent();
        }
    }
}
