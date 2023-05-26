using System.Net.Http.Headers;
using System.Text.Json;

namespace Ppress_Lib.Services
{
    /// <summary>
    /// Manager class for JobCreation service
    /// </summary>
    public class OLJobCreation : OLClientServiceBase
    {
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLJobCreation(OLClient client)
            : base("workflow/jobcreation", client)
        {
        }

        private OLJobCreation() : base(null, null)  { }


        /// <summary>
        /// Process JobCreation
        /// </summary>
        /// <param name="contentSetIds">List of ContentSet Ids to proceed</param>
        /// <param name="jobPresetPath">Filename of JobPreset</param>
        /// <param name="parameters">Parameters to pass to JobCreation</param>
        /// <param name="sendEvent">Event trigger when request has sent</param>
        /// <param name="progressEvent">Progress event</param>
        /// <returns>Operation Id</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<long> Process(long[] contentSetIds, string jobPresetPath, Dictionary<string, string>? parameters,
            OLEvents.SendEventHandler? sendEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if (contentSetIds.Length == 0 || string.IsNullOrEmpty(jobPresetPath))
                throw new ArgumentException("JobCreation Process: Bad arguments");

            // Id or name of preset JobConfiguration
            string jobPreset;

            // If file exists localy upload it
            if (File.Exists(jobPresetPath))
                jobPreset = (await client.Services.FileStore.UploadFileAsync(jobPresetPath)).ToString();
            else
                jobPreset = jobPresetPath;

            // Set events
            if (sendEvent != null) Onsend += sendEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            long _contentSetId;
            
            using (HttpClient httpClient = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(httpClient, contentSetIds, jobPreset, parameters);

                await GetProgressAsync(httpClient, _operationId);

                _contentSetId = await GetResultAsync(httpClient, _operationId);
            }

            // Unlink events
            if (sendEvent != null) this.Onsend -= sendEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return _contentSetId;
        }

        /// <summary>
        /// Configuration of JobCreation
        /// </summary>
        private class Config
        {
            public long[]? identifiers { get; private set; }
            public Dictionary<string, string>? parameters { get; private set; }

            /// <summary>
            /// JobCreation configuration
            /// </summary>
            /// <param name="identifiers">List of ContentsId</param>
            /// <param name="parameters">Parameters to pass to JobConfiguration</param>
            public Config(long[] identifiers, Dictionary<string, string>? parameters)
            {
                this.identifiers = identifiers;
                this.parameters = parameters;
            }

            public string ToJson()
            {
                return JsonSerializer.Serialize(this);
            }

            private Config() { }
        }

        /// <summary>
        /// Submit request to proceed JobCreation
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="contentSetIds">List of ContentSet Ids</param>
        /// <param name="jobPreset">Id or name of JobPreset</param>
        /// <param name="parameters">Parameters to pass to JobCreation</param>
        /// <returns>Operation Id</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> SubmitAsync(HttpClient httpClient, long[] contentSetIds, string jobPreset, Dictionary<string, string>? parameters)
        {
            string operationId;
            Config config = new(contentSetIds, parameters);

            try
            {
                using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}{jobPreset}");
                req.Content = new StringContent(config.ToJson(), new MediaTypeHeaderValue("application/json"));

                HttpResponseMessage resp = await httpClient.SendAsync(req);

                if (!resp.IsSuccessStatusCode) 
                    throw new Exception("JobCreation can't process this action.");
                if (!resp.Headers.TryGetValues("operationId", out IEnumerable<string>? values))
                    throw new Exception("JobCreation can't process this action.");
                operationId = values.FirstOrDefault() ?? "";
                Onsend?.Invoke(operationId);
                return operationId;
            }
            catch (Exception)
            {
                throw new Exception("unable to process ContentCreation");
            }
        }

        /// <summary>
        /// Get the result of JobCreation process
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="operationId">Opertion Id</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<long> GetResultAsync (HttpClient httpClient,  string operationId)
        {
            try
            {
                HttpResponseMessage resp = await httpClient.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("JobCreation is unable to get result");
                return long.Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception)
            {
                throw new Exception("JobCreation is unable to get result");
            }
        }

        /// <summary>
        /// Get Progress of JobCreation operation
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> GetProgressAsync (HttpClient httpClient, string operationId)
        {
            int retry = 0;

            while (retry < 60)
            {
                try
                {
                    HttpResponseMessage resp = await httpClient.GetAsync($"{serviceUrl}getProgress/{operationId}");
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
                catch (Exception e)
                {
                    throw new Exception("ContentCreation is unable to get progress",e);
                }                
                retry++;

                // Waiting for 1 seconds before asking next progress
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
