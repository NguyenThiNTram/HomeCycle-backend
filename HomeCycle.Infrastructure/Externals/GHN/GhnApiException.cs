using HomeCycle.Application.Interfaces.Externals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public class GhnApiException : Exception, IGhnApiError
    {
        //public int GhnCode { get; }
        //public string? GhnCodeMessage { get; }

        //public GhnApiException(
        //    int ghnCode,
        //    string message,
        //    string? ghnCodeMessage = null,
        //    Exception? innerException = null)
        //    : base(message, innerException)
        //{
        //    GhnCode = ghnCode;
        //    GhnCodeMessage = ghnCodeMessage;


        public HttpStatusCode StatusCode { get; }
        public string? CodeMessage { get; }

        public GhnApiException(HttpStatusCode statusCode, string message, string? codeMessage = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            CodeMessage = codeMessage;
        }
    }
}
