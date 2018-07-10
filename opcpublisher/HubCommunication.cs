
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using static OpcPublisher.OpcMonitoredItem;
    using static OpcPublisher.PublisherNodeConfiguration;
    using static OpcPublisher.PublisherTelemetryConfiguration;
    using static OpcStackConfiguration;
    using static Program;

    /// <summary>
    /// Class to handle all IoTHub/EdgeHub communication.
    /// </summary>
    public class HubCommunication
    {
        private class IotCentralMessage
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public IotCentralMessage()
            {
                Key = null;
                Value = null;
            }
        }

        public static long MonitoredItemsQueueCount => _monitoredItemsDataQueue.Count;

        public static long DequeueCount { get; private set; }

        public static long MissedSendIntervalCount { get; private set; }

        public static long TooLargeCount { get; private set; }

        public static long SentBytes { get; private set; }

        public static long SentMessages { get; private set; }

        public static DateTime SentLastTime { get; private set; }

        public static long FailedMessages { get; private set; }

        public const uint HubMessageSizeMax = (256 * 1024);

        public static uint HubMessageSize { get; set; } = 262144;

        public static TransportType HubProtocol { get; set; } = Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only;

        public static int DefaultSendIntervalSeconds { get; set; } = 10;

        public static int MonitoredItemsQueueCapacity { get; set; } = 8192;

        public static long EnqueueCount => _enqueueCount;

        public static long EnqueueFailureCount => _enqueueFailureCount;

        public static bool IsHttp1Transport() => (_transportType == TransportType.Http1);

        public static bool IotCentralMode { get; set; } = false;


        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public HubCommunication(CancellationToken ct)
        {
            _shutdownToken = ct;
        }

        /// <summary>
        /// Initializes edge message broker communication.
        /// </summary>
        public async Task<bool> InitHubCommunicationAsync(ModuleClient edgeHubClient, TransportType transportType)
        {
            // init EdgeHub communication parameters
            _edgeHubClient = edgeHubClient;
            return await InitHubCommunicationAsync(transportType);
        }


        /// <summary>
        /// Initializes message broker communication.
        /// </summary>
        public async Task<bool> InitHubCommunicationAsync(DeviceClient iotHubClient, TransportType transportType)
        {
            // init IoTHub communication parameters
            _iotHubClient = iotHubClient;
            return await InitHubCommunicationAsync(transportType);
        }

        /// <summary>
        /// Initializes message broker communication.
        /// </summary>
        private async Task<bool> InitHubCommunicationAsync(TransportType transportType)
        {
            try
            {
                // set hub communication parameters
                _transportType = transportType;
                ExponentialBackoff exponentialRetryPolicy = new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1024), TimeSpan.FromMilliseconds(3));

                // show IoTCentral mode
                Logger.Information($"IoTCentral mode: {IotCentralMode}");


                if (_iotHubClient == null)
                {
                    _edgeHubClient.ProductInfo = "OpcPublisher";
                    _edgeHubClient.SetRetryPolicy(exponentialRetryPolicy);
                    // register connection status change handler
                    _edgeHubClient.SetConnectionStatusChangesHandler(ConnectionStatusChange);

                    // open connection
                    Logger.Debug($"Open EdgeHub communication");
                    await _edgeHubClient.OpenAsync();

                    // init twin properties and method callbacks
                    Logger.Debug($"Register desired properties and method callbacks");

                    // register property update handler
                    await _edgeHubClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertiesUpdate, null);

                    // register method handlers
                    await _edgeHubClient.SetMethodHandlerAsync("PublishNodes", HandlePublishNodesMethodAsync, _edgeHubClient);
                    await _edgeHubClient.SetMethodHandlerAsync("UnpublishNodes", HandleUnpublishNodesMethodAsync, _edgeHubClient);
                    await _edgeHubClient.SetMethodHandlerAsync("UnpublishAllNodes", HandleUnpublishAllNodesMethodAsync, _edgeHubClient);
                    await _edgeHubClient.SetMethodHandlerAsync("GetConfiguredEndpoints", HandleGetConfiguredEndpointsMethodAsync, _edgeHubClient);
                    await _edgeHubClient.SetMethodHandlerAsync("GetConfiguredNodesOnEndpoint", HandleGetConfiguredNodesOnEndpointMethodAsync, _edgeHubClient);
                }
                else
                {
                    _iotHubClient.ProductInfo = "OpcPublisher";
                    _iotHubClient.SetRetryPolicy(exponentialRetryPolicy);
                    // register connection status change handler
                    _iotHubClient.SetConnectionStatusChangesHandler(ConnectionStatusChange);

                    // open connection
                    Logger.Debug($"Open IoTHub communication");
                    await _iotHubClient.OpenAsync();

                    // init twin properties and method callbacks (not supported for HTTP)
                    if (!IsHttp1Transport())
                    {
                        Logger.Debug($"Register desired properties and method callbacks");

                        // register property update handler
                        await _iotHubClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertiesUpdate, null);

                        // register method handlers
                        await _iotHubClient.SetMethodHandlerAsync("PublishNodes", HandlePublishNodesMethodAsync, _iotHubClient);
                        await _iotHubClient.SetMethodHandlerAsync("UnpublishNodes", HandleUnpublishNodesMethodAsync, _iotHubClient);
                        await _iotHubClient.SetMethodHandlerAsync("UnpublishAllNodes", HandleUnpublishAllNodesMethodAsync, _iotHubClient);
                        await _iotHubClient.SetMethodHandlerAsync("GetConfiguredEndpoints", HandleGetConfiguredEndpointsMethodAsync, _iotHubClient);
                        await _iotHubClient.SetMethodHandlerAsync("GetConfiguredNodesOnEndpoint", HandleGetConfiguredNodesOnEndpointMethodAsync, _iotHubClient);
                    }
                }
                Logger.Debug($"Init D2C message processing");
                return await InitMessageProcessingAsync();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failure initializing hub communication processing.");
                return false;
            }
        }

        private Task DesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Logger.Debug("Desired property update:");
                Logger.Debug(JsonConvert.SerializeObject(desiredProperties));

                // todo - add handling if we use properties
            }
            catch (AggregateException e)
            {
                foreach (Exception ex in e.InnerExceptions)
                {
                    Logger.Error(ex, "Error in desired property update.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in desired property update.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle connection status change notifications.
        /// </summary>
        static void ConnectionStatusChange(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
                Logger.Information($"Connection status changed to '{status}', reason '{reason}'");
        }

        /// <summary>
        /// Handle publish node method call.
        /// </summary>
        static async Task<MethodResponse> HandlePublishNodesMethodAsync(MethodRequest methodRequest, object userContext)
        {
            Uri endpointUrl = null;
            PublishNodesMethodRequestModel publishNodesMethodData = null;
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            try
            {
                Logger.Debug("PublishNodes method called.");
                publishNodesMethodData = JsonConvert.DeserializeObject<PublishNodesMethodRequestModel>(methodRequest.DataAsJson);
                endpointUrl = new Uri(publishNodesMethodData.EndpointUrl);
            }
            catch (UriFormatException)
            {
                Logger.Error($"PublishNodesMethod: The EndpointUrl has an invalid format '{publishNodesMethodData.EndpointUrl}'!");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"PublishNodesMethod");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            // find/create a session to the endpoint URL and start monitoring the node.
            try
            {
                // lock the publishing configuration till we are done
                await OpcSessionsListSemaphore.WaitAsync();

                if (ShutdownTokenSource.IsCancellationRequested)
                {
                    Logger.Warning($"PublishNodesMethod: Publisher shutdown detected. Aborting...");
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }

                // find the session we need to monitor the node
                OpcSession opcSession = null;
                opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUrl.AbsoluteUri.Equals(endpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase));

                // add a new session.
                if (opcSession == null)
                {
                    // create new session info.
                    opcSession = new OpcSession(endpointUrl, true, OpcSessionCreationTimeout);
                    OpcSessions.Add(opcSession);
                    Logger.Information($"PublishNodesMethod: No matching session found for endpoint '{endpointUrl.OriginalString}'. Requested to create a new one.");
                }

                // process all nodes
                foreach (var node in publishNodesMethodData.Nodes)
                {
                    NodeId nodeId = null;
                    ExpandedNodeId expandedNodeId = null;
                    bool isNodeIdFormat = true;
                    try
                    {
                        if (node.Id.Contains("nsu="))
                        {
                            expandedNodeId = ExpandedNodeId.Parse(node.Id);
                            isNodeIdFormat = false;
                        }
                        else
                        {
                            nodeId = NodeId.Parse(node.Id);
                            isNodeIdFormat = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"PublishNodesMethod: The NodeId has an invalid format '{node.Id}'!");
                        continue;
                    }

                    try
                    {
                        int publishingInterval = node.OpcPublishingInterval ?? OpcPublishingInterval;
                        int samplingInterval = node.OpcSamplingInterval ?? OpcSamplingInterval;
                        if (isNodeIdFormat)
                        {
                            // add the node info to the subscription with the default publishing interval, execute syncronously
                            Logger.Information($"PublishNodesMethod: Request to monitor item with NodeId '{nodeId.ToString()}' (PublishingInterval: {publishingInterval}, SamplingInterval: {samplingInterval})");
                            statusCode = await opcSession.AddNodeForMonitoringAsync(nodeId, null, publishingInterval, samplingInterval, ShutdownTokenSource.Token);
                        }
                        else
                        {
                            // add the node info to the subscription with the default publishing interval, execute syncronously
                            Logger.Information($"PublishNodesMethod: Request to monitor item with ExpandedNodeId '{expandedNodeId.ToString()}' (PublishingInterval: {publishingInterval}, SamplingInterval: {samplingInterval})");
                            statusCode = await opcSession.AddNodeForMonitoringAsync(null, expandedNodeId, publishingInterval, samplingInterval, ShutdownTokenSource.Token);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"PublishNodesMethod: Exception while trying to configure publishing node '{(isNodeIdFormat ? nodeId.ToString() : expandedNodeId.ToString())}'");
                        return (new MethodResponse((int)HttpStatusCode.InternalServerError));
                    }
                }
            }
            catch (AggregateException e)
            {
                foreach (Exception ex in e.InnerExceptions)
                {
                    Logger.Error(ex, "Error in PublishNodesMethod method handler.");
                }
                // Indicate that the message treatment is not completed
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return (new MethodResponse((int)statusCode));
        }


        /// <summary>
        /// Handle unpublish node method call.
        /// </summary>
        static async Task<MethodResponse> HandleUnpublishNodesMethodAsync(MethodRequest methodRequest, object userContext)
        {
            NodeId nodeId = null;
            ExpandedNodeId expandedNodeId = null;
            Uri endpointUrl = null;
            bool isNodeIdFormat = true;
            UnpublishNodesMethodRequestModel unpublishNodesMethodData = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            try
            {
                Logger.Debug("UnpublishNodes method called.");
                unpublishNodesMethodData = JsonConvert.DeserializeObject<UnpublishNodesMethodRequestModel>(methodRequest.DataAsJson);
                endpointUrl = new Uri(unpublishNodesMethodData.EndpointUrl);
            }
            catch (UriFormatException)
            {
                Logger.Error($"UnpublishNodesMethod: The EndpointUrl has an invalid format '{unpublishNodesMethodData.EndpointUrl}'!");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishNodesMethod");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            // find the session and stop monitoring the node.
            try
            {
                await OpcSessionsListSemaphore.WaitAsync();
                if (ShutdownTokenSource.IsCancellationRequested)
                {
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }

                // find the session we need to monitor the node
                OpcSession opcSession = null;
                try
                {
                    opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUrl.AbsoluteUri.Equals(endpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    opcSession = null;
                }

                if (opcSession == null)
                {
                    // do nothing if there is no session for this endpoint.
                    Logger.Error($"UnpublishNodes: Session for endpoint '{endpointUrl.OriginalString}' not found.");
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }
                else
                {
                    foreach (var node in unpublishNodesMethodData.Nodes)
                    {
                        try
                        {
                            if (node.Id.Contains("nsu="))
                            {
                                expandedNodeId = ExpandedNodeId.Parse(node.Id);
                                isNodeIdFormat = false;
                            }
                            else
                            {
                                nodeId = NodeId.Parse(node.Id);
                                isNodeIdFormat = true;
                            }

                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"UnpublishNodesMethod: The NodeId has an invalid format '{node.Id}'!");
                            return (new MethodResponse((int)HttpStatusCode.InternalServerError));
                        }

                        if (isNodeIdFormat)
                        {
                            // stop monitoring the node, execute syncronously
                            Logger.Information($"UnpublishNodes: Request to stop monitoring item with NodeId '{nodeId.ToString()}')");
                            statusCode = await opcSession.RequestMonitorItemRemovalAsync(nodeId, null, ShutdownTokenSource.Token);
                        }
                        else
                        {
                            // stop monitoring the node, execute syncronously
                            Logger.Information($"UnpublishNodes: Request to stop monitoring item with ExpandedNodeId '{expandedNodeId.ToString()}')");
                            statusCode = await opcSession.RequestMonitorItemRemovalAsync(null, expandedNodeId, ShutdownTokenSource.Token);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishNodes: Exception while trying to configure publishing node '{nodeId.ToString()}'");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return (new MethodResponse((int)statusCode));
        }



        /// <summary>
        /// Handle unpublish all nodes method call.
        /// </summary>
        static async Task<MethodResponse> HandleUnpublishAllNodesMethodAsync(MethodRequest methodRequest, object userContext)
        {
            Uri endpointUrl = null;
            UnpublishAllNodesMethodRequestModel unpublishAllNodesMethodData = null;

            try
            {
                Logger.Debug("UnpublishAllNodes method called.");
                unpublishAllNodesMethodData = JsonConvert.DeserializeObject<UnpublishAllNodesMethodRequestModel>(methodRequest.DataAsJson);
                if (unpublishAllNodesMethodData != null && unpublishAllNodesMethodData.EndpointUrl != null)
                {
                    endpointUrl = new Uri(unpublishAllNodesMethodData.EndpointUrl);
                }
            }
            catch (UriFormatException)
            {
                Logger.Error($"UnpublishAllNodes: The EndpointUrl has an invalid format '{unpublishAllNodesMethodData.EndpointUrl}'!");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishAllNodes");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            // schedule to remove all nodes on all sessions
            try
            {
                await OpcSessionsListSemaphore.WaitAsync();
                if (ShutdownTokenSource.IsCancellationRequested)
                {
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }

                // loop through all sessions
                var sessionsToCleanup = OpcSessions.Where(s => (endpointUrl == null ? true : s.EndpointUrl == endpointUrl));
                foreach (var session in sessionsToCleanup)
                {
                    // cleanup a disconnected session
                    if (session.State == OpcSession.SessionState.Disconnected)
                    {
                        foreach (var subscription in session.OpcSubscriptions)
                        {
                            subscription.OpcMonitoredItems.RemoveAll(i => true);
                        }
                        session.OpcSubscriptions.RemoveAll(s => true);
                        continue;
                    }

                    // loop through all subscriptions of a connected session
                    var subscriptionsToCleanup = session.OpcSubscriptions;
                    foreach (var subscription in subscriptionsToCleanup)
                    {
                        // loop through all monitored items
                        var monitoredItemsToCleanup = subscription.OpcMonitoredItems;
                        foreach (var monitoredItem in monitoredItemsToCleanup)
                        {
                            if (monitoredItem.ConfigType == OpcMonitoredItemConfigurationType.NodeId)
                            {
                                await session.RequestMonitorItemRemovalAsync(monitoredItem.ConfigNodeId, null, ShutdownTokenSource.Token);
                            }
                            else
                            {
                                await session.RequestMonitorItemRemovalAsync(null, monitoredItem.ConfigExpandedNodeId, ShutdownTokenSource.Token);
                            }
                        }
                    }
                }
                // remove disconnected sessions data structures
                OpcSessions.RemoveAll(s => s.OpcSubscriptions.Count == 0);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishAllNodes: Exception while trying to unpublish nodes");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return (new MethodResponse((int)HttpStatusCode.OK));
        }

        /// <summary>
        /// Handle method call to get all endpoints which published nodes.
        /// </summary>
        static async Task<MethodResponse> HandleGetConfiguredEndpointsMethodAsync(MethodRequest methodRequest, object userContext)
        {
            GetConfiguredEndpointsMethodRequestModel getConfiguredEndpointsMethodRequest = null;
            try
            {
                Logger.Debug("HandleGetConfiguredEndpointsMethodAsync method called.");
                getConfiguredEndpointsMethodRequest = JsonConvert.DeserializeObject<GetConfiguredEndpointsMethodRequestModel>(methodRequest.DataAsJson);
            }
            catch (Exception e)
            {
                Logger.Error(e, "HandleGetConfiguredEndpointsMethodAsync");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            // get the list of all endpoints
            uint nodeConfigVersion = 0;
            List<Uri> endpoints = GetPublisherConfigurationFileEntries(null, false, out nodeConfigVersion).Select( e => e.EndpointUrl).ToList();
            uint endpointsCount = (uint)endpoints.Count;

            // validate version
            uint startIndex = 0;
            if (getConfiguredEndpointsMethodRequest.ContinuationToken != null)
            {
                uint requestedNodeConfigVersion = (uint)(getConfiguredEndpointsMethodRequest.ContinuationToken >> 32);
                if (nodeConfigVersion != requestedNodeConfigVersion)
                {
                    Logger.Error($"HandleGetConfiguredEndpointsMethodAsync: The node configuration has changed. Requested version: {requestedNodeConfigVersion:X8}, Current version '{nodeConfigVersion:X8}'!");
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }
                startIndex = (uint)(getConfiguredEndpointsMethodRequest.ContinuationToken & 0x0FFFFFFFFL);
            }

            // set count
            uint requestedEndpointsCount = endpointsCount - startIndex;
            uint availableEndpointCount = endpointsCount - startIndex;
            uint actualEndpointsCount = Math.Min(requestedEndpointsCount, availableEndpointCount);

            // generate response
            GetConfiguredEndpointsMethodResponseModel getConfiguredEndpointsMethodResponse = new GetConfiguredEndpointsMethodResponseModel();
            string endpointsString;
            byte[] endpointsByteArray;
            while (true)
            {
                endpointsString = JsonConvert.SerializeObject(endpoints.GetRange((int)startIndex, (int)actualEndpointsCount));
                endpointsByteArray = Encoding.UTF8.GetBytes(endpointsString);
                if (endpointsByteArray.Length > MAX_RESPONSE_PAYLOAD_LENGTH)
                {
                    actualEndpointsCount /= 2;
                    continue;
                }
                else
                {
                    break;
                }
            };

            // build response
            getConfiguredEndpointsMethodResponse.ContinuationToken = null;
            if (actualEndpointsCount < availableEndpointCount)
            {
                getConfiguredEndpointsMethodResponse.ContinuationToken = ((ulong)nodeConfigVersion << 32) | actualEndpointsCount + startIndex;
            }
            getConfiguredEndpointsMethodResponse.Endpoints = endpoints.GetRange((int)startIndex, (int)actualEndpointsCount).Select( e => e.AbsoluteUri).ToList();
            string resultString = JsonConvert.SerializeObject(getConfiguredEndpointsMethodResponse);
            byte[] result = Encoding.UTF8.GetBytes(resultString);
            MethodResponse methodResponse = new MethodResponse(result, (int)HttpStatusCode.OK);
            Logger.Information($"HandleGetConfiguredEndpointsMethodAsync: Success returning {actualEndpointsCount} endpoint(s) (node config version: {nodeConfigVersion:X8})!");
            return methodResponse;
        }

        /// <summary>
        /// Handle method call to get list of configured nodes on a specific endpoint.
        /// </summary>
        static async Task<MethodResponse> HandleGetConfiguredNodesOnEndpointMethodAsync(MethodRequest methodRequest, object userContext)
        {
            Uri endpointUrl = null;
            GetConfiguredNodesOnEndpointMethodRequestModel getConfiguredNodesOnEndpointMethodRequest = null;
            try
            {
                Logger.Debug("HandleGetConfiguredNodesOnEndpointMethodAsync method called.");
                getConfiguredNodesOnEndpointMethodRequest = JsonConvert.DeserializeObject<GetConfiguredNodesOnEndpointMethodRequestModel>(methodRequest.DataAsJson);
                endpointUrl = new Uri(getConfiguredNodesOnEndpointMethodRequest.EndpointUrl);
            }
            catch (UriFormatException)
            {
                Logger.Error($"HandleGetConfiguredNodesOnEndpointMethodAsync: The EndpointUrl has an invalid format '{getConfiguredNodesOnEndpointMethodRequest.EndpointUrl}'!");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"HandleGetConfiguredNodesOnEndpointMethodAsync");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            // get the list of published nodes for the endpoint
            uint nodeConfigVersion = 0;
            List<PublisherConfigurationFileEntry> configFileEntries = GetPublisherConfigurationFileEntries(endpointUrl, false, out nodeConfigVersion);

            // there should be only one config file entry per endpoint
            if (configFileEntries.Count != 1)
            {
                Logger.Error($"HandleGetConfiguredNodesOnEndpointMethodAsync: There are more configuration entries for endpoint '{endpointUrl}'. Aborting...");
                return (new MethodResponse((int)HttpStatusCode.InternalServerError));
            }

            List<OpcNodeOnEndpoint> opcNodes = configFileEntries.First().OpcNodes;
            uint configuredNodesOnEndpointCount = (uint)opcNodes.Count();

            // validate version
            uint startIndex = 0;
            if (getConfiguredNodesOnEndpointMethodRequest.ContinuationToken != null)
            {
                uint requestedNodeConfigVersion = (uint)(getConfiguredNodesOnEndpointMethodRequest.ContinuationToken >> 32);
                if (nodeConfigVersion != requestedNodeConfigVersion)
                {
                    Logger.Error($"HandleGetConfiguredNodesOnEndpointMethodAsync: The node configuration has changed. Requested version: {requestedNodeConfigVersion:X8}, Current version '{nodeConfigVersion:X8}'!");
                    return (new MethodResponse((int)HttpStatusCode.Gone));
                }
                startIndex = (uint)(getConfiguredNodesOnEndpointMethodRequest.ContinuationToken & 0x0FFFFFFFFL);
            }

            // set count
            uint requestedNodeCount = configuredNodesOnEndpointCount - startIndex;
            uint availableNodeCount = configuredNodesOnEndpointCount - startIndex;
            uint actualNodeCount = Math.Min(requestedNodeCount, availableNodeCount);

            // generate response
            GetConfiguredNodesOnEndpointMethodResponseModel getConfiguredNodesOnEndpointMethodResponse = new GetConfiguredNodesOnEndpointMethodResponseModel();
            string publishedNodesString;
            byte[] publishedNodesByteArray;
            while (true)
            {
                publishedNodesString = JsonConvert.SerializeObject(opcNodes.GetRange((int)startIndex, (int)actualNodeCount));
                publishedNodesByteArray = Encoding.UTF8.GetBytes(publishedNodesString);
                if (publishedNodesByteArray.Length > MAX_RESPONSE_PAYLOAD_LENGTH)
                {
                    actualNodeCount /= 2;
                    continue;
                }
                else
                {
                    break;
                }
            };

            // build response
            getConfiguredNodesOnEndpointMethodResponse.ContinuationToken = null;
            if (actualNodeCount < availableNodeCount)
            {
                getConfiguredNodesOnEndpointMethodResponse.ContinuationToken = (ulong)nodeConfigVersion << 32 | actualNodeCount + startIndex;
            }
            getConfiguredNodesOnEndpointMethodResponse.Nodes = opcNodes.GetRange((int)startIndex, (int)actualNodeCount).Select(n => new NodeModel(n.Id, n.OpcPublishingInterval, n.OpcSamplingInterval)).ToList();
            string resultString = JsonConvert.SerializeObject(getConfiguredNodesOnEndpointMethodResponse);
            byte[] result = Encoding.UTF8.GetBytes(resultString);
            MethodResponse methodResponse = new MethodResponse(result, (int)HttpStatusCode.OK);
            Logger.Information($"HandleGetConfiguredNodesOnEndpointMethodAsync: Success returning {actualNodeCount} node(s) of {availableNodeCount} (start: {startIndex}) (node config version: {nodeConfigVersion:X8})!");
            return methodResponse;
        }

        /// <summary>
        /// Initializes internal message processing.
        /// </summary>
        public async Task<bool> InitMessageProcessingAsync()
        {
            try
            {
                // show config
                Logger.Information($"Message processing and hub communication configured with a send interval of {DefaultSendIntervalSeconds} sec and a message buffer size of {HubMessageSize} bytes.");

                // create the queue for monitored items
                _monitoredItemsDataQueue = new BlockingCollection<MessageData>(MonitoredItemsQueueCapacity);

                // start up task to send telemetry to IoTHub
                _monitoredItemsProcessorTask = null;

                Logger.Information("Creating task process and batch monitored item data updates...");
                _monitoredItemsProcessorTask = Task.Run(async () => await MonitoredItemsProcessorAsync(_shutdownToken), _shutdownToken);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failure initializing message processing.");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the IoTHub communication.
        /// </summary>
        public async Task ShutdownAsync()
        {
            // send cancellation token and wait for last IoT Hub message to be sent.
            try
            {
                await _monitoredItemsProcessorTask;

                if (_iotHubClient != null)
                {
                    await _iotHubClient.CloseAsync();
                    _iotHubClient = null;
                }
                if (_edgeHubClient != null)
                {
                    await _edgeHubClient.CloseAsync();
                    _edgeHubClient = null;
                }
                _monitoredItemsDataQueue = null;
                _monitoredItemsProcessorTask = null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failure while shutting down hub messaging.");
            }
        }

        /// <summary>
        /// Enqueue a message for sending to IoTHub.
        /// </summary>
        public static void Enqueue(MessageData json)
        {
            // Try to add the message.
            Interlocked.Increment(ref _enqueueCount);
            if (_monitoredItemsDataQueue.TryAdd(json) == false)
            {
                Interlocked.Increment(ref _enqueueFailureCount);
                if (_enqueueFailureCount % 10000 == 0)
                {
                    Logger.Information($"The internal monitored item message queue is above its capacity of {_monitoredItemsDataQueue.BoundedCapacity}. We have already lost {_enqueueFailureCount} monitored item notifications:(");
                }
            }
        }

        /// <summary>
        /// Creates a JSON message to be sent to IoTHub, based on the telemetry configuration for the endpoint.
        /// </summary>
        private async Task<string> CreateJsonMessageAsync(MessageData messageData)
        {
            try
            {
                // get telemetry configration
                EndpointTelemetryConfiguration telemetryConfiguration = GetEndpointTelemetryConfiguration(messageData.EndpointUrl);

                // currently the pattern processing is done in MonitoredItem_Notification of OpcSession.cs. in case of perf issues
                // it can be also done here, the risk is then to lose messages in the communication queue. if you enable it here, disable it in OpcSession.cs
                // messageData.ApplyPatterns(telemetryConfiguration);

                // build the JSON message
                StringBuilder _jsonStringBuilder = new StringBuilder();
                StringWriter _jsonStringWriter = new StringWriter(_jsonStringBuilder);
                using (JsonWriter _jsonWriter = new JsonTextWriter(_jsonStringWriter))
                {
                    await _jsonWriter.WriteStartObjectAsync();
                    string telemetryValue = string.Empty;

                    // process EndpointUrl
                    if ((bool)telemetryConfiguration.EndpointUrl.Publish)
                    {
                        await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.EndpointUrl.Name);
                        await _jsonWriter.WriteValueAsync(messageData.EndpointUrl);
                    }

                    // process NodeId
                    if (!string.IsNullOrEmpty(messageData.NodeId))
                    {
                        await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.NodeId.Name);
                        await _jsonWriter.WriteValueAsync(messageData.NodeId);
                    }

                    // process MonitoredItem object properties
                    if (!string.IsNullOrEmpty(messageData.ApplicationUri) || !string.IsNullOrEmpty(messageData.DisplayName))
                    {
                        if (!(bool)telemetryConfiguration.MonitoredItem.Flat)
                        {
                            await _jsonWriter.WritePropertyNameAsync("MonitoredItem");
                            await _jsonWriter.WriteStartObjectAsync();
                        }

                        // process ApplicationUri
                        if (!string.IsNullOrEmpty(messageData.ApplicationUri))
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.MonitoredItem.ApplicationUri.Name);
                            await _jsonWriter.WriteValueAsync(messageData.ApplicationUri);
                        }

                        // process DisplayName
                        if (!string.IsNullOrEmpty(messageData.DisplayName))
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.MonitoredItem.DisplayName.Name);
                            await _jsonWriter.WriteValueAsync(messageData.DisplayName);
                        }

                        if (!(bool)telemetryConfiguration.MonitoredItem.Flat)
                        {
                            await _jsonWriter.WriteEndObjectAsync();
                        }
                    }

                    // process Value object properties
                    if (!string.IsNullOrEmpty(messageData.Value) || !string.IsNullOrEmpty(messageData.SourceTimestamp) ||
                       messageData.StatusCode != null || !string.IsNullOrEmpty(messageData.Status))
                    {
                        if (!(bool)telemetryConfiguration.Value.Flat)
                        {
                            await _jsonWriter.WritePropertyNameAsync("Value");
                            await _jsonWriter.WriteStartObjectAsync();
                        }

                        // process Value
                        if (!string.IsNullOrEmpty(messageData.Value))
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.Value.Value.Name);
                            if (messageData.PreserveValueQuotes)
                            {
                                await _jsonWriter.WriteValueAsync(messageData.Value);
                            }
                            else
                            {
                                await _jsonWriter.WriteRawValueAsync(messageData.Value);
                            }
                        }

                        // process SourceTimestamp
                        if (!string.IsNullOrEmpty(messageData.SourceTimestamp))
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.Value.SourceTimestamp.Name);
                            await _jsonWriter.WriteValueAsync(messageData.SourceTimestamp);
                        }

                        // process StatusCode
                        if (messageData.StatusCode != null)
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.Value.StatusCode.Name);
                            await _jsonWriter.WriteValueAsync(messageData.StatusCode);
                        }

                        // process Status
                        if (!string.IsNullOrEmpty(messageData.Status))
                        {
                            await _jsonWriter.WritePropertyNameAsync(telemetryConfiguration.Value.Status.Name);
                            await _jsonWriter.WriteValueAsync(messageData.Status);
                        }

                        if (!(bool)telemetryConfiguration.Value.Flat)
                        {
                            await _jsonWriter.WriteEndObjectAsync();
                        }
                    }
                    await _jsonWriter.WriteEndObjectAsync();
                    await _jsonWriter.FlushAsync();
                }
                return _jsonStringBuilder.ToString();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Generation of JSON message failed.");
            }
            return string.Empty;
        }

        /// <summary>
        /// Creates a JSON message to be sent to IoTCentral.
        /// </summary>
        private async Task<string> CreateIotCentralJsonMessageAsync(MessageData messageData)
        {
            try
            {
                // build the JSON message for IoTCentral
                StringBuilder _jsonStringBuilder = new StringBuilder();
                StringWriter _jsonStringWriter = new StringWriter(_jsonStringBuilder);
                using (JsonWriter _jsonWriter = new JsonTextWriter(_jsonStringWriter))
                {
                    await _jsonWriter.WriteStartObjectAsync();
                    await _jsonWriter.WritePropertyNameAsync(messageData.DisplayName);
                    await _jsonWriter.WriteValueAsync(messageData.Value);
                    await _jsonWriter.WriteEndObjectAsync();
                    await _jsonWriter.FlushAsync();
                }
                return _jsonStringBuilder.ToString();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Generation of IoTCentral JSON message failed.");
            }
            return string.Empty;
        }

        /// <summary>
        /// Dequeue monitored item notification messages, batch them for send (if needed) and send them to IoTHub.
        /// </summary>
        protected async Task MonitoredItemsProcessorAsync(CancellationToken ct)
        {
            uint jsonSquareBracketLength = 2;
            Microsoft.Azure.Devices.Client.Message tempMsg = new Microsoft.Azure.Devices.Client.Message();
            // the system properties are MessageId (max 128 byte), Sequence number (ulong), ExpiryTime (DateTime) and more. ideally we get that from the client.
            int systemPropertyLength = 128 + sizeof(ulong) + tempMsg.ExpiryTimeUtc.ToString().Length;
            // if batching is requested the buffer will have the requested size, otherwise we reserve the max size
            uint hubMessageBufferSize = (HubMessageSize > 0 ? HubMessageSize : HubMessageSizeMax) - (uint)systemPropertyLength - (uint)jsonSquareBracketLength;
            byte[] hubMessageBuffer = new byte[hubMessageBufferSize];
            MemoryStream hubMessage = new MemoryStream(hubMessageBuffer);
            DateTime nextSendTime = DateTime.UtcNow + TimeSpan.FromSeconds(DefaultSendIntervalSeconds);
            double millisecondsTillNextSend = nextSendTime.Subtract(DateTime.UtcNow).TotalMilliseconds;

            using (hubMessage)
            {
                try
                {
                    string jsonMessage = string.Empty;
                    MessageData messageData = new MessageData();
                    bool needToBufferMessage = false;
                    int jsonMessageSize = 0;

                    hubMessage.Position = 0;
                    hubMessage.SetLength(0);
                    hubMessage.Write(Encoding.UTF8.GetBytes("["), 0, 1);
                    while (true)
                    {
                        // sanity check the send interval, compute the timeout and get the next monitored item message
                        if (DefaultSendIntervalSeconds > 0)
                        {
                            millisecondsTillNextSend = nextSendTime.Subtract(DateTime.UtcNow).TotalMilliseconds;
                            if (millisecondsTillNextSend < 0)
                            {
                                MissedSendIntervalCount++;
                                // do not wait if we missed the send interval
                                millisecondsTillNextSend = 0;
                            }
                        }
                        else
                        {
                            // if we are in shutdown do not wait, else wait infinite if send interval is not set
                            millisecondsTillNextSend = ct.IsCancellationRequested ? 0 : Timeout.Infinite;
                        }
                        bool gotItem = _monitoredItemsDataQueue.TryTake(out messageData, (int)millisecondsTillNextSend, ct);

                        // the two commandline parameter --ms (message size) and --si (send interval) control when data is sent to IoTHub/EdgeHub
                        // pls see detailed comments on performance and memory consumption at https://github.com/Azure/iot-edge-opc-publisher

                        // check if we got an item or if we hit the timeout or got canceled
                        if (gotItem)
                        {
                            if (IotCentralMode)
                            {
                                // for IoTCentral we send simple key/value pairs. key is the DisplayName, value the value.
                                jsonMessage = await CreateIotCentralJsonMessageAsync(messageData);
                            }
                            else
                            {
                                // create a JSON message from the messageData object
                                jsonMessage = await CreateJsonMessageAsync(messageData);
                            }

                            DequeueCount++;
                            jsonMessageSize = Encoding.UTF8.GetByteCount(jsonMessage.ToString());

                            // sanity check that the user has set a large enough messages size
                            if ((HubMessageSize > 0 && jsonMessageSize > HubMessageSize ) || (HubMessageSize == 0 && jsonMessageSize > hubMessageBufferSize))
                            {
                                Logger.Error($"There is a telemetry message (size: {jsonMessageSize}), which will not fit into an hub message (max size: {hubMessageBufferSize}].");
                                Logger.Error($"Please check your hub message size settings. The telemetry message will be discarded silently. Sorry:(");
                                TooLargeCount++;
                                continue;
                            }

                            // if batching is requested or we need to send at intervals, batch it otherwise send it right away
                            needToBufferMessage = false;
                            if (HubMessageSize > 0 || (HubMessageSize == 0 && DefaultSendIntervalSeconds > 0))
                            {
                                // if there is still space to batch, do it. otherwise send the buffer and flag the message for later buffering
                                if (hubMessage.Position + jsonMessageSize + 1 <= hubMessage.Capacity)
                                {
                                    // add the message and a comma to the buffer
                                    hubMessage.Write(Encoding.UTF8.GetBytes(jsonMessage.ToString()), 0, jsonMessageSize);
                                    hubMessage.Write(Encoding.UTF8.GetBytes(","), 0, 1);
                                    Logger.Debug($"Added new message with size {jsonMessageSize} to hub message (size is now {(hubMessage.Position - 1)}).");
                                    continue;
                                }
                                else
                                {
                                    needToBufferMessage = true;
                                }
                            }
                        }
                        else
                        {
                            // if we got no message, we either reached the interval or we are in shutdown and have processed all messages
                            if (ct.IsCancellationRequested)
                            {
                                Logger.Information($"Cancellation requested.");
                                _monitoredItemsDataQueue.CompleteAdding();
                                _monitoredItemsDataQueue.Dispose();
                                break;
                            }
                        }

                        // the batching is completed or we reached the send interval or got a cancelation request
                        try
                        {
                            Microsoft.Azure.Devices.Client.Message encodedhubMessage = null;

                            // if we reached the send interval, but have nothing to send (only the opening square bracket is there), we continue
                            if (!gotItem && hubMessage.Position == 1)
                            {
                                nextSendTime += TimeSpan.FromSeconds(DefaultSendIntervalSeconds);
                                hubMessage.Position = 0;
                                hubMessage.SetLength(0);
                                hubMessage.Write(Encoding.UTF8.GetBytes("["), 0, 1);
                                continue;
                            }

                            // if there is no batching and not interval configured, we send the JSON message we just got, otherwise we send the buffer
                            if (HubMessageSize == 0 && DefaultSendIntervalSeconds == 0)
                            {
                                // we use also an array for a single message to make backend processing more consistent
                                encodedhubMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes("[" + jsonMessage.ToString() + "]"));
                            }
                            else
                            {
                                // remove the trailing comma and add a closing square bracket
                                hubMessage.SetLength(hubMessage.Length - 1);
                                hubMessage.Write(Encoding.UTF8.GetBytes("]"), 0, 1);
                                encodedhubMessage = new Microsoft.Azure.Devices.Client.Message(hubMessage.ToArray());
                            }
                            if (_iotHubClient != null || _edgeHubClient != null)
                            {
                                nextSendTime += TimeSpan.FromSeconds(DefaultSendIntervalSeconds);
                                try
                                {
                                    SentBytes += encodedhubMessage.GetBytes().Length;
                                    if (_iotHubClient != null)
                                    {
                                        await _iotHubClient.SendEventAsync(encodedhubMessage);
                                    }
                                    else
                                    {
                                        await _edgeHubClient.SendEventAsync(encodedhubMessage);
                                    }
                                    SentMessages++;
                                    SentLastTime = DateTime.UtcNow;
                                    Logger.Debug($"Sending {encodedhubMessage.BodyStream.Length} bytes to hub.");
                                }
                                catch
                                {
                                    FailedMessages++;
                                }

                                // reset the messaage
                                hubMessage.Position = 0;
                                hubMessage.SetLength(0);
                                hubMessage.Write(Encoding.UTF8.GetBytes("["), 0, 1);

                                // if we had not yet buffered the last message because there was not enough space, buffer it now
                                if (needToBufferMessage)
                                {
                                    // add the message and a comma to the buffer
                                    hubMessage.Write(Encoding.UTF8.GetBytes(jsonMessage.ToString()), 0, jsonMessageSize);
                                    hubMessage.Write(Encoding.UTF8.GetBytes(","), 0, 1);
                                }
                            }
                            else
                            {
                                Logger.Information("No hub client available. Dropping messages...");
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Exception while sending message to hub. Dropping message...");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!(e is OperationCanceledException))
                    {

                        Logger.Error(e, "Error while processing monitored item messages.");
                    }
                }
            }
        }

        private static int MAX_RESPONSE_PAYLOAD_LENGTH = (8 * 1024 - 256);

        private static string _hubConnectionString = string.Empty;
        private static long _enqueueCount;
        private static long _enqueueFailureCount;
        private static BlockingCollection<MessageData> _monitoredItemsDataQueue;
        private static Task _monitoredItemsProcessorTask;
        private static DeviceClient _iotHubClient;
        private static ModuleClient _edgeHubClient;
        private static TransportType _transportType;
        private static CancellationToken _shutdownToken;
    }
}
