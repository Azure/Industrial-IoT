namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using System;
    using static Program;

    /// <summary>
    /// Class to handle all IoTEdge communication.
    /// </summary>
    public class IotEdgeHubCommunication : HubCommunicationBase
    {
        /// <summary>
        /// Detects if publisher is running as an IoTEdge module.
        /// </summary>
        public static bool IsIotEdgeModule
        {
            get => (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_MODULEID"))) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EdgeHubConnectionString"));
        }

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
            IHubClient hubClient = HubClient.CreateModuleClientFromEnvironment(HubProtocol);

            if (!InitHubCommunicationAsync(hubClient).Result)
            {
                string errorMessage = $"Cannot create module client. Exiting...";
                Logger.Fatal(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        private static readonly object _singletonLock = new object();
        private static IotEdgeHubCommunication _instance = null;
    }
}
