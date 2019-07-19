using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using System.Collections.Generic;

    /// <summary>
    /// Class to handle all IoTHub/EdgeHub communication.
    /// </summary>
    public interface IHubCommunication
    {
        /// <summary>
        /// Dictionary of available IoTHub direct methods.
        /// </summary>
        Dictionary<string, MethodCallback> IotHubDirectMethods { get; }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Initializes the hub communication.
        /// </summary>
        Task<bool> InitHubCommunicationAsync(IHubClient hubClient);

        /// <summary>
        /// Handle publish node method call.
        /// </summary>
        Task<MethodResponse> HandlePublishNodesMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle unpublish node method call.
        /// </summary>
        Task<MethodResponse> HandleUnpublishNodesMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle unpublish all nodes method call.
        /// </summary>
        Task<MethodResponse> HandleUnpublishAllNodesMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get all endpoints which published nodes.
        /// </summary>
        Task<MethodResponse> HandleGetConfiguredEndpointsMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get list of configured nodes on a specific endpoint.
        /// </summary>
        Task<MethodResponse> HandleGetConfiguredNodesOnEndpointMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get diagnostic information.
        /// </summary>
        Task<MethodResponse> HandleGetDiagnosticInfoMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get log information.
        /// </summary>
        Task<MethodResponse> HandleGetDiagnosticStartupLogMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get log information.
        /// </summary>
        Task<MethodResponse> HandleExitApplicationMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Handle method call to get application information.
        /// </summary>
        Task<MethodResponse> HandleGetInfoMethodAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Method that is called for any unimplemented call. Just returns that info to the caller
        /// </summary>
        Task<MethodResponse> DefaultMethodHandlerAsync(MethodRequest methodRequest, object userContext);

        /// <summary>
        /// Enqueue a message for sending to IoTHub.
        /// </summary>
        void Enqueue(MessageData json);

        /// <summary>
        /// Dequeue monitored item notification messages, batch them for send (if needed) and send them to IoTHub.
        /// </summary>
        Task MonitoredItemsProcessorAsync(CancellationToken ct);

        /// <summary>
        /// Exit the application.
        /// </summary>
        Task ExitApplicationAsync(int secondsTillExit);
    }
}
