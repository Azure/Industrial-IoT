
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

    /// <summary>
    /// Class to manage the OPC monitored items, which are the nodes we need to publish.
    /// </summary>
    public class OpcMonitoredItem
    {
        public enum OpcMonitoredItemState
        {
            Unmonitored = 0,
            Monitoreded,
            StopMonitoring,
        }

        public ExpandedNodeId StartNodeId;
        public string DisplayName;
        public OpcMonitoredItemState State;
        public uint AttributeId;
        public MonitoringMode MonitoringMode;
        public int RequestedSamplingInterval;
        public int SamplingInterval;
        public uint QueueSize;
        public bool DiscardOldest;
        public MonitoredItemNotificationEventHandler Notification;
        public Uri EndpointUri;
        public MonitoredItem MonitoredItem;
        public string ConfigNodeId;

        /// <summary>
        /// Ctor using NodeId (ns syntax for namespace).
        /// </summary>
        public OpcMonitoredItem(NodeId nodeId, Uri sessionEndpointUri)
        {
            ConfigNodeId = nodeId.ToString();
            StartNodeId = new ExpandedNodeId(nodeId);
            Initialize(sessionEndpointUri);
        }

        /// <summary>
        /// Ctor using ExpandedNodeId (nsu syntax for namespace).
        /// </summary>
        public OpcMonitoredItem(ExpandedNodeId expandedNodeId, Uri sessionEndpointUri)
        {
            ConfigNodeId = expandedNodeId.ToString();
            StartNodeId = expandedNodeId;
            Initialize(sessionEndpointUri);
        }

        /// <summary>
        /// Init class variables.
        /// </summary>
        private void Initialize(Uri sessionEndpointUri)
        {
            State = OpcMonitoredItemState.Unmonitored;
            DisplayName = string.Empty;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            RequestedSamplingInterval = OpcSamplingInterval;
            QueueSize = 0;
            DiscardOldest = true;
            Notification = new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
            EndpointUri = sessionEndpointUri;
        }

        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server.
        /// </summary>
        public void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
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

                // use the node Id as configured, to also have the namespace URI in case of a ExpandedNodeId.
                encoder.WriteString("NodeId", ConfigNodeId);

                // suppress output of server timestamp in json by setting it to minvalue
                value.ServerTimestamp = DateTime.MinValue;
                encoder.WriteDataValue("Value", value);

                string json = encoder.CloseAndReturnText();

                // add message to fifo send queue
                Trace(Utils.TraceMasks.OperationDetail, $"Enqueue a new message from subscription {monitoredItem.Subscription.Id} (publishing interval: {monitoredItem.Subscription.PublishingInterval}, sampling interval: {monitoredItem.SamplingInterval}):");
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

    /// <summary>
    /// Class to manage OPC subscriptions. We create a subscription for each different publishing interval
    /// on an Endpoint.
    /// </summary>
    public class OpcSubscription
    {
        public List<OpcMonitoredItem> OpcMonitoredItems;
        public int RequestedPublishingInterval;
        public double PublishingInterval;
        public Subscription Subscription;

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
            Connected,
        }

        public Uri EndpointUri;
        public Session Session;
        public SessionState State;
        public List<OpcSubscription> OpcSubscriptions;
        public uint SessionTimeout { get; }
        public uint UnsuccessfulConnectionCount;
        public uint MissedKeepAlives;
        public int PublishingInterval;
        private SemaphoreSlim _opcSessionSemaphore;
        private NamespaceTable _namespaceTable;
        private double _minSupportedSamplingInterval;

        /// <summary>
        /// Ctor for the session.
        /// </summary>
        /// <param name="endpointUri"></param>
        /// <param name="sessionTimeout"></param>
        public OpcSession(Uri endpointUri, uint sessionTimeout)
        {
            State = SessionState.Disconnected;
            EndpointUri = endpointUri;
            SessionTimeout = sessionTimeout * 1000;
            OpcSubscriptions = new List<OpcSubscription>();
            UnsuccessfulConnectionCount = 0;
            MissedKeepAlives = 0;
            PublishingInterval = OpcPublishingInterval;
            _opcSessionSemaphore = new SemaphoreSlim(1);
            _namespaceTable = new NamespaceTable();
        }

        /// <summary>
        /// This task is executed regularily and ensures:
        /// - disconnected sessions are reconnected.
        /// - monitored nodes are no longer monitored if requested to do so.
        /// - monitoring for a node starts if it is required.
        /// - unused subscriptions (without any nodes to monitor) are removed.
        /// - sessions with out subscriptions are removed.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAndMonitor()
        {
            _opcSessionSemaphore.Wait();
            try
            {
                // if the session is disconnected, create one.
                if (State == SessionState.Disconnected)
                {
                    Trace($"Connect and monitor session and nodes on endpoint '{EndpointUri.AbsoluteUri}'.");
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
                            if (!selectedEndpoint.EndpointUrl.Equals(configuredEndpoint.EndpointUrl.AbsoluteUri))
                            {
                                Trace($"the Server has updated the EndpointUrl to '{selectedEndpoint.EndpointUrl}'");
                            }

                            // init object state and install keep alive
                            UnsuccessfulConnectionCount = 0;
                            State = SessionState.Connected;
                            Session.KeepAliveInterval = OpcKeepAliveIntervalInSec * 1000;
                            Session.KeepAlive += StandardClient_KeepAlive;

                            // fetch the namespace array and cache it. it will not change as long the session exists.
                            DataValue namespaceArrayNodeValue = Session.ReadValue(VariableIds.Server_NamespaceArray);
                            _namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                            // show the available namespaces
                            Trace($"The session to endpoint '{selectedEndpoint.EndpointUrl}' has {_namespaceTable.Count} entries in its namespace array:");
                            int i = 0;
                            foreach (var ns in _namespaceTable.ToArray())
                            {
                                Trace($"Namespace index {i++}: {ns}");
                            }

                            // fetch the minimum supported item sampling interval from the server.
                            DataValue minSupportedSamplingInterval = Session.ReadValue(VariableIds.Server_ServerCapabilities_MinSupportedSampleRate);
                            _minSupportedSamplingInterval = minSupportedSamplingInterval.GetValue(0);
                            Trace($"The server on endpoint '{selectedEndpoint.EndpointUrl}' supports a minimal sampling interval of {_minSupportedSamplingInterval} ms.");
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
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    var itemsToRemove = opcSubscription.OpcMonitoredItems.Where(i => i.State == OpcMonitoredItem.OpcMonitoredItemState.StopMonitoring);
                    if (itemsToRemove.Any())
                    {
                        Trace($"Remove nodes in subscription with id {opcSubscription.Subscription.Id} on endpoint '{EndpointUri.AbsoluteUri}'");
                        opcSubscription.Subscription.RemoveItems(itemsToRemove.Select( i => i.MonitoredItem ));
                    }
                }

                // ensure all nodes in all subscriptions of this session are monitored.
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    // create the subscription, if it is not yet there.
                    if (opcSubscription.Subscription == null)
                    {
                        int revisedPublishingInterval;
                        opcSubscription.Subscription = CreateSubscription(opcSubscription.RequestedPublishingInterval, out revisedPublishingInterval);
                        opcSubscription.PublishingInterval = revisedPublishingInterval;
                        Trace($"Create subscription on endpoint '{EndpointUri.AbsoluteUri}' requested OPC publishing interval is {opcSubscription.RequestedPublishingInterval} ms. (revised: {revisedPublishingInterval} ms)");
                    }

                    // process all unmonitored items.
                    var unmonitoredItems = opcSubscription.OpcMonitoredItems.Where(i => i.State == OpcMonitoredItem.OpcMonitoredItemState.Unmonitored);

                    foreach (var item in unmonitoredItems)
                    {
                        // if the session is disconnected, we stop trying and wait for the next cycle
                        if (State == SessionState.Disconnected)
                        {
                            break;
                        }

                        Trace($"Start monitoring nodes on endpoint '{EndpointUri.AbsoluteUri}'.");
                        NodeId currentNodeId;
                        try
                        {
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
                            MonitoredItem monitoredItem = new MonitoredItem()
                            {
                                StartNodeId = currentNodeId,
                                AttributeId = item.AttributeId,
                                DisplayName = node.DisplayName.Text,
                                MonitoringMode = item.MonitoringMode,
                                SamplingInterval = item.RequestedSamplingInterval,
                                QueueSize = item.QueueSize,
                                DiscardOldest = item.DiscardOldest
                            };
                            monitoredItem.Notification += item.Notification;
                            opcSubscription.Subscription.AddItem(monitoredItem);
                            opcSubscription.Subscription.SetPublishingMode(true);
                            opcSubscription.Subscription.ApplyChanges();
                            item.MonitoredItem = monitoredItem;
                            item.State = OpcMonitoredItem.OpcMonitoredItemState.Monitoreded;
                            item.EndpointUri = EndpointUri;
                            Trace($"Created monitored item for node '{currentNodeId}' in subscription with id {opcSubscription.Subscription.Id} on endpoint '{EndpointUri.AbsoluteUri}'");
                            if (item.RequestedSamplingInterval != monitoredItem.SamplingInterval)
                            {
                                Trace($"Sampling interval: requested: {item.RequestedSamplingInterval}; revised: {monitoredItem.SamplingInterval}");
                                item.SamplingInterval = monitoredItem.SamplingInterval;
                            }
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
                                        await Disconnect();
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
                }

                // remove unused subscriptions.
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    if (opcSubscription.OpcMonitoredItems.Count == 0)
                    {
                        Trace($"Subscription with id {opcSubscription.Subscription.Id} on endpoint '{EndpointUri}' is not used and will be deleted.");
                        Session.RemoveSubscription(opcSubscription.Subscription);
                        opcSubscription.Subscription = null;
                    }
                }

                // shutdown unused sessions.
                OpcSessionsSemaphore.Wait();
                var unusedSessions = OpcSessions.Where(s => s.OpcSubscriptions.Count == 0);
                foreach (var unusedSession in unusedSessions)
                {
                    await unusedSession.Shutdown();
                    OpcSessions.Remove(unusedSession);
                }
                OpcSessionsSemaphore.Release();
            }
            catch (Exception e)
            {
                Trace(e, "Error during ConnectAndMonitor.");
            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        /// <summary>
        /// Disconnects a session and removes all subscriptions on it and marks all nodes on those subscriptions
        /// as unmonitored.
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            _opcSessionSemaphore.Wait();
            try
            {
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    try
                    {
                        Session.RemoveSubscription(opcSubscription.Subscription);
                    }
                    catch
                    {
                        // the session might be already invalidated. ignore.
                    }
                    try
                    {
                        opcSubscription.Subscription.Delete(true);
                    }
                    catch
                    {
                        // the subscription might be already invalidated. ignore.
                    }
                    opcSubscription.Subscription = null;

                    // mark all monitored items as unmonitored
                    foreach (var opcMonitoredItem in opcSubscription.OpcMonitoredItems)
                    {
                        opcMonitoredItem.State = OpcMonitoredItem.OpcMonitoredItemState.Unmonitored;
                    }
                }
                try
                {
                    Session.Close();
                }
                    catch
                    {
                        // the session might be already invalidated. ignore.
                    }
                Session = null;
            }
            catch (Exception e)
            {
                // we do not care much. ignore.
            }
            State = SessionState.Disconnected;
            MissedKeepAlives = 0;

            _opcSessionSemaphore.Release();
        }

        /// <summary>
        /// Adds a node to be monitored. If there is no session to the endpoint, one is created.
        /// If there is no spubscription with the requested publishing interval, one is created.
        /// </summary>
        /// <param name="publishingInterval"></param>
        /// <param name="nodeId"></param>
        public void AddNodeForMonitoring(int publishingInterval, NodeId nodeId)
        {
            _opcSessionSemaphore.Wait();
            OpcSubscription opcSubscription = null;
            try
            {
                // find a subscription we could the node monitor on
                try
                {
                    opcSubscription = OpcSubscriptions.FirstOrDefault(s => s.RequestedPublishingInterval == publishingInterval);
                }
                catch
                {
                    opcSubscription = null;
                }
                // if there was none found, create one
                if (opcSubscription == null)
                {
                    int revisedPublishingInterval;
                    opcSubscription = new OpcSubscription(publishingInterval)
                    {
                        Subscription = CreateSubscription(publishingInterval, out revisedPublishingInterval),
                        PublishingInterval = revisedPublishingInterval
                    };
                }

                // if it is already there, we just ignore it, otherwise we add a new item to monitor.
                OpcMonitoredItem opcMonitoredItem = null;
                try
                {
                    opcMonitoredItem = opcSubscription.OpcMonitoredItems.FirstOrDefault(m => m.StartNodeId == nodeId);
                }
                catch
                {
                    opcMonitoredItem = null;
                }
                // if there was none found, create one
                if (opcMonitoredItem == null)
                {
                    // add a new item to monitor
                    opcMonitoredItem = new OpcMonitoredItem(nodeId, EndpointUri);
                    opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                }
            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        /// <summary>
        /// Tags a monitored node to stop monitoring.
        /// </summary>
        /// <param name="nodeId"></param>
        public void TagNodeForMonitoringStop(NodeId nodeId)
        {
            _opcSessionSemaphore.Wait();
            try
            {
                // find all subscriptions the node is monitored on.
                var opcSubscriptions = OpcSubscriptions.Where( s => s.OpcMonitoredItems.Any(m => m.StartNodeId == nodeId));

                // tag all monitored items with nodeId to stop monitoring.
                foreach (var opcSubscription in opcSubscriptions)
                {
                    var opcMonitoredItems = opcSubscription.OpcMonitoredItems.Where(i => i.StartNodeId == nodeId);
                    foreach (var opcMonitoredItem in opcMonitoredItems)
                    {
                        // tag it for removal.
                        opcMonitoredItem.State = OpcMonitoredItem.OpcMonitoredItemState.StopMonitoring;
                    }
                }
            }
            finally
            {
                _opcSessionSemaphore.Release();
            }
        }

        /// <summary>
        /// Shutsdown all connected sessions.
        /// </summary>
        /// <returns></returns>
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
                        foreach (var opcSubscription in OpcSubscriptions)
                        {
                            Trace($"Removing {opcSubscription.Subscription.MonitoredItemCount} monitored items from subscription with id {opcSubscription.Subscription.Id}.");
                            opcSubscription.Subscription.RemoveItems(opcSubscription.Subscription.MonitoredItems);
                        }
                        Trace($"Removing {Session.SubscriptionCount} subscriptions from session.");
                        Session.RemoveSubscriptions(Session.Subscriptions);
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
        /// Create a subscription in the session.
        /// </summary>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <returns></returns>
        private Subscription CreateSubscription(int requestedPublishingInterval, out int revisedPublishingInterval)
        {
            Subscription subscription = new Subscription()
            {
                PublishingInterval = requestedPublishingInterval,
            };
            // need to happen before the create to set the Session property.
            Session.AddSubscription(subscription);
            subscription.Create();
            Trace($"Created subscription with id {subscription.Id} on endpoint '{EndpointUri.AbsoluteUri}'");
            if (requestedPublishingInterval != subscription.PublishingInterval)
            {
                Trace($"Publishing interval: requested: {requestedPublishingInterval}; revised: {subscription.PublishingInterval}");
            }
            revisedPublishingInterval = subscription.PublishingInterval;
            return subscription;
        }

        /// <summary>
        /// Handler for the standard "keep alive" event sent by all OPC UA servers
        /// </summary>
        private static void StandardClient_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (e != null && session != null && session.ConfiguredEndpoint != null)
            {
                OpcSession opcSession = null;
                try
                {
                    OpcSessionsSemaphore.Wait();
                    var opcSessions = OpcSessions.Where(s => s.Session != null);
                    opcSession = opcSessions.Where(s => s.Session.ConfiguredEndpoint.EndpointUrl.Equals(session.ConfiguredEndpoint.EndpointUrl)).FirstOrDefault();
                }
                catch
                {
                    opcSession = null;
                }
                finally
                {
                    OpcSessionsSemaphore.Release();
                }

                if (!ServiceResult.IsGood(e.Status))
                {
                    Trace($"Session endpoint: {session.ConfiguredEndpoint.EndpointUrl} has Status: {e.Status}");
                    Trace($"Outstanding requests: {session.OutstandingRequestCount}, Defunct requests: {session.DefunctRequestCount}");
                    Trace($"Good publish requests: {session.GoodPublishRequestCount}, KeepAlive interval: {session.KeepAliveInterval}");
                    Trace($"SessionId: {session.SessionId}");

                    if (opcSession != null && opcSession.State == SessionState.Connected)
                    {
                        opcSession.MissedKeepAlives++;
                        Trace($"Missed KeepAlives: {opcSession.MissedKeepAlives}");
                        if (opcSession.MissedKeepAlives >= OpcKeepAliveDisconnectThreshold)
                        {
                            Trace($"Hit configured missed keep alive threshold of {Program.OpcKeepAliveDisconnectThreshold}. Disconnecting the session to endpoint {session.ConfiguredEndpoint.EndpointUrl}.");
                            session.KeepAlive -= StandardClient_KeepAlive;
                            opcSession.Disconnect();
                        }
                    }
                }
                else
                {
                    if (opcSession != null && opcSession.MissedKeepAlives != 0)
                    {
                        // Reset missed keep alive count
                        Trace($"Session endpoint: {session.ConfiguredEndpoint.EndpointUrl} got a keep alive after {opcSession.MissedKeepAlives} {(opcSession.MissedKeepAlives == 1 ? "was" : "were")} missed.");
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
