using nizamla.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.Interfaces
{
    public interface IJwtService
    {
        (string token, DateTime expiresAt)CreateAccessToken(User user);
        Task<(string token, DateTime expiresAt)> CreateAndStoreRefreshTokenAsync(User user);
        Task<User?> ValidateRefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);  
      
    }
}
