namespace OpcPublisher
{
    using System;
    using static Program;

    /// <summary>
    /// Class to handle all IoTEdge communication.
    /// </summary>
    public class IotEdgeHubCommunication : HubCommunicationBase
    {
        /// <summary>
        /// Edge hub connection string - if not running inside iotedge
        /// </summary>
        public static string EdgeHubConnectionString { get; set; } = null;

        /// <summary>
        /// Get the singleton.
        /// </summary>
        public static IotEdgeHubCommunication Instance
        {
            get
            {
                lock (_singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new IotEdgeHubCommunication();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public IotEdgeHubCommunication()
        {
            // connect to IoT Edge hub
            Logger.Information($"Create module client using '{HubProtocol}' for communication.");
            IHubClient hubClient = string.IsNullOrEmpty(EdgeHubConnectionString) ?
                HubClient.CreateModuleClientFromEnvironment(HubProtocol) :
                HubClient.CreateModuleClientFromConnectionString(EdgeHubConnectionString, HubProtocol);

            if (!InitHubCommunicationAsync(hubClient).Result)
            {
                string errorMessage = $"Cannot create module client. Exiting...";
                Logger.Fatal(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        private static readonly object _singletonLock = new object();
        private static IotEdgeHubCommunication _instance;
    }
}
