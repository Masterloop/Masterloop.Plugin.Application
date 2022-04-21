using System.Net;

namespace Masterloop.Plugin.Application
{
    internal class HttpTypeResponse<T>
    {
        public HttpTypeResponse(HttpStatusCode statusCode, string statusDescription)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpTypeResponse(HttpStatusCode statusCode, string statusDescription, T content): this(statusCode, statusDescription)
        {
            Content = content;
        }

        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public T Content { get; }
    }
}