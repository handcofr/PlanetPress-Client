using Ppress_Lib;

namespace Ppress_Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            //Logger.log("Connection au serveur");
            OLClient client = new();
            client.Login("http://s21-0101:9340", "rest-client", "pwfrest-client");

            Logger.log($"AuthToken : {client.GetSessionToken()}");

            Logger.log("Authentication Handshake \t" + (client.Services.Authentication.Handshake() ? "OK." : "KO."));
            Logger.log("Authentication Version \t\t" +  client.Services.Authentication.ServiceVersion());

            Logger.log("DataMapping Handshake \t\t" + (client.Services.DataMapping.Handshake() ? "OK." : "KO."));
            Logger.log("DataMapping Version \t\t" + client.Services.DataMapping.ServiceVersion());

            Logger.log("FileStore Handshake \t\t" + (client.Services.FileStore.Handshake() ? "OK." : "KO."));
            Logger.log("FileStore Version \t\t" + client.Services.FileStore.ServiceVersion());
            

            string filename = Constants.DataFile;
            Logger.log($"FileStore UploadDataFile \t\"{filename}\"");
            int fileId = await client.Services.FileStore.UploadDataFileAsync(filename);
            Logger.log($"FileStore UploadDataFile \tResult File Manager ID={fileId}");

            Logger.log($"FileStore UploadDataFile \t\"{filename}\" as Stream");
            FileStream fileStream = File.OpenRead(filename);
            fileId = await client.Services.FileStore.UploadDataStreamAsync(fileStream);
            fileStream.Close();
            Logger.log($"FileStore UploadDataFile \tResult File Manager ID={fileId}");

            string mapper = Constants.DataMapper;
            int DataSetId = await client.Services.DataMapping.Process(fileId.ToString(), mapper,
                processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"DataMining Process\t\tFile={fileId}, Mapper={mapper}, Result DataSet ID={DataSetId}");

            string template = Constants.Template;
            int ContentId = await client.Services.ContentCreation.Process(DataSetId.ToString(), template,
                processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"ContentCreation Process\t\tDataSet={DataSetId}, Mapper={template}, Result Content ID={ContentId}");


            string job = Constants.JobPreset;

            string[] contentIds = new string[] { ContentId.ToString() };
            Dictionary<string, string> parameters = new();
            parameters.Add("test1", "toto");
            int JobId
                = await client.Services.JobCreation.Process(contentIds, job, parameters,
                processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"JobCreation Process\t\t\tResult Job ID={JobId}");

            string OutputPreset = Constants.OutputPreset;

            using (FileStream file = new(Constants.PDFFilename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await client.Services.OutputCreation.Process(JobId, OutputPreset, file,
                processEvent: ProcessSend, progressEvent: ProcessProgress);

                file.Close();
            }


            bool result = await client.Services.FileStore.DeleteFileAsync(fileId);
            Logger.log($"FileStore Delete file \t\tid={fileId} " +  (result ? "OK" : "KO"));

            Console.WriteLine("Press any key ...");
            Console.ReadKey();
        }

        static void ProcessSend(string operationId)
        {
            Logger.log($"Send Event Operation \t\t\tId={operationId}");
        }
        static void ProcessProgress(int progress)
        {
            Logger.log($"Progress Event Operation \t\t\t{progress}");
        }

    }
}
