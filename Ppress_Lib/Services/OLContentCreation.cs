using System.Web;

namespace Ppress_Lib.Services
{
    public class OLContentCreation : OLClientServiceBase
    {
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLContentCreation(OLClient client)
            : base("workflow/contentcreation", client)
        {
        }

        private OLContentCreation() : base(null, null)  { }


        public async Task<long> Process(string templatePath, long dataSetId, bool validate = false,
            OLEvents.SendEventHandler? processEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if (string.IsNullOrEmpty(templatePath) || dataSetId <= 0)
                throw new ArgumentException("DataMiningProcess: Bad arguments");

            long templateId = await client.Services.FileStore.UploadFileAsync(templatePath);

            if (processEvent != null) Onsend += processEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            long _contentSetId;
            
            using (HttpClient http = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(http, templateId, dataSetId, validate);

                await GetProgressAsync(http, _operationId);

                _contentSetId = await GetResultAsync(http, _operationId);
            }


            if (processEvent != null) this.Onsend -= processEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return _contentSetId;
        }

        private async Task<string> SubmitAsync(HttpClient http, long templateId, long dataSetId, bool validate = false)
        {
            string _dataSetId = HttpUtility.UrlEncode(dataSetId.ToString());
            string _templateId = HttpUtility.UrlEncode(templateId.ToString());
            string operationId;

            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}{_templateId}/{_dataSetId}", null);
                string buffer = resp.Content.ReadAsStringAsync().Result;
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

        public async Task<long> GetResultAsync (HttpClient http, string operationId)
        {
            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("ContentCreation is unable to get result");
                return long.Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception)
            {
                throw new Exception("ContentCreation is unable to get result");
            }
        }

        public async Task<bool> GetProgressAsync (HttpClient http, string operationId)
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
