
using System.ComponentModel.DataAnnotations;

namespace nizamla.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(List<string> errors)
            : base("Doğrulama hatası")
        {
            Errors = errors;
        }

        public ValidationException(string message)
            : base("Doğrulama hatası: " + message)
        {
            Errors = new List<string> { message };
        }
    }
}
