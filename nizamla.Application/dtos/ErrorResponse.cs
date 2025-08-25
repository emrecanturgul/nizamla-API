using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.dtos
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TraceId { get; set; }
        public List<string>? Errors { get; set; } 
        public static ErrorResponse FromException(Exception ex , int statusCode = 500 , string? traceId = null)
        {
            return new ErrorResponse
            {
                StatusCode = statusCode,
                Message = ex.Message,
                TraceId = traceId,
                Errors = new List<string> { ex.ToString() }
            };
        }

    }
}
