namespace Ppress_Lib.Services
{
    public class OLEvents
    {
        public delegate void SendEventHandler(string operationId);
        public delegate void ProgressEventHandler(int progress);
        public delegate void DoneEventHandler(int result);
    }
}
