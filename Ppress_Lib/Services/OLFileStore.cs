using System.Collections.Specialized;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Web;

namespace Ppress_Lib.Services
{
    public class OLFileStore : OLClientServiceBase
    {
        private readonly ISet<long> uploadedFiles = new HashSet<long>();

        public OLFileStore(OLClient client)
            : base("filestore", client)
        {
        }

        /// <summary>
        /// Upload arbitrary file to FileStore
        /// </summary>
        /// <param name="path"></param>
        /// <param name="persistent"></param>
        /// <param name="named"></param>
        /// <returns>The ID of file</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<long> UploadFileAsync(string path, bool persistent = false)
        {
            EnsureSessionActive();

            String urlPath, mimeType;
            switch (new FileInfo(path).Extension)
            {
                case ".OL-template":
                    urlPath = "template";
                    mimeType = "application/zip";
                    break;
                case ".OL-jobpreset":
                    urlPath = "JobCreationConfig";
                    mimeType = "application/xml";
                    break;
                case ".OL-outputpreset":
                    urlPath = "OutputCreationConfig";
                    mimeType = "application/xml";
                    break;
                case ".OL-datamapper":
                    urlPath = "DataMiningConfig";
                    mimeType = "application/octet-stream";
                    break;
                default:
                    urlPath = "DataFile";
                    mimeType = "application/octet-stream";
                    break;
            }

            UriBuilder uriBuilder = new($"{serviceUrl}{urlPath}");
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            if (persistent) query.Add("persistent", persistent.ToString());
            uriBuilder.Query = query.ToString();
            using HttpClient http = client.GetHttpClientInstance();

            try
            {
                StreamContent content = new(File.OpenRead(path));
                content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                using HttpResponseMessage resp = await http.PostAsync(uriBuilder.ToString(), content);
                long id = long.Parse(resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result);
                uploadedFiles.Add(id);
                return id;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Unable to upload file to server");
            }
        }

        public async Task<bool> DeleteFileAsync(long fileId)
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

        public async void Cleanup()
        {
            try
            {
                foreach (long id in uploadedFiles)
                {
                    await DeleteFileAsync(id);
                }
            }
            finally
            {
                uploadedFiles.Clear();
            }
        }

        private OLFileStore() : base(null, null)
        {

        }
    }
}