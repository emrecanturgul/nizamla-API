using nizamla.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Domain.Entities
{
   public class RefreshToken
    {

        public int Id { get; set; }
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

        public int UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
