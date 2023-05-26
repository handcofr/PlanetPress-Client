using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Web;

namespace Ppress_Lib.Services
{
    /// <summary>
    /// Manager class for service FileStore
    /// </summary>
    public class OLFileStore : OLClientServiceBase
    {
        // Set of files uploaded we want to delete of the cache on cleanup.
        private readonly ISet<long> uploadedFiles = new HashSet<long>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        public OLFileStore(OLClient client)
            : base("filestore", client)
        {
        }

        /// <summary>
        /// Upload arbitrary file to FileStore
        /// </summary>
        /// <param name="path">Full path of the file</param>
        /// <param name="cache">Include file or not during cleanup ?</param>
        /// <param name="persistent"></param>
        /// <returns>The Id of file in the FileStore</returns>
        /// <exception cref="FileNotFoundException">Unable to find file</exception>
        /// <exception cref="Exception"></exception>
        public async Task<long> UploadFileAsync(string path, bool cache = false, bool persistent = false)
        {
            EnsureSessionActive();

            String urlPath, mimeType, extension;
            try
            {
                extension = new FileInfo(path).Extension;
            } catch (FileNotFoundException e) {
                throw e;
            }

            switch (extension)
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
            using HttpClient httpClient = client.GetHttpClientInstance();

            try
            {
                StreamContent content = new(File.OpenRead(path));
                content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                using HttpResponseMessage resp = await httpClient.PostAsync(uriBuilder.ToString(), content);
                long id = long.Parse(resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync().Result);
    
                // Don't include in the cleanup set if we wan't to keep in server cache
                if (!cache)
                    uploadedFiles.Add(id);

                return id;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Unable to upload file to server");
            }
        }

        /// <summary>
        /// Delete file in FileStore by fileId
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Cleanup uploaded files that not stay in cache
        /// </summary>
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

        /// <summary>
        /// Disable default constructor
        /// </summary>
        private OLFileStore() : base(null, null)
        {

        }
    }
}