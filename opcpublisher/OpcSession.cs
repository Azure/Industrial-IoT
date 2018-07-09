
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpcPublisher
{
    using Opc.Ua;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using static HubCommunication;
    using static OpcPublisher.OpcMonitoredItem;
    using static OpcPublisher.PublisherTelemetryConfiguration;
    using static OpcStackConfiguration;
    using static Program;
    using static PublisherNodeConfiguration;

    /// <summary>
    /// Class to manage the OPC monitored items, which are the nodes we need to publish.
    /// </summary>
    public class OpcMonitoredItem
    {
        public enum OpcMonitoredItemState
        {
            Unmonitored = 0,
            UnmonitoredNamespaceUpdateRequested,
            Monitored,
            RemovalRequested,
        }

        public enum OpcMonitoredItemConfigurationType
        {
            NodeId = 0,
            ExpandedNodeId
        }

        public string DisplayName { get; set; }
        public OpcMonitoredItemState State { get; set; }
        public uint AttributeId { get; set; }
        public MonitoringMode MonitoringMode { get; set; }
        public int RequestedSamplingInterval { get; set; }
        public int SamplingInterval { get; set; }
        public uint QueueSize { get; set; }
        public bool DiscardOldest { get; set; }
        public MonitoredItemNotificationEventHandler Notification { get; set; }
        public Uri EndpointUrl { get; set; }
        public MonitoredItem OpcUaClientMonitoredItem { get; set; }
        public NodeId ConfigNodeId { get; set; }
        public ExpandedNodeId ConfigExpandedNodeId { get; set; }
        public string OriginalId { get; set; }
        public OpcMonitoredItemConfigurationType ConfigType { get; set; }

        /// <summary>
        /// Ctor using NodeId (ns syntax for namespace).
        /// </summary>
        public OpcMonitoredItem(NodeId nodeId, Uri sessionEndpointUrl)
        {
            ConfigNodeId = nodeId;
            ConfigExpandedNodeId = null;
            OriginalId = nodeId.ToString();
            ConfigType = OpcMonitoredItemConfigurationType.NodeId;
            Init(sessionEndpointUrl);
            State = OpcMonitoredItemState.Unmonitored;
        }

        /// <summary>
        /// Ctor using ExpandedNodeId (nsu syntax for namespace).
        /// </summary>
        public OpcMonitoredItem(ExpandedNodeId expandedNodeId, Uri sessionEndpointUrl)
        {
            ConfigNodeId = null;
            ConfigExpandedNodeId = expandedNodeId;
            OriginalId = expandedNodeId.ToString();
            ConfigType = OpcMonitoredItemConfigurationType.ExpandedNodeId;
            Init(sessionEndpointUrl);
            State = OpcMonitoredItemState.UnmonitoredNamespaceUpdateRequested;
        }

        /// <summary>
        /// Checks if the monitored item does monitor the node described by the given objects.
        /// </summary>
        public bool IsMonitoringThisNode(NodeId nodeId, ExpandedNodeId expandedNodeId, NamespaceTable namespaceTable)
        {
            if (State == OpcMonitoredItemState.RemovalRequested)
            {
                return false;
            }
            if (ConfigType == OpcMonitoredItemConfigurationType.NodeId)
            {
                if (nodeId != null)
                {
                    if (ConfigNodeId == nodeId)
                    {
                        return true;
                    }
                }
                if (expandedNodeId != null)
                {
                    string namespaceUri = namespaceTable.ToArray().ElementAtOrDefault(ConfigNodeId.NamespaceIndex);
                    if (expandedNodeId.NamespaceUri != null && expandedNodeId.NamespaceUri.Equals(namespaceUri, StringComparison.OrdinalIgnoreCase))
                    {
                        if (expandedNodeId.Identifier.ToString().Equals(ConfigNodeId.Identifier.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            if (ConfigType == OpcMonitoredItemConfigurationType.ExpandedNodeId)
            {
                if (nodeId != null)
                {
                    int namespaceIndex = namespaceTable.GetIndex(ConfigExpandedNodeId?.NamespaceUri);
                    if (nodeId.NamespaceIndex == namespaceIndex)
                    {
                        if (nodeId.Identifier.ToString().Equals(ConfigExpandedNodeId.Identifier.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                if (expandedNodeId != null)
                {
                    if (ConfigExpandedNodeId.NamespaceUri != null && 
                        ConfigExpandedNodeId.NamespaceUri.Equals(expandedNodeId.NamespaceUri, StringComparison.OrdinalIgnoreCase) &&
                        ConfigExpandedNodeId.Identifier.ToString().Equals(expandedNodeId.Identifier.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Class used to pass data from the MonitoredItem notification to the hub message processing.
        /// </summary>
        public class MessageData
        {
            public string EndpointUrl;
            public string NodeId;
            public string ApplicationUri;
            public string DisplayName;
            public string Value;
            public string SourceTimestamp;
            public uint? StatusCode;
            public string Status;
            public bool PreserveValueQuotes;

            public MessageData()
            {
                EndpointUrl = null;
                NodeId = null;
                ApplicationUri = null;
                DisplayName = null;
                Value = null;
                StatusCode = null;
                SourceTimestamp = null;
                Status = null;
                PreserveValueQuotes = false;
            }

            public void ApplyPatterns(EndpointTelemetryConfiguration telemetryConfiguration)
            {
                if (telemetryConfiguration.EndpointUrl.Publish == true)
                {
                    EndpointUrl = telemetryConfiguration.EndpointUrl.PatternMatch(EndpointUrl);
                }
                if (telemetryConfiguration.NodeId.Publish == true)
                {
                    NodeId = telemetryConfiguration.NodeId.PatternMatch(NodeId);
                }
                if (telemetryConfiguration.MonitoredItem.ApplicationUri.Publish == true)
                {
                    ApplicationUri = telemetryConfiguration.MonitoredItem.ApplicationUri.PatternMatch(ApplicationUri);
                }
                if (telemetryConfiguration.MonitoredItem.DisplayName.Publish == true)
                {
                    DisplayName = telemetryConfiguration.MonitoredItem.DisplayName.PatternMatch(DisplayName);
                }
                if (telemetryConfiguration.Value.Value.Publish == true)
                {
                    Value = telemetryConfiguration.Value.Value.PatternMatch(Value);
                }
                if (telemetryConfiguration.Value.SourceTimestamp.Publish == true)
                {
                    SourceTimestamp = telemetryConfiguration.Value.SourceTimestamp.PatternMatch(SourceTimestamp);
                }
                if (telemetryConfiguration.Value.StatusCode.Publish == true && StatusCode != null)
                {
                    if (!string.IsNullOrEmpty(telemetryConfiguration.Value.StatusCode.Pattern))
                    {
                        Logger.Information($"'Pattern' settngs for StatusCode are ignored.");
                    }
                }
                if (telemetryConfiguration.Value.Status.Publish == true)
                {
                    Status = telemetryConfiguration.Value.Status.PatternMatch(Status);
                }
            }
        }


        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server.
        /// </summary>
        public void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
        {
            try
            {
                if (args == null || args.NotificationValue == null || monitoredItem == null || monitoredItem.Subscription == null || monitoredItem.Subscription.Session == null)
                {
                    return;
                }

                MonitoredItemNotification notification = args.NotificationValue as MonitoredItemNotification;
                if (notification == null)
                {
                    return;
                }

                DataValue value = notification.Value as DataValue;
                if (value == null)
                {
                    return;
                }

                MessageData messageData = new MessageData();
                if (IotCentralMode)
                {
                    // for IoTCentral we use the DisplayName as the key in the telemetry and the Value as the value.
                    if (monitoredItem.DisplayName != null)
                    {
                        // use the DisplayName as reported in the MonitoredItem
                        messageData.DisplayName = monitoredItem.DisplayName;
                    }
                    if (value.Value != null)
                    {
                        // use the Value as reported in the notification event argument encoded with the OPC UA JSON endcoder
                        JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                        value.ServerTimestamp = DateTime.MinValue;
                        value.SourceTimestamp = DateTime.MinValue;
                        value.StatusCode = StatusCodes.Good;
                        encoder.WriteDataValue("Value", value);
                        string valueString = encoder.CloseAndReturnText();
                        // we only want the value string, search for everything till the real value starts
                        // and get it
                        string marker = "{\"Value\":{\"Value\":";
                        int markerStart = valueString.IndexOf(marker);
                        messageData.PreserveValueQuotes = true;
                        if (markerStart >= 0)
                        {
                            // we either have a value in quotes or just a value
                            int valueLength;
                            int valueStart = marker.Length;
                            if (valueString.IndexOf("\"", valueStart) >= 0)
                            {
                                // value is in quotes and two closing curly brackets at the end
                                valueStart++;
                                valueLength = valueString.Length - valueStart - 3;
                            }
                            else
                            {
                                // value is without quotes with two curly brackets at the end
                                valueLength = valueString.Length - marker.Length - 2;
                                messageData.PreserveValueQuotes = false;
                            }
                            messageData.Value = valueString.Substring(valueStart, valueLength);
                        }
                        Logger.Debug($"   IoTCentral key: {messageData.DisplayName}");
                        Logger.Debug($"   IoTCentral values: {messageData.Value}");
                    }
                }
                else
                {
                    // update the required message data to pass only the required data to HubCommunication
                    EndpointTelemetryConfiguration telemetryConfiguration = GetEndpointTelemetryConfiguration(EndpointUrl.AbsoluteUri);

                    // the endpoint URL is required to allow HubCommunication lookup the telemetry configuration
                    messageData.EndpointUrl = EndpointUrl.AbsoluteUri;
                    if (telemetryConfiguration.NodeId.Publish == true)
                    {
                        messageData.NodeId = OriginalId;
                    }
                    if (telemetryConfiguration.MonitoredItem.ApplicationUri.Publish == true)
                    {
                        messageData.ApplicationUri = (monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri + (string.IsNullOrEmpty(OpcSession.PublisherSite) ? "" : $":{OpcSession.PublisherSite}"));
                    }
                    if (telemetryConfiguration.MonitoredItem.DisplayName.Publish == true && monitoredItem.DisplayName != null)
                    {
                        // use the DisplayName as reported in the MonitoredItem
                        messageData.DisplayName = monitoredItem.DisplayName;
                    }
                    if (telemetryConfiguration.Value.SourceTimestamp.Publish == true && value.SourceTimestamp != null)
                    {
                        // use the SourceTimestamp as reported in the notification event argument in ISO8601 format
                        messageData.SourceTimestamp = value.SourceTimestamp.ToString("o");
                    }
                    if (telemetryConfiguration.Value.StatusCode.Publish == true && value.StatusCode != null)
                    {
                        // use the StatusCode as reported in the notification event argument
                        messageData.StatusCode = value.StatusCode.Code;
                    }
                    if (telemetryConfiguration.Value.Status.Publish == true && value.StatusCode != null)
                    {
                        // use the StatusCode as reported in the notification event argument to lookup the symbolic name
                        messageData.Status = StatusCode.LookupSymbolicId(value.StatusCode.Code);
                    }
                    if (telemetryConfiguration.Value.Value.Publish == true && value.Value != null)
                    {
                        // use the Value as reported in the notification event argument encoded with the OPC UA JSON endcoder
                        JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                        value.ServerTimestamp = DateTime.MinValue;
                        value.SourceTimestamp = DateTime.MinValue;
                        value.StatusCode = StatusCodes.Good;
                        encoder.WriteDataValue("Value", value);
                        string valueString = encoder.CloseAndReturnText();
                        // we only want the value string, search for everything till the real value starts
                        // and get it
                        string marker = "{\"Value\":{\"Value\":";
                        int markerStart = valueString.IndexOf(marker);
                        messageData.PreserveValueQuotes = true;
                        if (markerStart >= 0)
                        {
                            // we either have a value in quotes or just a value
                            int valueLength;
                            int valueStart = marker.Length;
                            if (valueString.IndexOf("\"", valueStart) >= 0)
                            {
                                // value is in quotes and two closing curly brackets at the end
                                valueStart++;
                                valueLength = valueString.Length - valueStart - 3;
                            }
                            else
                            {
                                // value is without quotes with two curly brackets at the end
                                valueLength = valueString.Length - marker.Length - 2;
                                messageData.PreserveValueQuotes = false;
                            }
                            messageData.Value = valueString.Substring(valueStart, valueLength);
                        }
                    }

                    // currently the pattern processing is done here, which adds runtime to the notification processing.
                    // In case of perf issues it can be also done in CreateJsonMessageAsync of IoTHubMessaging.cs.

                    // apply patterns
                    messageData.ApplyPatterns(telemetryConfiguration);

                    Logger.Debug($"   ApplicationUri: {messageData.ApplicationUri}");
                    Logger.Debug($"   EndpointUrl: {messageData.EndpointUrl}");
                    Logger.Debug($"   DisplayName: {messageData.DisplayName}");
                    Logger.Debug($"   Value: {messageData.Value}");
                }

                // add message to fifo send queue
                if (monitoredItem.Subscription == null)
                {
                    Logger.Debug($"Subscription already removed. No more details available.");
                }
                else
                {
                    Logger.Debug($"Enqueue a new message from subscription {(monitoredItem.Subscription == null ? "removed" : monitoredItem.Subscription.Id.ToString())}");
                    Logger.Debug($" with publishing interval: {monitoredItem.Subscription.PublishingInterval} and sampling interval: {monitoredItem.SamplingInterval}):");
                }
                HubCommunication.Enqueue(messageData);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error processing monitored item notification");
            }
        }

        /// <summary>
        /// Init instance variables.
        /// </summary>
        private void Init(Uri sessionEndpointUrl)
        {
            State = OpcMonitoredItemState.Unmonitored;
            DisplayName = string.Empty;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            RequestedSamplingInterval = OpcSamplingInterval;
            QueueSize = 0;
            DiscardOldest = true;
            Notification = new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
            EndpointUrl = sessionEndpointUrl;
        }
    }

    /// <summary>
    /// Class to manage OPC subscriptions. We create a subscription for each different publishing interval
    /// on an Endpoint.
    /// </summary>
    public class OpcSubscription
    {
        public List<OpcMonitoredItem> OpcMonitoredItems;
        public int RequestedPublishingInterval;
        public double PublishingInterval;
        public Subscription OpcUaClientSubscription;

        public OpcSubscription(int? publishingInterval)
        {
            RequestedPublishingInterval = publishingInterval ?? OpcPublishingInterval;
            PublishingInterval = RequestedPublishingInterval;
            OpcMonitoredItems = new List<OpcMonitoredItem>();
        }
    }

    /// <summary>
    /// Class to manage OPC sessions.
    /// </summary>
    public class OpcSession
    {
        public enum SessionState
        {
            Disconnected = 0,
            Connecting,
            Connected,
        }

        public static bool FetchOpcNodeDisplayName { get; set; } = false;

        public static string PublisherSite { get; set; }

        public static Int32 NodeConfigVersion = 0;

        public Uri EndpointUrl;

        public Session OpcUaClientSession;

        public SessionState State;

        public List<OpcSubscription> OpcSubscriptions;

        public uint UnsuccessfulConnectionCount;

        public uint MissedKeepAlives;

        public int PublishingInterval;

        public uint SessionTimeout { get; }

        public bool UseSecurity { get; set; } = true;

        public int GetNumberOfOpcSubscriptions()
        {
            int result = 0;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;
                if (sessionLocked)
                {
                    result = OpcSubscriptions.Count();
                }
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return result;
        }

        public int GetNumberOfOpcMonitoredItems()
        {
            int result = 0;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;
                if (sessionLocked)
                {
                    var subscriptions = OpcSessions.SelectMany(s => s.OpcSubscriptions);
                    foreach (var subscription in subscriptions)
                    {
                        result += subscription.OpcMonitoredItems.Count(i => i.State == OpcMonitoredItemState.Monitored);
                    }
                }
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return result;
        }

        /// <summary>
        /// Ctor for the session.
        /// </summary>
        public OpcSession(Uri endpointUrl, bool useSecurity, uint sessionTimeout)
        {
            State = SessionState.Disconnected;
            EndpointUrl = endpointUrl;
            SessionTimeout = sessionTimeout * 1000;
            OpcSubscriptions = new List<OpcSubscription>();
            UnsuccessfulConnectionCount = 0;
            MissedKeepAlives = 0;
            PublishingInterval = OpcPublishingInterval;
            UseSecurity = useSecurity;
            _sessionCancelationTokenSource = new CancellationTokenSource();
            _sessionCancelationToken = _sessionCancelationTokenSource.Token;
            _opcSessionSemaphore = new SemaphoreSlim(1);
            _namespaceTable = new NamespaceTable();
            _telemetryConfiguration = GetEndpointTelemetryConfiguration(endpointUrl.AbsoluteUri);
        }

        /// <summary>
        /// This task is executed regularily and ensures:
        /// - disconnected sessions are reconnected.
        /// - monitored nodes are no longer monitored if requested to do so.
        /// - monitoring for a node starts if it is required.
        /// - unused subscriptions (without any nodes to monitor) are removed.
        /// - sessions with out subscriptions are removed.
        /// </summary>
        public async Task ConnectAndMonitorAsync(CancellationToken ct)
        {
            uint lastNodeConfigVersion = 0;
            try
            {
                await ConnectSessionAsync(ct);

                await MonitorNodesAsync(ct);

                await StopMonitoringNodesAsync(ct);

                await RemoveUnusedSubscriptionsAsync(ct);

                await RemoveUnusedSessionsAsync(ct);

                // update the config file if required
                if (NodeConfigVersion != lastNodeConfigVersion)
                {
                    lastNodeConfigVersion = (uint)NodeConfigVersion;
                    await UpdateNodeConfigurationFileAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in ConnectAndMonitorAsync.");
            }
        }

        /// <summary>
        /// Connects the session if it is disconnected.
        /// </summary>
        public async Task ConnectSessionAsync(CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                EndpointDescription selectedEndpoint = null;
                ConfiguredEndpoint configuredEndpoint = null;
                sessionLocked = await LockSessionAsync();

                // if the session is already connected or connecting or shutdown in progress, return
                if (!sessionLocked || ct.IsCancellationRequested || State == SessionState.Connected || State == SessionState.Connecting)
                {
                    return;
                }

                Logger.Information($"Connect and monitor session and nodes on endpoint '{EndpointUrl.AbsoluteUri}'.");
                State = SessionState.Connecting;
                try
                {
                    // release the session to not block for high network timeouts.
                    ReleaseSession();
                    sessionLocked = false;

                    // start connecting
                    selectedEndpoint = CoreClientUtils.SelectEndpoint(EndpointUrl.AbsoluteUri, UseSecurity);
                    configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(PublisherOpcApplicationConfiguration));
                    uint timeout = SessionTimeout * ((UnsuccessfulConnectionCount >= OpcSessionCreationBackoffMax) ? OpcSessionCreationBackoffMax : UnsuccessfulConnectionCount + 1);
                    Logger.Information($"Create {(UseSecurity ? "secured" : "unsecured")} session for endpoint URI '{EndpointUrl.AbsoluteUri}' with timeout of {timeout} ms.");
                    OpcUaClientSession = await Session.Create(
                            PublisherOpcApplicationConfiguration,
                            configuredEndpoint,
                            true,
                            false,
                            PublisherOpcApplicationConfiguration.ApplicationName,
                            timeout,
                            new UserIdentity(new AnonymousIdentityToken()),
                            null);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Session creation to endpoint '{EndpointUrl.AbsoluteUri}' failed {++UnsuccessfulConnectionCount} time(s). Please verify if server is up and Publisher configuration is correct.");
                    State = SessionState.Disconnected;
                    OpcUaClientSession = null;
                    return;
                }
                finally
                {
                    if (OpcUaClientSession != null)
                    {
                        sessionLocked = await LockSessionAsync();
                        if (sessionLocked)
                        {
                            Logger.Information($"Session successfully created with Id {OpcUaClientSession.SessionId}.");
                            if (!selectedEndpoint.EndpointUrl.Equals(configuredEndpoint.EndpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Information($"the Server has updated the EndpointUrl to '{selectedEndpoint.EndpointUrl}'");
                            }

                            // init object state and install keep alive
                            UnsuccessfulConnectionCount = 0;
                            OpcUaClientSession.KeepAliveInterval = OpcKeepAliveIntervalInSec * 1000;
                            OpcUaClientSession.KeepAlive += StandardClient_KeepAlive;

                            // fetch the namespace array and cache it. it will not change as long the session exists.
                            DataValue namespaceArrayNodeValue = OpcUaClientSession.ReadValue(VariableIds.Server_NamespaceArray);
                            _namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                            // show the available namespaces
                            Logger.Information($"The session to endpoint '{selectedEndpoint.EndpointUrl}' has {_namespaceTable.Count} entries in its namespace array:");
                            int i = 0;
                            foreach (var ns in _namespaceTable.ToArray())
                            {
                                Logger.Information($"Namespace index {i++}: {ns}");
                            }

                            // fetch the minimum supported item sampling interval from the server.
                            DataValue minSupportedSamplingInterval = OpcUaClientSession.ReadValue(VariableIds.Server_ServerCapabilities_MinSupportedSampleRate);
                            _minSupportedSamplingInterval = minSupportedSamplingInterval.GetValue(0);
                            Logger.Information($"The server on endpoint '{selectedEndpoint.EndpointUrl}' supports a minimal sampling interval of {_minSupportedSamplingInterval} ms.");
                            State = SessionState.Connected;
                        }
                        else
                        {
                            State = SessionState.Disconnected;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in ConnectSessions.");
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
        }

        /// <summary>
        /// Monitoring for a node starts if it is required.
        /// </summary>
        public async Task<bool> MonitorNodesAsync(CancellationToken ct)
        {
            bool requestConfigFileUpdate = false;
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();

                // if the session is not connected or shutdown in progress, return
                if (!sessionLocked || ct.IsCancellationRequested || State != SessionState.Connected)
                {
                    return requestConfigFileUpdate;
                }

                // ensure all nodes in all subscriptions of this session are monitored.
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    // create the subscription, if it is not yet there.
                    if (opcSubscription.OpcUaClientSubscription == null)
                    {
                        int revisedPublishingInterval;
                        opcSubscription.OpcUaClientSubscription = CreateSubscription(opcSubscription.RequestedPublishingInterval, out revisedPublishingInterval);
                        opcSubscription.PublishingInterval = revisedPublishingInterval;
                        Logger.Information($"Create subscription on endpoint '{EndpointUrl.AbsoluteUri}' requested OPC publishing interval is {opcSubscription.RequestedPublishingInterval} ms. (revised: {revisedPublishingInterval} ms)");
                    }

                    // process all unmonitored items.
                    var unmonitoredItems = opcSubscription.OpcMonitoredItems.Where(i => (i.State == OpcMonitoredItemState.Unmonitored || i.State == OpcMonitoredItemState.UnmonitoredNamespaceUpdateRequested));
                    int additionalMonitoredItemsCount = 0;
                    int monitoredItemsCount = 0;
                    bool haveUnmonitoredItems = false;
                    if (unmonitoredItems.Count() != 0)
                    {
                        haveUnmonitoredItems = true;
                        monitoredItemsCount = opcSubscription.OpcMonitoredItems.Count(i => (i.State == OpcMonitoredItemState.Monitored));
                        Logger.Information($"Start monitoring items on endpoint '{EndpointUrl.AbsoluteUri}'. Currently monitoring {monitoredItemsCount} items.");
                    }

                    // init perf data
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    foreach (var item in unmonitoredItems)
                    {
                        // if the session is not connected or a shutdown is in progress, we stop trying and wait for the next cycle
                        if (ct.IsCancellationRequested || State != SessionState.Connected)
                        {
                            break;
                        }

                        NodeId currentNodeId = null;
                        try
                        {
                            // update the namespace of the node if requested. there are two cases where this is requested:
                            // 1) publishing requests via the OPC server method are raised using a NodeId format. for those
                            //    the NodeId format is converted into an ExpandedNodeId format
                            // 2) ExpandedNodeId configuration file entries do not have at parsing time a session to get
                            //    the namespace index. this is set now.
                            if (item.State == OpcMonitoredItemState.UnmonitoredNamespaceUpdateRequested)
                            {
                                if (item.ConfigType == OpcMonitoredItemConfigurationType.ExpandedNodeId)
                                {
                                    int namespaceIndex = _namespaceTable.GetIndex(item.ConfigExpandedNodeId?.NamespaceUri);
                                    if (namespaceIndex < 0)
                                    {
                                        Logger.Information($"The namespace URI of node '{item.ConfigExpandedNodeId.ToString()}' can be not mapped to a namespace index.");
                                    }
                                    else
                                    {
                                        item.ConfigExpandedNodeId = new ExpandedNodeId(item.ConfigExpandedNodeId.Identifier, (ushort)namespaceIndex, item.ConfigExpandedNodeId?.NamespaceUri, 0);
                                    }
                                }
                                if (item.ConfigType == OpcMonitoredItemConfigurationType.NodeId)
                                {
                                    string namespaceUri = _namespaceTable.ToArray().ElementAtOrDefault(item.ConfigNodeId.NamespaceIndex);
                                    if (string.IsNullOrEmpty(namespaceUri))
                                    {
                                        Logger.Information($"The namespace index of node '{item.ConfigNodeId.ToString()}' is invalid and the node format can not be updated.");
                                    }
                                    else
                                    {
                                        item.ConfigExpandedNodeId = new ExpandedNodeId(item.ConfigNodeId.Identifier, item.ConfigNodeId.NamespaceIndex, namespaceUri, 0);
                                        item.ConfigType = OpcMonitoredItemConfigurationType.ExpandedNodeId;
                                    }
                                }
                                item.State = OpcMonitoredItemState.Unmonitored;
                            }

                            // lookup namespace index if ExpandedNodeId format has been used and build NodeId identifier.
                            if (item.ConfigType == OpcMonitoredItemConfigurationType.ExpandedNodeId)
                            {
                                int namespaceIndex = _namespaceTable.GetIndex(item.ConfigExpandedNodeId?.NamespaceUri);
                                if (namespaceIndex < 0)
                                {
                                    Logger.Warning($"Syntax or namespace URI of ExpandedNodeId '{item.ConfigExpandedNodeId.ToString()}' is invalid and will be ignored.");
                                    continue;
                                }
                                currentNodeId = new NodeId(item.ConfigExpandedNodeId.Identifier, (ushort)namespaceIndex);
                            }
                            else
                            {
                                currentNodeId = item.ConfigNodeId;
                            }

                            // if configured, get the DisplayName for the node, otherwise use the nodeId
                            Opc.Ua.Node node;
                            if (FetchOpcNodeDisplayName == true)
                            {
                                node = OpcUaClientSession.ReadNode(currentNodeId);
                                item.DisplayName = node.DisplayName.Text ?? currentNodeId.ToString();
                            }
                            else
                            {
                                item.DisplayName = currentNodeId.ToString();
                            }

                            // add the new monitored item.
                            MonitoredItem monitoredItem = new MonitoredItem()
                            {
                                StartNodeId = currentNodeId,
                                AttributeId = item.AttributeId,
                                DisplayName = item.DisplayName,
                                MonitoringMode = item.MonitoringMode,
                                SamplingInterval = item.RequestedSamplingInterval,
                                QueueSize = item.QueueSize,
                                DiscardOldest = item.DiscardOldest
                            };
                            monitoredItem.Notification += item.Notification;
                            opcSubscription.OpcUaClientSubscription.AddItem(monitoredItem);
                            if (additionalMonitoredItemsCount++ % 10000 == 0)
                            {
                                opcSubscription.OpcUaClientSubscription.SetPublishingMode(true);
                                opcSubscription.OpcUaClientSubscription.ApplyChanges();
                            }
                            item.OpcUaClientMonitoredItem = monitoredItem;
                            item.State = OpcMonitoredItemState.Monitored;
                            item.EndpointUrl = EndpointUrl;
                            Logger.Verbose($"Created monitored item for node '{currentNodeId.ToString()}' in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}' on endpoint '{EndpointUrl.AbsoluteUri}' (version: {NodeConfigVersion:X8})");
                            if (item.RequestedSamplingInterval != monitoredItem.SamplingInterval)
                            {
                                Logger.Information($"Sampling interval: requested: {item.RequestedSamplingInterval}; revised: {monitoredItem.SamplingInterval}");
                                item.SamplingInterval = monitoredItem.SamplingInterval;
                            }
                            if (additionalMonitoredItemsCount % 10000 == 0)
                            {
                                    Logger.Information($"Now monitoring {monitoredItemsCount + additionalMonitoredItemsCount} items in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}'");
                            }
                            // request a config file update, if everything is successfully monitored
                            requestConfigFileUpdate = true;
                        }
                        catch (Exception e) when (e.GetType() == typeof(ServiceResultException))
                        {
                            ServiceResultException sre = (ServiceResultException)e;
                            switch ((uint)sre.Result.StatusCode)
                            {
                                case StatusCodes.BadSessionIdInvalid:
                                    {
                                        Logger.Information($"Session with Id {OpcUaClientSession.SessionId} is no longer available on endpoint '{EndpointUrl}'. Cleaning up.");
                                        // clean up the session
                                        InternalDisconnect();
                                        break;
                                    }
                                case StatusCodes.BadNodeIdInvalid:
                                case StatusCodes.BadNodeIdUnknown:
                                    {
                                        Logger.Error($"Failed to monitor node '{currentNodeId.Identifier}' on endpoint '{EndpointUrl}'.");
                                        Logger.Error($"OPC UA ServiceResultException is '{sre.Result}'. Please check your publisher configuration for this node.");
                                        break;
                                    }
                                default:
                                    {
                                        Logger.Error($"Unhandled OPC UA ServiceResultException '{sre.Result}' when monitoring node '{currentNodeId.Identifier}' on endpoint '{EndpointUrl}'. Continue.");
                                        break;
                                    }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"Failed to monitor node '{currentNodeId.Identifier}' on endpoint '{EndpointUrl}'");
                        }
                    }
                    opcSubscription.OpcUaClientSubscription.SetPublishingMode(true);
                    opcSubscription.OpcUaClientSubscription.ApplyChanges();
                    stopWatch.Stop();
                    if (haveUnmonitoredItems == true)
                    {
                        monitoredItemsCount = opcSubscription.OpcMonitoredItems.Count(i => (i.State == OpcMonitoredItemState.Monitored));
                        Logger.Information($"Done processing unmonitored items on endpoint '{EndpointUrl.AbsoluteUri}' took {stopWatch.ElapsedMilliseconds} msec. Now monitoring {monitoredItemsCount} items in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}'.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in MonitorNodes.");
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return requestConfigFileUpdate;
        }

        /// <summary>
        /// Checks if there are monitored nodes tagged to stop monitoring.
        /// </summary>
        public async Task<bool> StopMonitoringNodesAsync(CancellationToken ct)
        {
            bool requestConfigFileUpdate = false;
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();

                // if session is not connected or shutdown is in progress, return
                if (!sessionLocked || ct.IsCancellationRequested || State != SessionState.Connected)
                {
                    return requestConfigFileUpdate;
                }

                foreach (var opcSubscription in OpcSubscriptions)
                {
                    // remove items tagged to stop in the stack
                    var itemsToRemove = opcSubscription.OpcMonitoredItems.Where(i => i.State == OpcMonitoredItemState.RemovalRequested);
                    if (itemsToRemove.Any())
                    {
                        Logger.Information($"Remove nodes in subscription with id {opcSubscription.OpcUaClientSubscription.Id} on endpoint '{EndpointUrl.AbsoluteUri}'");
                        try
                        {
                            opcSubscription.OpcUaClientSubscription.RemoveItems(itemsToRemove.Select(i => i.OpcUaClientMonitoredItem));
                            Logger.Information($"There are now {opcSubscription.OpcUaClientSubscription.MonitoredItemCount} monitored items in this subscription.");
                        }
                        catch
                        {
                            // nodes may be tagged for stop before they are monitored, just continue
                        }
                        // remove them in our data structure
                        opcSubscription.OpcMonitoredItems.RemoveAll(i => i.State == OpcMonitoredItemState.RemovalRequested);
                        Interlocked.Increment(ref NodeConfigVersion);
                        Logger.Information($"There are now {opcSubscription.OpcMonitoredItems.Count} items managed by publisher for this subscription. (version: {NodeConfigVersion:X8})");
                        requestConfigFileUpdate = true;
                    }
                }
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return requestConfigFileUpdate;
        }

        /// <summary>
        /// Checks if there are subscriptions without any monitored items and remove them.
        /// </summary>
        public async Task RemoveUnusedSubscriptionsAsync(CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();

                // if session is not connected or shutdown is in progress, return
                if (!sessionLocked || ct.IsCancellationRequested || State != SessionState.Connected)
                {
                    return;
                }

                // remove the subscriptions in the stack
                var subscriptionsToRemove = OpcSubscriptions.Where(i => i.OpcMonitoredItems.Count == 0);
                if (subscriptionsToRemove.Any())
                {
                    Logger.Information($"Remove unused subscriptions on endpoint '{EndpointUrl}'.");
                    OpcUaClientSession.RemoveSubscriptions(subscriptionsToRemove.Select(s => s.OpcUaClientSubscription));
                    Logger.Information($"There are now {OpcUaClientSession.SubscriptionCount} subscriptions in this session.");
                }
                // remove them in our data structures
                OpcSubscriptions.RemoveAll(s => s.OpcMonitoredItems.Count == 0);
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }

        }

        /// <summary>
        /// Checks if there are session without any subscriptions and remove them.
        /// </summary>
        public async Task RemoveUnusedSessionsAsync(CancellationToken ct)
        {
            try
            {
                await OpcSessionsListSemaphore.WaitAsync();

                // if session is not connected or shutdown is in progress, return
                if (ct.IsCancellationRequested || State != SessionState.Connected)
                {
                    return;
                }

                // remove sessions in the stack
                var sessionsToRemove = OpcSessions.Where(s => s.OpcSubscriptions.Count == 0);
                foreach (var sessionToRemove in sessionsToRemove)
                {
                    Logger.Information($"Remove unused session on endpoint '{EndpointUrl}'.");
                    await sessionToRemove.ShutdownAsync();
                }
                // remove then in our data structures
                OpcSessions.RemoveAll(s => s.OpcSubscriptions.Count == 0);
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
        }

        /// <summary>
        /// Disconnects a session and removes all subscriptions on it and marks all nodes on those subscriptions
        /// as unmonitored.
        /// </summary>
        public async Task DisconnectAsync()
        {
            bool sessionLocked = await LockSessionAsync();
            if (sessionLocked)
            {
                try
                {
                    InternalDisconnect();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Error while disconnecting '{EndpointUrl}'.");
                }
                ReleaseSession();
            }
        }

        /// <summary>
        /// Returns the namespace index for a namespace URI.
        /// </summary>
        public int GetNamespaceIndexUnlocked(string namespaceUri)
        {
            return _namespaceTable.GetIndex(namespaceUri);
        }

        /// <summary>
        /// Internal disconnect method. Caller must have taken the _opcSessionSemaphore.
        /// </summary>
        private void InternalDisconnect()
        {
            try
            {
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    try
                    {
                        OpcUaClientSession.RemoveSubscription(opcSubscription.OpcUaClientSubscription);
                    }
                    catch
                    {
                        // the session might be already invalidated. ignore.
                    }
                    try
                    {
                        opcSubscription.OpcUaClientSubscription.Delete(true);
                    }
                    catch
                    {
                        // the subscription might be already invalidated. ignore.
                    }
                    opcSubscription.OpcUaClientSubscription = null;

                    // mark all monitored items as unmonitored
                    foreach (var opcMonitoredItem in opcSubscription.OpcMonitoredItems)
                    {
                        // tag all monitored items as unmonitored
                        if (opcMonitoredItem.State == OpcMonitoredItemState.Monitored)
                        {
                            opcMonitoredItem.State = OpcMonitoredItemState.Unmonitored;
                        }
                    }
                }
                try
                {
                    OpcUaClientSession.Close();
                }
                catch
                {
                    // the session might be already invalidated. ignore.
                }
                OpcUaClientSession = null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in InternalDisconnect.");
            }
            State = SessionState.Disconnected;
            MissedKeepAlives = 0;
        }

        /// <summary>
        /// Adds a node to be monitored. If there is no subscription with the requested publishing interval,
        /// one is created.
        /// </summary>
        public async Task<HttpStatusCode> AddNodeForMonitoringAsync(NodeId nodeId, ExpandedNodeId expandedNodeId, int opcPublishingInterval, int opcSamplingInterval, CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();
                if (!sessionLocked || ct.IsCancellationRequested)
                {
                    return HttpStatusCode.Gone;
                }

                // check if there is already a subscription with the same publishing interval, which can be used to monitor the node
                OpcSubscription opcSubscription = OpcSubscriptions.FirstOrDefault(s => s.RequestedPublishingInterval == opcPublishingInterval);
                
                // if there was none found, create one
                if (opcSubscription == null)
                {
                    opcSubscription = new OpcSubscription(opcPublishingInterval);
                    OpcSubscriptions.Add(opcSubscription);
                    Logger.Information($"AddNodeForMonitoring: No matching subscription with publishing interval of {opcPublishingInterval} found'. Requested to create a new one.");
                }

                // create objects for publish check
                ExpandedNodeId expandedNodeIdCheck = expandedNodeId;
                NodeId nodeIdCheck = nodeId;
                if (State == SessionState.Connected)
                {
                    if (expandedNodeId == null)
                    {
                        string namespaceUri = _namespaceTable.ToArray().ElementAtOrDefault(nodeId.NamespaceIndex);
                        expandedNodeIdCheck = new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex, namespaceUri, 0);
                    }
                    if (nodeId == null)
                    {
                        nodeIdCheck = new NodeId(expandedNodeId.Identifier, (ushort)(_namespaceTable.GetIndex(expandedNodeId.NamespaceUri)));
                    }
                }

                // if it is already published, we do nothing, else we create a new monitored item
                if (!IsNodePublishedInSessionInternal(nodeIdCheck, expandedNodeIdCheck))
                {
                    OpcMonitoredItem opcMonitoredItem = null;
                    // add a new item to monitor
                    if (expandedNodeId == null)
                    {
                        opcMonitoredItem = new OpcMonitoredItem(nodeId, EndpointUrl);
                    }
                    else
                    {
                        opcMonitoredItem = new OpcMonitoredItem(expandedNodeId, EndpointUrl);
                    }
                    opcMonitoredItem.RequestedSamplingInterval = opcSamplingInterval;
                    opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                    Interlocked.Increment(ref NodeConfigVersion);
                    Logger.Debug($"AddNodeForMonitoring: Added item with nodeId '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' for monitoring.");

                    // trigger the actual OPC communication with the server to be done
                    Task t = Task.Run(async () => await ConnectAndMonitorAsync(ct));
                    return HttpStatusCode.Accepted;
                }
                else
                {
                    Logger.Debug($"AddNodeForMonitoring: Node with Id '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' is already monitored.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"AddNodeForMonitoring: Exception while trying to add node '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' for monitoring.");
                return HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return HttpStatusCode.OK;
        }

        /// <summary>
        /// Tags a monitored node to stop monitoring and remove it.
        /// </summary>
        public async Task<HttpStatusCode> RequestMonitorItemRemovalAsync(NodeId nodeId, ExpandedNodeId expandedNodeId, CancellationToken ct)
        {
            HttpStatusCode result = HttpStatusCode.Gone;
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();

                if (!sessionLocked || ct.IsCancellationRequested)
                {
                    return HttpStatusCode.Gone;
                }

                // create objects for publish check
                ExpandedNodeId expandedNodeIdCheck = expandedNodeId;
                NodeId nodeIdCheck = nodeId;
                if (State == SessionState.Connected)
                {
                    if (expandedNodeId == null)
                    {
                        string namespaceUri = _namespaceTable.ToArray().ElementAtOrDefault(nodeId.NamespaceIndex);
                        expandedNodeIdCheck = new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex, namespaceUri, 0);
                    }
                    if (nodeId == null)
                    {
                        nodeIdCheck = new NodeId(expandedNodeId.Identifier, (ushort)(_namespaceTable.GetIndex(expandedNodeId.NamespaceUri)));
                    }

                }

                // if node is not published return succuss
                if (!IsNodePublishedInSessionInternal(nodeIdCheck, expandedNodeIdCheck))
                {
                    Logger.Information($"RequestMonitorItemRemoval: Node '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' is not monitored.");
                    return HttpStatusCode.OK;
                }

                // tag all monitored items with nodeId to stop monitoring.
                // if the node to tag is specified as NodeId, it will also tag nodes configured in ExpandedNodeId format.
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    var opcMonitoredItems = opcSubscription.OpcMonitoredItems.Where(m => { return m.IsMonitoringThisNode(nodeIdCheck, expandedNodeIdCheck, _namespaceTable); });
                    foreach (var opcMonitoredItem in opcMonitoredItems)
                    {
                        // tag it for removal.
                        opcMonitoredItem.State = OpcMonitoredItemState.RemovalRequested;
                        Logger.Information($"RequestMonitorItemRemoval: Node with id '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' tagged to stop monitoring.");
                        result = HttpStatusCode.Accepted;
                    }
                }

                // trigger the actual OPC communication with the server to be done
                Task t = Task.Run(async () => await ConnectAndMonitorAsync(ct));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"RequestMonitorItemRemoval: Exception while trying to tag node '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' to stop monitoring.");
                result = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if the node specified by either the given NodeId or ExpandedNodeId on the given endpoint is published in the session. Caller to take session semaphore.
        /// </summary>
        private bool IsNodePublishedInSessionInternal(NodeId nodeId, ExpandedNodeId expandedNodeId)
        {
            try
            {
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    if (opcSubscription.OpcMonitoredItems.Any(m => { return m.IsMonitoringThisNode(nodeId, expandedNodeId, _namespaceTable); }))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Check if node is published failed.");
            }
            return false;
        }

        /// <summary>
        /// Checks if the node specified by either the given NodeId or ExpandedNodeId on the given endpoint is published in the session.
        /// </summary>
        private bool IsNodePublishedInSession(NodeId nodeId, ExpandedNodeId expandedNodeId)
        {
            bool result = false;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;

                if (sessionLocked && !_sessionCancelationToken.IsCancellationRequested)
                {
                    result = IsNodePublishedInSessionInternal(nodeId, expandedNodeId);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Check if node is published failed.");
            }
            finally
            {
                if (sessionLocked)
                {
                    ReleaseSession();
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if the node specified by either the given NodeId or ExpandedNodeId on the given endpoint is published.
        /// </summary>
        public static bool IsNodePublished(NodeId nodeId, ExpandedNodeId expandedNodeId, Uri endpointUrl)
        {
            try
            {
                OpcSessionsListSemaphore.Wait();

                // itereate through all sessions, subscriptions and monitored items and create config file entries
                foreach (var opcSession in OpcSessions)
                {
                    if (opcSession.EndpointUrl.AbsoluteUri.Equals(endpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                    {
                        if (opcSession.IsNodePublishedInSession(nodeId, expandedNodeId))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Check if node is published failed.");
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return false;
        }

    /// <summary>
    /// Shutdown the current session if it is connected.
    /// </summary>
    public async Task ShutdownAsync()
        {
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync();

                // if the session is connected, close it.
                if (sessionLocked && (State == SessionState.Connecting || State == SessionState.Connected))
                {
                    try
                    {
                        foreach (var opcSubscription in OpcSubscriptions)
                        {
                            Logger.Information($"Removing {opcSubscription.OpcUaClientSubscription.MonitoredItemCount} monitored items from subscription with id '{opcSubscription.OpcUaClientSubscription.Id}'.");
                            opcSubscription.OpcUaClientSubscription.RemoveItems(opcSubscription.OpcUaClientSubscription.MonitoredItems);
                        }
                        Logger.Information($"Removing {OpcUaClientSession.SubscriptionCount} subscriptions from session.");
                        while (OpcSubscriptions.Count > 0)
                        {
                            OpcSubscription opcSubscription = OpcSubscriptions.ElementAt(0);
                            OpcSubscriptions.RemoveAt(0);
                            Subscription opcUaClientSubscription = opcSubscription.OpcUaClientSubscription;
                            opcUaClientSubscription.Delete(true);
                        }
                        Logger.Information($"Closing session to endpoint URI '{EndpointUrl.AbsoluteUri}' closed successfully.");
                        OpcUaClientSession.Close();
                        State = SessionState.Disconnected;
                        Logger.Information($"Session to endpoint URI '{EndpointUrl.AbsoluteUri}' closed successfully.");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while closing session to endpoint '{EndpointUrl.AbsoluteUri}'.");
                        State = SessionState.Disconnected;
                        return;
                    }
                }
            }
            finally
            {
                if (sessionLocked)
                {
                    // cancel all threads waiting on the session semaphore
                    _sessionCancelationTokenSource.Cancel();
                    _opcSessionSemaphore.Dispose();
                    _opcSessionSemaphore = null;
                }
            }
        }

        /// <summary>
        /// Create a subscription in the session.
        /// </summary>
        private Subscription CreateSubscription(int requestedPublishingInterval, out int revisedPublishingInterval)
        {
            Subscription subscription = new Subscription()
            {
                PublishingInterval = requestedPublishingInterval,
            };
            // need to happen before the create to set the Session property.
            OpcUaClientSession.AddSubscription(subscription);
            subscription.Create();
            Logger.Information($"Created subscription with id {subscription.Id} on endpoint '{EndpointUrl.AbsoluteUri}'");
            if (requestedPublishingInterval != subscription.PublishingInterval)
            {
                Logger.Information($"Publishing interval: requested: {requestedPublishingInterval}; revised: {subscription.PublishingInterval}");
            }
            revisedPublishingInterval = subscription.PublishingInterval;
            return subscription;
        }

        /// <summary>
        /// Handler for the standard "keep alive" event sent by all OPC UA servers
        /// </summary>
        private void StandardClient_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            // Ignore if we are shutting down.
            if (ShutdownTokenSource.IsCancellationRequested == true)
            {
                return;
            }

            if (e != null && session != null && session.ConfiguredEndpoint != null && OpcUaClientSession != null)
            {
                try
                {
                    if (!ServiceResult.IsGood(e.Status))
                    {
                        Logger.Warning($"Session endpoint: {session.ConfiguredEndpoint.EndpointUrl} has Status: {e.Status}");
                        Logger.Information($"Outstanding requests: {session.OutstandingRequestCount}, Defunct requests: {session.DefunctRequestCount}");
                        Logger.Information($"Good publish requests: {session.GoodPublishRequestCount}, KeepAlive interval: {session.KeepAliveInterval}");
                        Logger.Information($"SessionId: {session.SessionId}");

                        if (State == SessionState.Connected)
                        {
                            MissedKeepAlives++;
                            Logger.Information($"Missed KeepAlives: {MissedKeepAlives}");
                            if (MissedKeepAlives >= OpcKeepAliveDisconnectThreshold)
                            {
                                Logger.Warning($"Hit configured missed keep alive threshold of {OpcKeepAliveDisconnectThreshold}. Disconnecting the session to endpoint {session.ConfiguredEndpoint.EndpointUrl}.");
                                session.KeepAlive -= StandardClient_KeepAlive;
                                Task t = Task.Run(async () => await DisconnectAsync());
                            }
                        }
                    }
                    else
                    {
                        if (MissedKeepAlives != 0)
                        {
                            // Reset missed keep alive count
                            Logger.Information($"Session endpoint: {session.ConfiguredEndpoint.EndpointUrl} got a keep alive after {MissedKeepAlives} {(MissedKeepAlives == 1 ? "was" : "were")} missed.");
                            MissedKeepAlives = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error in keep alive handling for endpoint '{session.ConfiguredEndpoint.EndpointUrl}'. (message: '{ex.Message}'");
                }
            }
            else
            {
                Logger.Warning("Keep alive arguments seems to be wrong.");
            }
        }

        /// <summary>
        /// Take the session semaphore.
        /// </summary>
        public async Task<bool> LockSessionAsync()
        {
            await _opcSessionSemaphore.WaitAsync(_sessionCancelationToken);
            if (_sessionCancelationToken.IsCancellationRequested)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Release the session semaphore.
        /// </summary>
        public void ReleaseSession()
        {
            _opcSessionSemaphore.Release();
        }

        private SemaphoreSlim _opcSessionSemaphore;
        private CancellationTokenSource _sessionCancelationTokenSource;
        private CancellationToken _sessionCancelationToken;
        private NamespaceTable _namespaceTable;
        private EndpointTelemetryConfiguration _telemetryConfiguration;
        private double _minSupportedSamplingInterval;
    }
}
