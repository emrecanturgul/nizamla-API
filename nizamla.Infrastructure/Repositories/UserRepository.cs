using Microsoft.EntityFrameworkCore;
using nizamla.Application.Interfaces;
using nizamla.Core.Entities;
using nizamla.Domain.Entities;
using nizamla.Infrastructure.Data;

namespace nizamla.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context) => _context = context;

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task AddRefreshTokenAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task<List<RefreshToken>> GetRefreshTokensByUserIdAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token)
        {
            
            if (token == null) return;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
