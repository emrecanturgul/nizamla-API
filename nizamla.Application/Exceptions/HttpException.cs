
using System.Net;

namespace nizamla.Application.Exceptions
{
    public class HttpException : Exception
    {
        public int StatusCode { get; }
        public string ClientError { get; }

        public HttpException(HttpStatusCode code, string clientError, string? details = null)
            : base(details ?? clientError)
        {
            StatusCode = (int)code;
            ClientError = clientError;
        }
    }
}
