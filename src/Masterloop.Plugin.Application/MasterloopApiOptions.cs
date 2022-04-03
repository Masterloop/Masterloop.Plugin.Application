namespace Masterloop.Plugin.Application
{
    public class MasterloopApiOptions
    {
        /// <summary>
        ///     Host to connect to, e.g. "myserver.example.com" or "10.0.0.2"
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        ///     Username of the API account
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     Password of the Api account
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     True if using HTTPS (SSL/TLS), False if using HTTP (unencrypted).
        ///     Set to 'true' by default
        /// </summary>
        public bool UseHttps { get; set; } = true;
    }
}