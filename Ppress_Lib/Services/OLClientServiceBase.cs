namespace Ppress_Lib.Services
{
    public class OLClientServiceBase
    {
        protected string serviceUrl;
        protected OLClient client { get;}

        public OLClientServiceBase(string? service_url, OLClient? client)
        {
            if (client == null) throw new ArgumentNullException("client");
                this.client = client;

            if (service_url == null) throw new ArgumentNullException("service_url");
            
            serviceUrl = client.BaseURL +  service_url;
            if (! serviceUrl.EndsWith('/')) serviceUrl += "/";
        }
        private OLClientServiceBase()
        {
            serviceUrl = "";
            client =new OLClient();
        }

        protected void EnsureSessionActive()
        {
            if (!Handshake()) client.Login();
        }

        /// <summary>
        /// Handshake service to ensure is alive and respond
        /// </summary>
        /// <returns></returns>
        public bool Handshake()
        {
            using HttpClient http = client.GetHttpClientInstance();
            http.BaseAddress = new Uri(serviceUrl);
            return http.GetAsync(serviceUrl).Result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Retrieve the service service version
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string ServiceVersion()
        {
            EnsureSessionActive();

            using HttpRequestMessage req = new(HttpMethod.Get, $"{serviceUrl}version");
            using HttpClient http = client.GetHttpClientInstance();
            using HttpResponseMessage resp = http.SendAsync(req).Result;
            try
            {
                return resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Unable to get service version");
            }
        }
    }
}
