using System.Net;

namespace Masterloop.Plugin.Application
{
    public class HttpByteResponse
    {
        public HttpByteResponse(HttpStatusCode statusCode, string statusDescription, byte[] content)
        {
            Content = content;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public byte[] Content { get; }
    }
}