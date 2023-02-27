using System.Net.Http.Headers;
using System.Web;

namespace Ppress_Lib.Services
{

    public class OLDataMapping : OLClientServiceBase
    {
        public event OLEvents.SendEventHandler? Onsend;
        public event OLEvents.ProgressEventHandler? Onprogress;


        public OLDataMapping(OLClient client)
            : base("workflow/datamining", client)
        {
        }

        private OLDataMapping() : base(null, null)  { }


        public async Task<int> Process(string dataFile, string fileMapper, bool validate = false,
            OLEvents.SendEventHandler? processEvent = null,
            OLEvents.ProgressEventHandler? progressEvent = null)
        {
            EnsureSessionActive();

            if (string.IsNullOrEmpty(dataFile) || string.IsNullOrEmpty(fileMapper))
                throw new ArgumentException("DataMiningProcess: Bad arguments");

            if (processEvent != null) Onsend += processEvent;
            if (progressEvent != null) Onprogress += progressEvent;
            
            int _dataSetId;
            
            using (HttpClient http = client.GetHttpClientInstance())
            {
                string _operationId = await SubmitAsync(http, dataFile, fileMapper, validate);

                await GetProgressAsync(http, _operationId);

                _dataSetId = await GetResultAsync(http, _operationId);

            }


            if (processEvent != null) this.Onsend -= processEvent;
            if (progressEvent != null) Onprogress -= progressEvent;

            return _dataSetId;


        }

        private async Task<string> SubmitAsync(HttpClient http, string dataFile, string fileMapper, bool validate = false)
        {
            string _dataFile = HttpUtility.UrlEncode(dataFile);
            string _fileMapper = HttpUtility.UrlEncode(fileMapper);
            string operationId;

            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}{_fileMapper}/{_dataFile}", null);
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

        public async Task<int> GetResultAsync (HttpClient http,  string operationId)
        {

            try
            {
                HttpResponseMessage resp = await http.PostAsync($"{serviceUrl}getResult/{operationId}", null);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("DataMining is unable to get result");
                return int.Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception)
            {
                throw new Exception("DataMining is unable to get result");
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
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
