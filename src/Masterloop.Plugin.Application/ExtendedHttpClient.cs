using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Masterloop.Plugin.Application
{
    public class ExtendedHttpClient
    {
        private const int DefaultTimeoutInSeconds = 30;

        private readonly HttpClient _httpClient;

        public ExtendedHttpClient(string username, string password, bool useCompression, string originAddress,
            ApplicationMetadata applicationMetadata)
        {
            Username = username;
            Password = password;
            UseCompression = useCompression;
            OriginAddress = originAddress;

            StatusCode = HttpStatusCode.Unused;
            StatusDescription = string.Empty;

            _httpClient = new HttpClient();
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = useCompression ? DecompressionMethods.GZip : DecompressionMethods.None
            };

            _httpClient = new HttpClient(httpClientHandler);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                //request.Credentials = new NetworkCredential(this.Username, this.Password);
                var authInfo = $"{Username}:{Password}";
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Base64Encode(authInfo));
            }

            SetMetaData(applicationMetadata);
            SetTimeout(DefaultTimeoutInSeconds);
            //client.ReadWriteTimeout = Timeout * 1000;
        }

        public string Username { get; }

        public string Password { get; }

        public HttpStatusCode StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public string OriginAddress { get; }

        public bool UseCompression { get; }

        public void SetTimeout(int timeInSeconds)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(timeInSeconds);
        }

        public void SetMetaData(ApplicationMetadata applicationMetadata)
        {
            if (applicationMetadata == null)
                return;

            _httpClient.DefaultRequestHeaders.Remove("OriginApplication");
            _httpClient.DefaultRequestHeaders.Remove("OriginAddress");
            _httpClient.DefaultRequestHeaders.Remove("OriginReference");

            if (!string.IsNullOrEmpty(applicationMetadata.Application))
                _httpClient.DefaultRequestHeaders.Add("OriginApplication", applicationMetadata.Application);

            if (!string.IsNullOrEmpty(OriginAddress))
                _httpClient.DefaultRequestHeaders.Add("OriginAddress", OriginAddress);

            if (!string.IsNullOrEmpty(applicationMetadata.Reference))
                _httpClient.DefaultRequestHeaders.Add("OriginReference", applicationMetadata.Reference);
        }

        public async Task<HttpStringResponse> DownloadStringAsync(string url, string accept)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

            var response = await _httpClient.SendAsync(request);
            if (response == null)
                return null;

            StatusCode = response.StatusCode;
            StatusDescription = response.ReasonPhrase;

            return new HttpStringResponse(response.StatusCode, response.ReasonPhrase,
                response.IsSuccessStatusCode ? await response.Content?.ReadAsStringAsync() : null);
        }

        public async Task<HttpByteResponse> DownloadBytesAsync(string url, string accept)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

            var response = await _httpClient.SendAsync(request);
            if (response == null)
                return null;

            StatusCode = response.StatusCode;
            StatusDescription = response.ReasonPhrase;

            return new HttpByteResponse(response.StatusCode, response.ReasonPhrase,
                response.IsSuccessStatusCode ? await response.Content?.ReadAsByteArrayAsync() : null);
        }

        public async Task<HttpStringResponse> UploadStringAsync(string url, string body, string accept,
            string contentType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

            var response = await _httpClient.SendAsync(request);
            if (response == null)
                return null;

            StatusCode = response.StatusCode;
            StatusDescription = response.ReasonPhrase;

            return new HttpStringResponse(response.StatusCode, response.ReasonPhrase,
                response.IsSuccessStatusCode ? await response.Content?.ReadAsStringAsync() : null);
        }

        public async Task<HttpStringResponse> DeleteAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            var response = await _httpClient.SendAsync(request);
            if (response == null)
                return null;

            StatusCode = response.StatusCode;
            StatusDescription = response.ReasonPhrase;

            return new HttpStringResponse(response.StatusCode, response.ReasonPhrase,
                response.IsSuccessStatusCode ? await response.Content?.ReadAsStringAsync() : null);
        }

        private static string Base64Encode(string textToEncode)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(textToEncode));
        }
    }
}