using System.ComponentModel.DataAnnotations;

namespace nizamla.Application.dtos.auth
{
    public class RegisterRequest
    {
        [Required, MinLength(3), MaxLength(64)]
        public string Username { get; set; } = default!;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = default!;

        [Required, MinLength(6), MaxLength(64)]
        public string Password { get; set; } = default!;
    }
}
