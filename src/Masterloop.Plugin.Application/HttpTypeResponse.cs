using System.Net;

namespace Masterloop.Plugin.Application
{
    internal class HttpTypeResponse<T> where T : class
    {
        public HttpTypeResponse(HttpStatusCode statusCode, string statusDescription, T content)
        {
            Content = content;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public T Content { get; }
    }
}