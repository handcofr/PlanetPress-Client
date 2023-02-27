using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ppress_Lib.Services
{
    public class OLAuthentication : OLClientServiceBase
    {
        private string? _username;
        private string? _password;

        public OLAuthentication(OLClient client) 
            : base("authentication", client)
        {
        }

        private OLAuthentication() : base(null,null)
        {

        }

        internal string Login()
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                throw new ArgumentNullException("You need to login first with credentials");
            return Login(_username, _password);
        }

        internal string Login(string username, string password)
        {
            _username = username;
            _password = password;
            string auth = $"{username}:{password}";
            string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            using HttpRequestMessage req = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{serviceUrl}login"),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            using HttpClient http = client.GetHttpClientInstance();
            using HttpResponseMessage resp = http.SendAsync(req).Result;
            try
            {
                return resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Unable to login to server");
            }
        }

    }
}
