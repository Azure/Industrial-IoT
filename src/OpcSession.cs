
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Publisher
{
    using Opc.Ua;
    using System.Threading;
    using System.Threading.Tasks;
    using static Opc.Ua.Workarounds.TraceWorkaround;
    using static Program;

    public class MonitoredItemInfo
    {
        public enum MonitoredItemState
        {
            Unmonitored = 0,
            Monitoreded,
            StopMonitoring,
        }

        public ExpandedNodeId StartNodeId { get; set; }
        public string DisplayName { get; set; }
        public MonitoredItemState State { get; set; }
        public uint AttributeId { get; set; }
        public MonitoringMode MonitoringMode { get; set; }
        public int SamplingInterval { get; set; }
        public uint QueueSize { get; set; }
        public bool DiscardOldest { get; set; }
        public MonitoredItemNotificationEventHandler Notification { get; set; }
        public Uri EndpointUri { get; set; }
        public MonitoredItem MonitoredItem;

        public MonitoredItemInfo(NodeId nodeId, Uri sessionEndpointUri)
        {
            StartNodeId = new ExpandedNodeId(nodeId);
            Initialize(sessionEndpointUri);
        }

        public MonitoredItemInfo(ExpandedNodeId expandedNodeId, Uri sessionEndpointUri)
        {
            StartNodeId = expandedNodeId;
            Initialize(sessionEndpointUri);
        }

        private void Initialize(Uri sessionEndpointUri)
        {
            State = MonitoredItemState.Unmonitored;
            DisplayName = string.Empty;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            SamplingInterval = OpcSamplingRateMillisec;
            QueueSize = 0;
            DiscardOldest = true;
            Notification = new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
            EndpointUri = sessionEndpointUri;
        }

        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server
        /// </summary>
        public static void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
        {
            try
            {
                if (args.NotificationValue == null || monitoredItem.Subscription.Session == null)
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

                JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                
                string applicationURI = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri;
                encoder.WriteString("ApplicationUri", (applicationURI + (string.IsNullOrEmpty(ShopfloorDomain) ? "" : $":{ShopfloorDomain}")));
                encoder.WriteString("DisplayName", monitoredItem.DisplayName);

                // write NodeId as ns=x;i=y
                NodeId nodeId = monitoredItem.ResolvedNodeId;
                encoder.WriteString("NodeId", new NodeId(nodeId.Identifier, nodeId.NamespaceIndex).ToString());

                // suppress output of server timestamp in json by setting it to minvalue
                value.ServerTimestamp = DateTime.MinValue;
                encoder.WriteDataValue("Value", value);

                string json = encoder.CloseAndReturnText();

                // add message to fifo send queue
                Trace(Utils.TraceMasks.OperationDetail, "Enqueue a new message:");
                Trace(Utils.TraceMasks.OperationDetail,  "   ApplicationUri: " + (applicationURI + (string.IsNullOrEmpty(ShopfloorDomain) ? "" : $":{ShopfloorDomain}")));
                Trace(Utils.TraceMasks.OperationDetail, $"   DisplayName: {monitoredItem.DisplayName}");
                Trace(Utils.TraceMasks.OperationDetail, $"   Value: {value}");
                Program.IotHubMessaging.Enqueue(json);

            }
            catch (Exception e)
            {
                Trace(e, "Error processing monitored item notification");
            }
        }
    }

    public class OpcSession
    {
        public enum SessionState
        {
            Disconnected = 0,
            Connected,
        }

        public Uri EndpointUri { get; set; }
        public Session Session { get; set; }
        public SessionState State { get; set; }
        public List<MonitoredItemInfo> MonitoredItemsInfo;
        public uint SessionTimeout { get; }
        public uint UnsuccessfulConnectionCount { get; set; }
        public uint MissedKeepAlives { get; set; }
        private SemaphoreSlim _opcSessionSemaphore;
        private NamespaceTable _namespaceTable;

        public OpcSession(Uri endpointUri, uint sessionTimeout)
        {
            State = SessionState.Disconnected;
            EndpointUri = endpointUri;
            SessionTimeout = sessionTimeout * 1000;
            MonitoredItemsInfo = new List<MonitoredItemInfo>();
            UnsuccessfulConnectionCount = 0;
            MissedKeepAlives = 0;
            _opcSessionSemaphore = new SemaphoreSlim(1);
            _namespaceTable = new NamespaceTable();
        }

        public async Task ConnectAndMonitor()
        {
            _opcSessionSemaphore.Wait();
            Trace($"Connect and monitor session and nodes on endpoint '{EndpointUri.AbsoluteUri}'.");
            try
            {
                // if the session is disconnected, create it.
                if (State == SessionState.Disconnected)
                {
                    EndpointDescription selectedEndpoint = CoreClientUtils.SelectEndpoint(EndpointUri.AbsoluteUri, true);
                    ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server, EndpointConfiguration.Create(OpcConfiguration));
                    configuredEndpoint.Update(selectedEndpoint);

                    try
                    {
                        uint timeout = SessionTimeout * ((UnsuccessfulConnectionCount >= OpcSessionCreationBackoffMax) ? OpcSessionCreationBackoffMax : UnsuccessfulConnectionCount + 1);
                        Trace($"Create session for endpoint URI '{EndpointUri.AbsoluteUri}' with timeout of {timeout} ms.");
                        Session = await Session.Create(
                                OpcConfiguration,
                                configuredEndpoint,
                                true,
                                false,
                                OpcConfiguration.ApplicationName,
                                timeout,
                                new UserIdentity(new AnonymousIdentityToken()),
                                null);

                        if (Session != null)
                        {
                            Trace($"Session successfully created with Id {Session.SessionId}.");
                            if (!selectedEndpoint.EndpointUrl.Equals(configuredEndpoint.EndpointUrl))
                            {
                                Trace($"the Server has updated the EndpointUrl to '{selectedEndpoint.EndpointUrl}'");
                            }

                            // init object state and install keep alive
                            UnsuccessfulConnectionCount = 0;
                            State = SessionState.Connected;
                            Session.KeepAliveInterval = OpcKeepAliveIntervalInSec * 1000;
                            Session.KeepAlive += new KeepAliveEventHandler((sender, e) => StandardClient_KeepAlive(sender, e, Session));

                            // fetch the namespace array and cache it. it will not change as long the session exists.
                            NodeId namespaceArrayNodeId = new NodeId(Variables.Server_NamespaceArray, 0);
                            DataValue namespaceArrayNodeValue = Session.ReadValue(namespaceArrayNodeId);
                            _namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                            // show the available namespaces
                            Trace($"The session to endpoint '{selectedEndpoint.EndpointUrl}' has {_namespaceTable.Count} entries in its namespace array:");
                            int i = 0;
                            foreach (var ns in _namespaceTable.ToArray())
                            {
                                Trace($"Namespace index {i++}: {ns}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace(e, $"Session creation to endpoint '{EndpointUri.AbsoluteUri}' failed {++UnsuccessfulConnectionCount} time(s). Please verify if server is up and Publisher configuration is correct.");
                        State = SessionState.Disconnected;
                        Session = null;
                        return;
                    }
                }

                // stop monitoring of nodes if requested and remove them from the monitored items list.
                var itemsToRemove = MonitoredItemsInfo.Where(i => i.State == MonitoredItemInfo.MonitoredItemState.StopMonitoring);
                if (itemsToRemove.GetEnumerator().MoveNext())
                {
                    itemsToRemove.GetEnumerator().Reset();
                    Trace($"Remove nodes on endpoint '{EndpointUri.AbsoluteUri}'");
                    Session.DefaultSubscription.RemoveItems(itemsToRemove.Select(i => i.MonitoredItem));
                }

                // ensure all nodes on this session are monitored.
                Trace($"Start monitoring nodes on endpoint '{EndpointUri.AbsoluteUri}'");
                var unmonitoredItems = MonitoredItemsInfo.Where(i => i.State == MonitoredItemInfo.MonitoredItemState.Unmonitored);
                foreach (var item in unmonitoredItems)
                {
                    // if the session is disconnected, we stop trying and wait for the next cycle
                    if (State == SessionState.Disconnected)
                    {
                        break;
                    }

                    NodeId currentNodeId;
                    try
                    {
                        Subscription subscription = Session.DefaultSubscription;
                        if (Session.AddSubscription(subscription))
                        {
                            Trace("Create default subscription.");
                            subscription.Create();
                        }

                        // lookup namespace index if ExpandedNodeId format has been used and build NodeId identifier.
                        if (!string.IsNullOrEmpty(item.StartNodeId.NamespaceUri))
                        {
                            currentNodeId = NodeId.Create(item.StartNodeId.Identifier, item.StartNodeId.NamespaceUri, _namespaceTable);
                        }
                        else
                        {
                            currentNodeId = new NodeId((NodeId)item.StartNodeId);
                        }

                        // get the DisplayName for the node, otherwise use the nodeId
                        Node node = Session.ReadNode(currentNodeId);
                        item.DisplayName = node.DisplayName.Text ?? currentNodeId.ToString();

                        // add the new monitored item.
                        MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem)
                        {
                            StartNodeId = currentNodeId,
                            AttributeId = item.AttributeId,
                            DisplayName = node.DisplayName.Text,
                            MonitoringMode = item.MonitoringMode,
                            SamplingInterval = item.SamplingInterval,
                            QueueSize = item.QueueSize,
                            DiscardOldest = item.DiscardOldest
                        };
                        monitoredItem.Notification += item.Notification;
                        subscription.AddItem(monitoredItem);
                        subscription.ApplyChanges();
                        item.MonitoredItem = monitoredItem;
                        item.State = MonitoredItemInfo.MonitoredItemState.Monitoreded;
                        item.EndpointUri = EndpointUri;
                        Trace($"Created monitored item for node '{currentNodeId}' on endpoint '{EndpointUri.AbsoluteUri}'");
                    }
                    catch (Exception e) when (e.GetType() == typeof(ServiceResultException))
                    {
                        ServiceResultException sre = (ServiceResultException)e;
                        switch ((uint)sre.Result.StatusCode)
                        {
                            case StatusCodes.BadSessionIdInvalid:
                                {
                                    Trace($"Session with Id {Session.SessionId} is no longer available on endpoint '{EndpointUri}'. Cleaning up.");
                                    // clean up the session
                                    _opcSessionSemaphore.Release();
                                    Disconnect();
                                    break;
                                }
                            case StatusCodes.BadNodeIdInvalid:
                            case StatusCodes.BadNodeIdUnknown:
                                {
                                    Trace($"Failed to monitor node '{item.StartNodeId.Identifier}' on endpoint '{EndpointUri}'.");
                                    Trace($"OPC UA ServiceResultException is '{sre.Result}'. Please check your publisher configuration for this node.");
                                    break;
                                }
                            default:
                                {
                                    Trace($"Unhandled OPC UA ServiceResultException '{sre.Result}' when monitoring node '{item.StartNodeId.Identifier}' on endpoint '{EndpointUri}'. Continue.");
                                    break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                            Trace(e, $"Failed to monitor node '{item.StartNodeId.Identifier}' on endpoint '{EndpointUri}'");
                    }
                }

                // shutdown unused sessions.
                var unusedSessions = OpcSessions.Where(s => s.MonitoredItemsInfo.Count == 0);
                foreach (var unusedSession in unusedSessions)
                {
                    await unusedSession.Shutdown();
                    OpcSessions.Remove(unusedSession);
                }
            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        public async Task Disconnect()
        {
            _opcSessionSemaphore.Wait();
            try
            {
                Session.RemoveSubscriptions(Session.Subscriptions);
                Session.Close();
            }
            catch (Exception e)
            {
                // in case of a bad session disconnect this is expected
            }
            State = SessionState.Disconnected;

            // mark all monitored items as unmonitored
            foreach (var monitoredItemInfo in MonitoredItemsInfo)
            {
                monitoredItemInfo.State = MonitoredItemInfo.MonitoredItemState.Unmonitored;
            }
            MissedKeepAlives = 0;
            _opcSessionSemaphore.Release();
        }

        public void AddNodeForMonitoring(NodeId nodeId)
        {
            _opcSessionSemaphore.Wait();
            try
            {
                MonitoredItemInfo monitoredItemInfo = MonitoredItemsInfo.First(m => m.StartNodeId == nodeId);

                // if it is already there, we just ignore it, otherwise we add a new item to monitor.
                if (monitoredItemInfo == null)
                {
                    // add a new item to monitor
                    monitoredItemInfo = new MonitoredItemInfo(nodeId, EndpointUri);
                    MonitoredItemsInfo.Add(monitoredItemInfo);
                }

            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        public void TagNodeForMonitoringStop(NodeId nodeId)
        {
            _opcSessionSemaphore.Wait();
            try
            {
                MonitoredItemInfo monitoredItemInfo = MonitoredItemsInfo.First(m => m.StartNodeId == nodeId);

                // if it is not there, we just ignore it, otherwise we tag it for removal.
                if (monitoredItemInfo == null)
                {
                    // tag it for removal.
                    monitoredItemInfo.State = MonitoredItemInfo.MonitoredItemState.StopMonitoring;
                }

            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        public async Task Shutdown()
        {
            _opcSessionSemaphore.Wait();
            try
            {
                // if the session is connected, close it.
                if (State == SessionState.Connected)
                {
                    try
                    {
                        Trace($"Removing {Session.DefaultSubscription.MonitoredItemCount} monitored items from default subscription from session to endpoint URI '{EndpointUri.AbsoluteUri}'.");
                        Session.DefaultSubscription.RemoveItems(Session.DefaultSubscription.MonitoredItems);
                        Trace($"Closing session to endpoint URI '{EndpointUri.AbsoluteUri}' closed successfully.");
                        Session.Close();
                        State = SessionState.Disconnected;
                        Trace($"Session to endpoint URI '{EndpointUri.AbsoluteUri}' closed successfully.");
                    }
                    catch (Exception e)
                    {
                        Trace(e, $"Error while closing session to endpoint '{EndpointUri.AbsoluteUri}'.");
                        State = SessionState.Disconnected;
                        return;
                    }
                }
            }
            finally
            {
                _opcSessionSemaphore.Release();
                if (OpcSessions.Count(s => s.State == SessionState.Connected) == 0)
                {
                    _opcSessionSemaphore.Dispose();
                }
            }
        }

        /// <summary>
        /// Handler for the standard "keep alive" event sent by all OPC UA servers
        /// </summary>
        private static void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e, Session session)
        {
            if (e != null && session != null)
            {
                var opcSession = Program.OpcSessions.Where(s => s.Session.ConfiguredEndpoint.EndpointUrl.Equals(sender.ConfiguredEndpoint.EndpointUrl)).First();
                if (!ServiceResult.IsGood(e.Status))
                {
                    Trace($"Session endpoint: {sender.ConfiguredEndpoint.EndpointUrl} has Status: {e.Status}");
                    Trace($"Outstanding requests: {session.OutstandingRequestCount}, Defunct requests: {session.DefunctRequestCount}");
                    if (opcSession != null)
                    {
                        opcSession.MissedKeepAlives++;
                        Trace($"Now missed {opcSession.MissedKeepAlives} keep alive.");
                        if (opcSession.MissedKeepAlives >= OpcKeepAliveDisconnectThreshold)
                        {
                            Trace($"Hit configured missed keep alive threshold of {Program.OpcKeepAliveDisconnectThreshold}. Disconnecting the session to endpoint {sender.ConfiguredEndpoint.EndpointUrl}.");
                            session.KeepAlive -= new KeepAliveEventHandler((a, b) => StandardClient_KeepAlive(sender, e, session));
                            opcSession.Disconnect();
                        }
                    }
                }
                else
                {
                    if (opcSession.MissedKeepAlives != 0)
                    {
                        // Reset missed keep alive count
                        Trace($"Session endpoint: {sender.ConfiguredEndpoint.EndpointUrl} got a keep alive after {opcSession.MissedKeepAlives} {(opcSession.MissedKeepAlives == 1 ? "was" : "were")} missed.");
                        opcSession.MissedKeepAlives = 0;
                    }
                }
            }
            else
            {
                Trace("Keep alive arguments seems to be wrong.");
            }
        }
    }
}
