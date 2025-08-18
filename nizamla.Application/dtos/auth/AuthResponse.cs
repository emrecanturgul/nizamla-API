using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.dtos.auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = default!;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
