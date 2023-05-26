using System.Web;

namespace Ppress_Lib.Services
{
    /// <summary>
    /// Manager class for ContentCreation service
    /// </summary>
    public class OLContentCreation : OLClientServiceBase
    {
        // Events
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLContentCreation(OLClient client)
            : base("workflow/contentcreation", client)
        {
        }

        private OLContentCreation() : base(null, null)  { }

        /// <summary>
        /// Proceed ContentCreation
        /// </summary>
        /// <param name="templatePath">Id or name of tempplate</param>
        /// <param name="dataSetId">DataSet Id</param>
        /// <param name="validate"></param>
        /// <param name="sendEvent">On send event</param>
        /// <param name="progressEvent">Progress event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<long> Process(string templatePath, long dataSetId, bool validate = false,
            OLEvents.SendEventHandler? sendEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if (string.IsNullOrEmpty(templatePath) || dataSetId <= 0)
                throw new ArgumentException("DataMiningProcess: Bad arguments");

            string template;

            // If file exists localy upload it
            if (File.Exists(templatePath))
                template = (await client.Services.FileStore.UploadFileAsync(templatePath)).ToString();
            else
                template = templatePath;

            // Set events hooks
            if (sendEvent != null) Onsend += sendEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            long _contentSetId;
            
            using (HttpClient httpClient = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(httpClient, template, dataSetId, validate);

                await GetProgressAsync(httpClient, _operationId);

                _contentSetId = await GetResultAsync(httpClient, _operationId);
            }

            // Unset events
            if (sendEvent != null) this.Onsend -= sendEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return _contentSetId;
        }

        /// <summary>
        /// Submit ContentCreation request
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="template">Template Id or name</param>
        /// <param name="dataSetId">DataSet Id</param>
        /// <param name="validate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> SubmitAsync(HttpClient httpClient, string template, long dataSetId, bool validate = false)
        {
            string _dataSetId = HttpUtility.UrlEncode(dataSetId.ToString());
            template = HttpUtility.UrlEncode(template);
            string operationId;

            try
            {
                HttpResponseMessage resp = await httpClient.PostAsync($"{serviceUrl}{template}/{_dataSetId}", null);
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

        /// <summary>
        /// Get result of ContentCreation
        /// </summary>
        /// <param name="http"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
