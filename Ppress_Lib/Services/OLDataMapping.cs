using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Ppress_Lib.Services
{
    /// <summary>
    /// Manager class for DataMapping service
    /// </summary>
    public class OLDataMapping : OLClientServiceBase
    {
        // Events
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLDataMapping(OLClient client)
            : base("workflow/datamining", client)
        {
        }

        private OLDataMapping() : base(null, null) { }



        /// <summary>
        /// Process Mapping from name of Mapping and Datafile
        /// If files exists localy then we upload them to server
        /// </summary>
        /// <param name="dmConfigPath"></param>
        /// <param name="dataFilePath"></param>
        /// <param name="validate"></param>
        /// <param name="processEvent"></param>
        /// <param name="progressEvent"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<long> Process(string dmConfigPath, string dataFilePath, bool validate = false,
            OLEvents.SendEventHandler? processEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            // Id or name of the mapping config on the server
            string dmConfig;
            // Id or name of the datafile on the server
            string dataFile;

            // Result dataSetId
            long dataSetId;

            EnsureSessionActive();

            // Link events
            if (processEvent != null) Onsend += processEvent;
            if (progressEvent != null) Onprogress += progressEvent;

            if (string.IsNullOrEmpty(dmConfigPath) || string.IsNullOrEmpty(dataFilePath))
                throw new ArgumentException("DataMiningProcess: Bad arguments");
            
            // If Mapping config file exists localy upload it.
            if (File.Exists(dmConfigPath)) 
                dmConfig = (await client.Services.FileStore.UploadFileAsync(dmConfigPath)).ToString();
            else
                // File on  server
                dmConfig = dmConfigPath;

            if (File.Exists(dataFilePath))
                dataFile = (await client.Services.FileStore.UploadFileAsync(dataFilePath)).ToString();
            else
                // File on  server
                dataFile = dataFilePath;

            using (HttpClient httpClient = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(httpClient, dmConfig, dataFile, validate);

                await GetProgressAsync(httpClient, _operationId);

                dataSetId = await GetResultAsync(httpClient, _operationId);
            }

            // Set Events hooks
            if (processEvent != null) this.Onsend -= processEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return dataSetId;

        }

        /// <summary>
        /// Submit datamining
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="dmConfig">Id or name of DataMapping configuration</param>
        /// <param name="dataFile">Id or name of Data file </param>
        /// <param name="validate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> SubmitAsync(HttpClient httpClient, string dmConfig, string dataFile, bool validate = false)
        {
            dataFile = HttpUtility.UrlEncode(dataFile.ToString());
            dmConfig = HttpUtility.UrlEncode(dmConfig.ToString());

            // Result 
            string operationId;

            try
            {
                HttpResponseMessage resp = await httpClient.PostAsync($"{serviceUrl}{dmConfig}/{dataFile}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("DataMining can't process this action.");
                if (!resp.Headers.TryGetValues("operationId", out IEnumerable<string>? values))
                    throw new Exception("DataMining can't process this action.");
                operationId = values.FirstOrDefault() ?? "";
                Onsend?.Invoke(operationId);
                return operationId;
            }
            catch (Exception)
            {
                throw new Exception("unable to process DataMapping");
            }
        }

        /// <summary>
        /// Get the result of an operation
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<long> GetResultAsync(HttpClient httpClient, string operationId)
        {
            try
            {
                HttpResponseMessage resp = await httpClient.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("DataMining is unable to get result");
                return long.Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception)
            {
                throw new Exception("DataMining is unable to get result");
            }
        }

        /// <summary>
        /// Get the process of an operation
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> GetProgressAsync(HttpClient httpClient, string operationId)
        {
            int retry = 0;
            bool done = false;

            while (!done && retry < 60)
            {
                try
                {
                    HttpResponseMessage resp = await httpClient.GetAsync($"{serviceUrl}getProgress/{operationId}");
                    if (!resp.IsSuccessStatusCode)
                        throw new Exception("DataMining is unable to get progress");
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
                    throw new Exception("DataMining is unable to get progress");
                }
                retry++;

                // Pause since the next progress query.
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
