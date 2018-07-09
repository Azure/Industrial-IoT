using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using System;
    using static Program;

    /// <summary>
    /// Class to handle all IoTEdge communication.
    /// </summary>
    public class IotEdgeHubCommunication : HubCommunication
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
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_MODULEID")));
        }

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public IotEdgeHubCommunication(CancellationToken ct) : base(ct)
        {
        }

        /// <summary>
        /// Initializes the EdgeHub communication.
        /// </summary>
        public async Task<bool> InitAsync()
        {
            try
            {
                // connect to EdgeHub
                HubProtocol = TransportType.Amqp_Tcp_Only;
                Logger.Information($"Create IoTEdgeHub module client using '{HubProtocol}' for communication.");
                ModuleClient hubClient = await ModuleClient.CreateFromEnvironmentAsync(HubProtocol);

                if (await InitHubCommunicationAsync(hubClient, HubProtocol))
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in IoTEdgeHub initialization.)");
                return false;
            }
        }
    }
}
