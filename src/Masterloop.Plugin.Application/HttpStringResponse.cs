using System.Net;

namespace Masterloop.Plugin.Application
{
    public class HttpStringResponse
    {
        public HttpStringResponse(HttpStatusCode statusCode, string statusDescription, string content)
        {
            Content = content;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public string Content { get; }
    }
}