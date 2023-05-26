namespace Ppress_Lib.Services
{
    public class OLOutputCreation : OLClientServiceBase
    {
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLOutputCreation(OLClient client)
            : base("workflow/outputcreation", client)
        {
        }

        private OLOutputCreation() : base(null, null)  { }


        public async Task<bool> Process(string outputPresetPath, long jobSetId, Stream streamResult,
            OLEvents.SendEventHandler? processEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if (jobSetId <= 0 || string.IsNullOrEmpty(outputPresetPath))
                throw new ArgumentException("JobCreation Process: Bad arguments");

            string outputPreset;

            if (File.Exists(outputPresetPath))
                outputPreset = (await client.Services.FileStore.UploadFileAsync(outputPresetPath)).ToString();
            else
                outputPreset = outputPresetPath;

            if (processEvent != null) Onsend += processEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            string _result;
            
            using (HttpClient http = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(http, outputPreset, jobSetId);

                await GetProgressAsync(http, _operationId);

                _result = await GetResultAsync(http, _operationId, streamResult);
            }


            if (processEvent != null) this.Onsend -= processEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return true;
        }

        private async Task<string> SubmitAsync(HttpClient http, string outputPresetId, long jobSetId)
        {
            string operationId;

            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}{outputPresetId}/{jobSetId}", null);
                if (!resp.IsSuccessStatusCode) 
                    throw new Exception("OutputCreation can't process this action.");
                if (!resp.Headers.TryGetValues("operationId", out IEnumerable<string>? values))
                    throw new Exception("OutputCreation can't process this action.");
                operationId = values.FirstOrDefault() ?? "";
                Onsend?.Invoke(operationId);
                return operationId;
            }
            catch (Exception)
            {
                throw new Exception("unable to process OutputCreation");
            }
        }

        public async Task<string> GetResultAsync (HttpClient http, string operationId, Stream streamResult)
        {
            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("OutputCreation is unable to get result");

                using (Stream contentStream = await resp.Content.ReadAsStreamAsync())
                {
                    await contentStream.CopyToAsync(streamResult);
                }

                return "OK";
            }
            catch (Exception)
            {
                throw new Exception("OutputCreation is unable to get result");
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
                        throw new Exception("OutputCreation is unable to get progress");
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
                    throw new Exception("OutputCreation is unable to get progress");
                }                
                retry++;
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
