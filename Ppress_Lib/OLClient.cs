using Ppress_Client;
using System.Net.Http.Headers;

namespace Ppress_Lib
{
    public class OLClient
    {
        public string? BaseURL { get; private set; }

        /// <summary>
        /// Session Token
        /// </summary>
        private string? SessionToken;

        /// <summary>
        /// REST API Services 
        /// </summary>
        public OLClientServices Services { get; } 

        public OLClient()
        {
            Services = new OLClientServices(this);
            
        }

        public void Login()
        {
            SessionToken = Services.Authentication.Login();
        }

        public void Login(string serverUrl, string username, string password)
        {
            BaseURL = serverUrl + "/rest/serverengine/";
            SessionToken = Services.Authentication.Login(username, password);
        }

        public string? GetSessionToken() 
        {
            return SessionToken;
        }

        /// <summary>
        /// Get a new instaance of HttpClient with auth_token
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal HttpClient GetHttpClientInstance(string? url = null)
        {
            HttpClient result = new();
            if (url != null) result.BaseAddress = new Uri(url);
            
            result.DefaultRequestHeaders.Add("auth_token", SessionToken);
            result.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            result.DefaultRequestHeaders.CacheControl = CacheControlHeaderValue.Parse("no-cache");
            result.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            result.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            result.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");



            return result;
        }
    }
}
