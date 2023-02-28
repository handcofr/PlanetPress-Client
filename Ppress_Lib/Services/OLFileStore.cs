using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Ppress_Lib.Services
{
    public class OLFileStore : OLClientServiceBase
    {
        public OLFileStore(OLClient client)
            : base("filestore", client)
        {
        }

        /// <summary>
        /// Upload data file to FileStore
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="persistent"></param>
        /// <param name="named"></param>
        /// <returns>The ID of file</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<int> UploadDataFileAsync(string filename, bool persistent = false, bool named = false)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            EnsureSessionActive();
            FileInfo fileInfo = new(filename);

            UriBuilder uriBuilder = new($"{serviceUrl}DataFile");
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            if (persistent) query.Add("persisent", "true");
            if (named) query.Add("filename", fileInfo.Name);
            uriBuilder.Query = query.ToString();
            using HttpClient http = client.GetHttpClientInstance();
            using HttpRequestMessage req = new();
            req.Headers.Add("processData", "false");

            try
            {
                StringContent content = new(await File.ReadAllTextAsync(filename),
                    Encoding.UTF8, "application/octet-stream");
                using HttpResponseMessage resp = await http.PostAsync(uriBuilder.ToString(), content);
                string buffer1 = resp.Content.ReadAsStringAsync().Result;
                return int.Parse(resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result);
            }
            catch (HttpRequestException)
            {
                throw new Exception("Unable to upload file to server");
            }

        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            EnsureSessionActive();
            using HttpClient http = client.GetHttpClientInstance($"{serviceUrl}delete/{fileId}");
            HttpRequestMessage req = new();
            using HttpResponseMessage resp = await http.SendAsync(req);
            try
            {
                return resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result == "true";
            }
            catch (HttpRequestException)
            {
                throw new Exception($"Unable to delete file {fileId}");
            }
        }

        private OLFileStore() : base(null, null)
        {

        }
    }
}