using Ppress_Lib;

namespace Ppress_Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            //Logger.log("Connection au serveur");
            OLClient client = new();
            client.Login(Constants.ServerUrl, Constants.UserName, Constants.UserPassword);

            Logger.log($"AuthToken : {client.GetSessionToken()}");

            Logger.log("Authentication Handshake \t" + (client.Services.Authentication.Handshake() ? "OK." : "KO."));
            Logger.log("Authentication Version \t\t" +  client.Services.Authentication.ServiceVersion());

            Logger.log("DataMapping Handshake \t\t" + (client.Services.DataMapping.Handshake() ? "OK." : "KO."));
            Logger.log("DataMapping Version \t\t" + client.Services.DataMapping.ServiceVersion());

            Logger.log("FileStore Handshake \t\t" + (client.Services.FileStore.Handshake() ? "OK." : "KO."));
            Logger.log("FileStore Version \t\t" + client.Services.FileStore.ServiceVersion());


            string dmConfigPath = Constants.DataMapper;
            string dataFilePath = Constants.DataFile;
            long dataSetId = await client.Services.DataMapping.Process(dmConfigPath, dataFilePath,
                processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"DataMining Process\t\tFile={dataFilePath}, Mapper={dmConfigPath}, Result DataSet ID={dataSetId}");

            string templatePath = Constants.Template;
            long contentSetId = await client.Services.ContentCreation.Process(templatePath, dataSetId,
                processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"ContentCreation Process\t\tDataSet={dataSetId}, Template={templatePath}, Result Content ID={contentSetId}");


            string jobPresetPath = Constants.JobPreset;
            long[] contentSetIds = new long[] { contentSetId };
            Dictionary<string, string> parameters = new();
            parameters.Add("test1", "toto");
            long jobId = await client.Services.JobCreation.Process(contentSetIds, jobPresetPath,
                parameters, processEvent: ProcessSend, progressEvent: ProcessProgress);
            Logger.log($"JobCreation Process\t\t\tResult Job ID={jobId}");

            using (FileStream file = new(Constants.PDFFilename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                string outputPresetPath = Constants.OutputPreset;
                await client.Services.OutputCreation.Process(outputPresetPath, jobId, file,
                    processEvent: ProcessSend, progressEvent: ProcessProgress);

                file.Close();
            }

            client.Services.FileStore.Cleanup();

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
