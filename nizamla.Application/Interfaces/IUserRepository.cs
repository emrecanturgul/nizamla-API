using nizamla.Core.Entities;
using nizamla.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(User user);
        Task<User?> GetByEmailAsync(string email);
        Task AddRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task<List<RefreshToken>> GetRefreshTokensByUserIdAsync(int userId);

        Task RevokeRefreshTokenAsync(RefreshToken token);
        Task SaveChangesAsync();
        
    }
}
