using System.Net.Http.Headers;
using System.Web;
using System.Text.Json;
using System.Net.Http.Json;

namespace Ppress_Lib.Services
{
    public class OLJobCreation : OLClientServiceBase
    {
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLJobCreation(OLClient client)
            : base("workflow/jobcreation", client)
        {
        }

        private OLJobCreation() : base(null, null)  { }


        public async Task<int> Process(string[] contentsIds, string job, Dictionary<string,string>? parameters,
            OLEvents.SendEventHandler? processEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if ((contentsIds.Length == 0) || string.IsNullOrEmpty(job))
                throw new ArgumentException("JobCreation Process: Bad arguments");

            if (processEvent != null) Onsend += processEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            int _contentId;
            
            using (HttpClient http = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(http, contentsIds, job, parameters);

                await GetProgressAsync(http, _operationId);

                _contentId = await GetResultAsync(http, _operationId);

            }


            if (processEvent != null) this.Onsend -= processEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return _contentId;


        }

        public class Config
        {
            public string[] identifiers { get; set; }
            public Dictionary<string, string>? parameters { get; set; }
            public Config(string[] identifiers, Dictionary<string, string>? parameters)
            {
                this.identifiers = identifiers;
                this.parameters = parameters;
            }
        }

        private async Task<string> SubmitAsync(HttpClient http, string[] contentIds, string template, Dictionary<string, string>? parameters)
        {
            string operationId;
            Config config = new(contentIds, parameters);
            string sConfig = JsonSerializer.Serialize(config);
            try
            {
                using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}{template}");
                req.Content = new StringContent(sConfig,new MediaTypeHeaderValue("application/json"));

                HttpResponseMessage resp = await http.SendAsync(req);

                if (!resp.IsSuccessStatusCode) 
                    throw new Exception("ContentCreation can't process this action.");
                if (!resp.Headers.TryGetValues("operationId", out IEnumerable<string>? values))
                    throw new Exception("ContentCreation can't process this action.");
                operationId = values.FirstOrDefault() ?? "";
                Onsend?.Invoke(operationId);
                return operationId;
            }
            catch (Exception)
            {
                throw new Exception("unable to process ContentCreation");
            }

        }

        public async Task<int> GetResultAsync (HttpClient http,  string operationId)
        {

            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("ContentCreation is unable to get result");
                return int.Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception)
            {
                throw new Exception("ContentCreation is unable to get result");
            }
        }

        public async Task<bool> GetProgressAsync (HttpClient http,  string operationId)
        {
            int retry = 0;
            bool done = false;

            while (!done && retry < 60)
            {
                try
                {
                    HttpResponseMessage resp = await http.GetAsync($"{serviceUrl}getProgress/{operationId}");
                    if (!resp.IsSuccessStatusCode)
                        throw new Exception("ContentCreation is unable to get progress");
                    string rs = await resp.Content.ReadAsStringAsync();
                    if (rs == "done")
                    {
                        Onprogress?.Invoke(100);
                        return true;
                    }
                    else
                        Onprogress?.Invoke(int.Parse(rs));
                }
                catch (Exception)
                {
                    throw new Exception("ContentCreation is unable to get progress");
                }                
                retry++;
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
