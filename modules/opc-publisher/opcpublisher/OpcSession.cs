using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpcPublisher
{
    using Opc.Ua;
    using OpcPublisher.Crypto;
    using System.Diagnostics;
    using System.Net;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using static OpcApplicationConfiguration;
    using static OpcPublisher.OpcMonitoredItem;
    using static Program;

    /// <summary>
    /// Class to manage OPC sessions.
    /// </summary>
    public class OpcSession : IOpcSession
    {
        /// <summary>
        /// The state of the session object.
        /// </summary>
        public enum SessionState
        {
            Disconnected = 0,
            Connecting,
            Connected,
        }

        /// <summary>
        /// Command line option to flag to read the node display names from the server and use it in telemetry events.
        /// </summary>
        public static bool FetchOpcNodeDisplayName { get; set; } = false;

        /// <summary>
        /// Command line argument to set the site to be added to telemetry events, identifying the source of the event,
        /// by prepending it to the ApplicationUri value of the event.
        /// </summary>
        public static string PublisherSite { get; set; }

#pragma warning disable CA2211 // Non-constant fields should not be visible
        /// <summary>
        /// The version of the node configuration. Each change in the configuration
        /// increments the version to protect get calls using continuation tokens.
        /// </summary>
        public static int NodeConfigVersion;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        /// <summary>
        /// Command line argument to control the time to wait till a new attempt is made
        /// to establish a connection which is not yet connected again.
        /// </summary>
        public static int SessionConnectWaitSec { get; set; } = 10;

        /// <summary>
        /// The endpoint to connect to for the session.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// The OPC UA stack session object of the session.
        /// </summary>
        public IOpcUaSession OpcUaClientSession { get; set; }

        /// <summary>
        /// The state of the session.
        /// </summary>
        public SessionState State { get; set; }

        /// <summary>
        /// The subscriptions on this session.
        /// </summary>
        public List<IOpcSubscription> OpcSubscriptions { get; }

        /// <summary>
        /// Counts session connection attempts which were unsuccessful.
        /// </summary>
        public uint UnsuccessfulConnectionCount { get; set; }

        /// <summary>
        /// Counts missed keep alives.
        /// </summary>
        public uint MissedKeepAlives { get; set; }

        /// <summary>
        /// The default publishing interval to use on this session.
        /// </summary>
        public int PublishingInterval { get; set; }

        /// <summary>
        /// The OPC UA timeout setting to use for the OPC UA session.
        /// </summary>
        public uint SessionTimeout { get; }

        /// <summary>
        /// Flag to control if a secure or unsecure OPC UA transport should be used for the session.
        /// </summary>
        public bool UseSecurity { get; set; } = true;

        /// <summary>
        /// Signals to run the connect and monitor task.
        /// </summary>
        public AutoResetEvent ConnectAndMonitorSession { get; set; }

        /// <summary>
        /// The encrypted credential for authentication against the OPC UA Server. This is only used, when <see cref="OpcAuthenticationMode"/> is set to "UsernamePassword".
        /// </summary>
        public EncryptedNetworkCredential EncryptedAuthCredential { get; set; }

        /// <summary>
        /// The authentication mode to use for authentication against the OPC UA Server.
        /// </summary>
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// Number of subscirptoins on this session.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Number of configured monitored items on this session.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfOpcMonitoredItemsConfigured()
        {
            int result = 0;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;
                if (sessionLocked)
                {
                    foreach (var subscription in OpcSubscriptions)
                    {
                        result += subscription.OpcMonitoredItems.Count();
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
        /// Number of actually monitored items on this sessions.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfOpcMonitoredItemsMonitored()
        {
            int result = 0;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;
                if (sessionLocked)
                {
                    foreach (var subscription in OpcSubscriptions)
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
        /// Number of monitored items to be removed from this session.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfOpcMonitoredItemsToRemove()
        {
            int result = 0;
            bool sessionLocked = false;
            try
            {
                sessionLocked = LockSessionAsync().Result;
                if (sessionLocked)
                {
                    foreach (var subscription in OpcSubscriptions)
                    {
                        result += subscription.OpcMonitoredItems.Count(i => i.State == OpcMonitoredItemState.RemovalRequested);
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
        public OpcSession(string endpointUrl, bool useSecurity, uint sessionTimeout, OpcAuthenticationMode opcAuthenticationMode, EncryptedNetworkCredential encryptedAuthCredential)
        {
            State = SessionState.Disconnected;
            EndpointUrl = endpointUrl;
            SessionTimeout = sessionTimeout * 1000;
            OpcSubscriptions = new List<IOpcSubscription>();
            UnsuccessfulConnectionCount = 0;
            MissedKeepAlives = 0;
            PublishingInterval = OpcPublishingInterval;
            UseSecurity = useSecurity;
            ConnectAndMonitorSession = new AutoResetEvent(false);
            _sessionCancelationTokenSource = new CancellationTokenSource();
            _sessionCancelationToken = _sessionCancelationTokenSource.Token;
            _opcSessionSemaphore = new SemaphoreSlim(1);
            _namespaceTable = new NamespaceTable();
            _telemetryConfiguration = TelemetryConfiguration.GetEndpointTelemetryConfiguration(endpointUrl);
            _connectAndMonitorAsync = Task.Run(ConnectAndMonitorAsync, _sessionCancelationToken);
            this.OpcAuthenticationMode= opcAuthenticationMode;
            this.EncryptedAuthCredential = encryptedAuthCredential;
        }

        public async Task Reconnect()
        {
            try
            {
                var sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                if (sessionLocked)
                {
                    InternalDisconnect();
                }

                if (State != SessionState.Disconnected)
                {
                    throw new Exception("Could not disconnect session.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while disconnecting session", ex);
            }
            finally
            {
                ReleaseSession();
            }
        }

        /// <summary>
        /// Ctor for the session.
        /// </summary>
        public OpcSession()
        {
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _sessionCancelationTokenSource?.Cancel();
                DisconnectAsync().Wait();
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    opcSubscription.Dispose();
                }
                OpcSubscriptions?.Clear();
                try
                {
                    _connectAndMonitorAsync.Wait();
                }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                {
                }
                _sessionCancelationTokenSource?.Dispose();
                _sessionCancelationTokenSource = null;
                _opcSessionSemaphore?.Dispose();
                _opcSessionSemaphore = null;
                OpcUaClientSession?.Dispose();
                OpcUaClientSession = null;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This task is started when a session is configured and is running till session shutdown and ensures:
        /// - disconnected sessions are reconnected.
        /// - monitored nodes are no longer monitored if requested to do so.
        /// - monitoring for a node starts if it is required.
        /// - unused subscriptions (without any nodes to monitor) are removed.
        /// - sessions with out subscriptions are removed.
        /// </summary>
        public virtual async Task ConnectAndMonitorAsync()
        {
            uint lastNodeConfigVersion = 0;

            WaitHandle[] connectAndMonitorEvents = {
                _sessionCancelationToken.WaitHandle,
                ConnectAndMonitorSession
            };

            // run till session is closed
            while (true)
            {
                try
                {
                    // wait till:
                    // - cancelation is requested
                    // - got signaled because we need to check for pending session activity
                    // - timeout to try to reestablish any disconnected sessions
                    try
                    {
                        WaitHandle.WaitAny(connectAndMonitorEvents, SessionConnectWaitSec * 1000);
                        _sessionCancelationToken.ThrowIfCancellationRequested();
                    }
                    catch
                    {
                        break;
                    }

                    await ConnectSessionAsync(_sessionCancelationToken).ConfigureAwait(false);

                    await MonitorNodesAsync(_sessionCancelationToken).ConfigureAwait(false);
                    _sessionCancelationToken.ThrowIfCancellationRequested();

                    await StopMonitoringNodesAsync(_sessionCancelationToken).ConfigureAwait(false);
                    _sessionCancelationToken.ThrowIfCancellationRequested();

                    await RemoveUnusedSubscriptionsAsync(_sessionCancelationToken).ConfigureAwait(false);
                    _sessionCancelationToken.ThrowIfCancellationRequested();

                    await RemoveUnusedSessionsAsync(_sessionCancelationToken).ConfigureAwait(false);
                    _sessionCancelationToken.ThrowIfCancellationRequested();

                    // update the config file if required
                    if (NodeConfigVersion != lastNodeConfigVersion)
                    {
                        lastNodeConfigVersion = (uint)NodeConfigVersion;
                        await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    if (!_sessionCancelationToken.IsCancellationRequested)
                    {
                        Logger.Error(e, "Exception");
                    }
                    else
                    {
                        break;
                    }
                }
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

                try
                {
                    sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                    // if the session is already connected or connecting or shutdown in progress, return
                    if (!sessionLocked || ct.IsCancellationRequested || State == SessionState.Connected || State == SessionState.Connecting)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                Logger.Information($"Connect and monitor session and nodes on endpoint '{EndpointUrl}'.");
                State = SessionState.Connecting;
                try
                {
                    // release the session to not block for high network timeouts.
                    ReleaseSession();
                    sessionLocked = false;

                    // start connecting
                    selectedEndpoint = CoreClientUtils.SelectEndpoint(EndpointUrl, UseSecurity);
                    configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(OpcApplicationConfiguration.ApplicationConfiguration));
                    uint timeout = SessionTimeout * ((UnsuccessfulConnectionCount >= OpcSessionCreationBackoffMax) ? OpcSessionCreationBackoffMax : UnsuccessfulConnectionCount + 1);
                    Logger.Information($"Create {(UseSecurity ? "secured" : "unsecured")} session for endpoint URI '{EndpointUrl}' with timeout of {timeout} ms.");

                    UserIdentity userIdentity = null;

                    switch (OpcAuthenticationMode)
                    {
                        case OpcAuthenticationMode.Anonymous:
                            userIdentity = new UserIdentity(new AnonymousIdentityToken());
                            break;
                        case OpcAuthenticationMode.UsernamePassword:
                            if (EncryptedAuthCredential == null)
                            {
                                throw new NullReferenceException("Please specity user credential to authentication with mode 'UsernamePassword'");
                            }

                            var plainCredential = await EncryptedAuthCredential.Decrypt();

                            userIdentity = new UserIdentity(plainCredential.UserName, plainCredential.Password);
                            break;
                        default:
                            throw new NotImplementedException($"The authentication mode '{OpcAuthenticationMode}' has not yet been implemented.");
                    }

                    OpcUaClientSession = new OpcUaSession(
                            OpcApplicationConfiguration.ApplicationConfiguration,
                            configuredEndpoint,
                            true,
                            false,
                            OpcApplicationConfiguration.ApplicationConfiguration.ApplicationName,
                            timeout,
                            userIdentity,
                            null);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Session creation to endpoint '{EndpointUrl}' failed {++UnsuccessfulConnectionCount} time(s). Please verify if server is up and Publisher configuration is correct.");
                    State = SessionState.Disconnected;
                    OpcUaClientSession = null;
                    return;
                }
                finally
                {
                    if (OpcUaClientSession != null)
                    {
                        sessionLocked = await LockSessionAsync().ConfigureAwait(false);
                        if (sessionLocked)
                        {
                            Logger.Information($"Session successfully created with Id {OpcUaClientSession.SessionId}.");
                            if (!selectedEndpoint.EndpointUrl.Equals(configuredEndpoint.EndpointUrl.OriginalString, StringComparison.OrdinalIgnoreCase))
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
                Logger.Error(e, "Exception");
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
        public async Task MonitorNodesAsync(CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                try
                {
                    sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                    // if the session is not connected or shutdown in progress, return
                    if (!sessionLocked || ct.IsCancellationRequested || State != SessionState.Connected)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                // ensure all nodes in all subscriptions of this session are monitored.
                foreach (var opcSubscription in OpcSubscriptions)
                {
                    // create the subscription, if it is not yet there.
                    if (opcSubscription.OpcUaClientSubscription == null)
                    {
                        opcSubscription.OpcUaClientSubscription = CreateSubscription(opcSubscription.RequestedPublishingInterval, out int revisedPublishingInterval);
                        opcSubscription.PublishingInterval = revisedPublishingInterval;
                        Logger.Information($"Create subscription on endpoint '{EndpointUrl}' requested OPC publishing interval is {opcSubscription.RequestedPublishingInterval} ms. (revised: {revisedPublishingInterval} ms)");
                    }

                    // process all unmonitored items.
                    var unmonitoredItems = opcSubscription.OpcMonitoredItems.Where(i => i.State == OpcMonitoredItemState.Unmonitored || i.State == OpcMonitoredItemState.UnmonitoredNamespaceUpdateRequested);
                    int additionalMonitoredItemsCount = 0;
                    int monitoredItemsCount = 0;
                    bool haveUnmonitoredItems = false;
                    if (unmonitoredItems.Count() != 0)
                    {
                        haveUnmonitoredItems = true;
                        monitoredItemsCount = opcSubscription.OpcMonitoredItems.Count(i => i.State == OpcMonitoredItemState.Monitored);
                        Logger.Information($"Start monitoring items on endpoint '{EndpointUrl}'. Currently monitoring {monitoredItemsCount} items.");
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
                                var ns = _namespaceTable.GetString(currentNodeId.NamespaceIndex);
                                item.ConfigExpandedNodeId = new ExpandedNodeId(currentNodeId, ns);
                            }

                            // if configured, get the DisplayName for the node, otherwise use the nodeId
                            Node node;
                            if (string.IsNullOrEmpty(item.DisplayName))
                            {
                                if (FetchOpcNodeDisplayName == true)
                                {
                                    node = OpcUaClientSession.ReadNode(currentNodeId);
                                    item.DisplayName = node.DisplayName.Text ?? currentNodeId.ToString();
                                }
                                else
                                {
                                    item.DisplayName = currentNodeId.ToString();
                                }
                            }

                            // handle skip first request
                            item.SkipNextEvent = item.SkipFirst;

                            // create a heartbeat timer, but no start it
                            if (item.HeartbeatInterval > 0)
                            {
                                item.HeartbeatSendTimer = new Timer(item.HeartbeatSend, null, Timeout.Infinite, Timeout.Infinite);
                            }

                            // add the new monitored item.
                            IOpcUaMonitoredItem monitoredItem = new OpcUaMonitoredItem
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
                            Logger.Verbose($"Created monitored item for node '{currentNodeId.ToString()}' in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}' on endpoint '{EndpointUrl}' (version: {NodeConfigVersion:X8})");
                            if (item.RequestedSamplingInterval != monitoredItem.SamplingInterval)
                            {
                                Logger.Information($"Sampling interval: requested: {item.RequestedSamplingInterval}; revised: {monitoredItem.SamplingInterval}");
                                item.SamplingInterval = monitoredItem.SamplingInterval;
                            }
                            if (additionalMonitoredItemsCount % 10000 == 0)
                            {
                                Logger.Information($"Now monitoring {monitoredItemsCount + additionalMonitoredItemsCount} items in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}'");
                            }
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
                                        Logger.Error($"Failed to monitor node '{currentNodeId}' on endpoint '{EndpointUrl}'.");
                                        Logger.Error($"OPC UA ServiceResultException is '{sre.Result}'. Please check your publisher configuration for this node.");
                                        break;
                                    }
                                default:
                                    {
                                        Logger.Error($"Unhandled OPC UA ServiceResultException '{sre.Result}' when monitoring node '{currentNodeId}' on endpoint '{EndpointUrl}'. Continue.");
                                        break;
                                    }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"Failed to monitor node '{currentNodeId}' on endpoint '{EndpointUrl}'");
                        }
                    }
                    opcSubscription.OpcUaClientSubscription.SetPublishingMode(true);
                    opcSubscription.OpcUaClientSubscription.ApplyChanges();
                    stopWatch.Stop();
                    if (haveUnmonitoredItems == true)
                    {
                        monitoredItemsCount = opcSubscription.OpcMonitoredItems.Count(i => i.State == OpcMonitoredItemState.Monitored);
                        Logger.Information($"Done processing unmonitored items on endpoint '{EndpointUrl}' took {stopWatch.ElapsedMilliseconds} msec. Now monitoring {monitoredItemsCount} items in subscription with id '{opcSubscription.OpcUaClientSubscription.Id}'.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception");
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
        /// Checks if there are monitored nodes tagged to stop monitoring.
        /// </summary>
        public async Task StopMonitoringNodesAsync(CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                try
                {
                    sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                    // if shutdown is in progress, return
                    if (!sessionLocked || ct.IsCancellationRequested)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                foreach (var opcSubscription in OpcSubscriptions)
                {
                    // remove items tagged to stop in the stack
                    var itemsToRemove = opcSubscription.OpcMonitoredItems.Where(i => i.State == OpcMonitoredItemState.RemovalRequested);
                    if (itemsToRemove.Any())
                    {
                        try
                        {
                            Logger.Information($"Remove nodes in subscription with id {opcSubscription.OpcUaClientSubscription.Id} on endpoint '{EndpointUrl}'");
                            opcSubscription.OpcUaClientSubscription.RemoveItems(itemsToRemove.Select(i => i.OpcUaClientMonitoredItem));
                            Logger.Information($"There are now {opcSubscription.OpcUaClientSubscription.MonitoredItemCount} monitored items in this subscription.");
                        }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                        catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                        {
                            // nodes may be tagged for stop before they are monitored, just continue
                        }
                        // stop heartbeat timer for all items to remove
                        foreach (var itemToRemove in itemsToRemove)
                        {
                            itemToRemove?.HeartbeatSendTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                        }
                        // remove them in our data structure
                        opcSubscription.OpcMonitoredItems.RemoveAll(i => i.State == OpcMonitoredItemState.RemovalRequested);
                        Interlocked.Increment(ref NodeConfigVersion);
                        Logger.Information($"There are now {opcSubscription.OpcMonitoredItems.Count} items managed by publisher for this subscription. (version: {NodeConfigVersion:X8})");
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
        }

        /// <summary>
        /// Checks if there are subscriptions without any monitored items and remove them.
        /// </summary>
        public async Task RemoveUnusedSubscriptionsAsync(CancellationToken ct)
        {
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                // if shutdown is in progress, return
                if (!sessionLocked || ct.IsCancellationRequested)
                {
                    return;
                }

                // remove the subscriptions in the stack
                var subscriptionsToRemove = OpcSubscriptions.Where(i => i.OpcMonitoredItems.Count == 0);
                if (subscriptionsToRemove.Any())
                {
                    try
                    {
                        Logger.Information($"Remove unused subscriptions on endpoint '{EndpointUrl}'.");
                        OpcUaClientSession.RemoveSubscriptions(subscriptionsToRemove.Select(s => s.OpcUaClientSubscription));
                        Logger.Information($"There are now {OpcUaClientSession.SubscriptionCount} subscriptions in this session.");
                    }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                    {
                        // subsriptions may be no longer required before they are created, just continue
                    }
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
                try
                {
                    await NodeConfiguration.OpcSessionsListSemaphore.WaitAsync().ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                // if shutdown is in progress, return
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                // remove sessions in the stack
                var sessionsToRemove = NodeConfiguration.OpcSessions.Where(s => s.OpcSubscriptions.Count == 0);
                foreach (var sessionToRemove in sessionsToRemove)
                {
                    Logger.Information($"Remove unused session on endpoint '{EndpointUrl}'.");
                    await sessionToRemove.ShutdownAsync().ConfigureAwait(false);
                }
                // remove then in our data structures
                NodeConfiguration.OpcSessions.RemoveAll(s => s.OpcSubscriptions.Count == 0);
            }
            finally
            {
                NodeConfiguration?.OpcSessionsListSemaphore?.Release();
            }
        }

        /// <summary>
        /// Disconnects a session and removes all subscriptions on it and marks all nodes on those subscriptions
        /// as unmonitored.
        /// </summary>
        public async Task DisconnectAsync()
        {
            bool sessionLocked = await LockSessionAsync().ConfigureAwait(false);
            if (sessionLocked)
            {
                try
                {
                    InternalDisconnect();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Exception while disconnecting '{EndpointUrl}'.");
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
                        if (opcSubscription.OpcUaClientSubscription != null)
                        {
                            OpcUaClientSession.RemoveSubscription(opcSubscription.OpcUaClientSubscription);
                        }
                    }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                    {
                        // the session might be already invalidated. ignore.
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
                    OpcUaClientSession?.Close();
                }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                {
                    // the session might be already invalidated. ignore.
                }
                OpcUaClientSession = null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception");
            }
            State = SessionState.Disconnected;
            MissedKeepAlives = 0;
        }

        /// <summary>
        /// Adds a node to be monitored. If there is no subscription with the requested publishing interval,
        /// one is created.
        /// </summary>
        public async Task<HttpStatusCode> AddNodeForMonitoringAsync(NodeId nodeId, ExpandedNodeId expandedNodeId,
            int? opcPublishingInterval, int? opcSamplingInterval, string displayName,
            int? heartbeatInterval, bool? skipFirst, CancellationToken ct)
        {
            string logPrefix = "AddNodeForMonitoringAsync:";
            bool sessionLocked = false;
            try
            {
                sessionLocked = await LockSessionAsync().ConfigureAwait(false);
                if (!sessionLocked || ct.IsCancellationRequested)
                {
                    return HttpStatusCode.Gone;
                }

                // check if there is already a subscription with the same publishing interval, which can be used to monitor the node
                int opcPublishingIntervalForNode = opcPublishingInterval ?? OpcPublishingIntervalDefault;
                IOpcSubscription opcSubscription = OpcSubscriptions.FirstOrDefault(s => s.RequestedPublishingInterval == opcPublishingIntervalForNode);

                // if there was none found, create one
                if (opcSubscription == null)
                {
                    if (opcPublishingInterval == null)
                    {
                        Logger.Information($"{logPrefix} No matching subscription with default publishing interval found.");
                        Logger.Information($"Create a new subscription with a default publishing interval.");
                    }
                    else
                    {
                        Logger.Information($"{logPrefix} No matching subscription with publishing interval of {opcPublishingInterval} found.");
                        Logger.Information($"Create a new subscription with a publishing interval of {opcPublishingInterval}.");
                    }
                    opcSubscription = new OpcSubscription(opcPublishingInterval);
                    OpcSubscriptions.Add(opcSubscription);
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
                        nodeIdCheck = new NodeId(expandedNodeId.Identifier, (ushort)_namespaceTable.GetIndex(expandedNodeId.NamespaceUri));
                    }
                }

                // if it is already published, we do nothing, else we create a new monitored item
                // todo check properties and update
                if (!IsNodePublishedInSessionInternal(nodeIdCheck, expandedNodeIdCheck))
                {
                    OpcMonitoredItem opcMonitoredItem = null;
                    // add a new item to monitor
                    if (expandedNodeId == null)
                    {
                        opcMonitoredItem = new OpcMonitoredItem(nodeId, EndpointUrl, opcSamplingInterval, displayName, heartbeatInterval, skipFirst);
                    }
                    else
                    {
                        opcMonitoredItem = new OpcMonitoredItem(expandedNodeId, EndpointUrl, opcSamplingInterval, displayName, heartbeatInterval, skipFirst);
                    }
                    opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                    Interlocked.Increment(ref NodeConfigVersion);
                    Logger.Debug($"{logPrefix} Added item with nodeId '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' for monitoring.");

                    // trigger the actual OPC communication with the server to be done
                    ConnectAndMonitorSession.Set();
                    return HttpStatusCode.Accepted;
                }
                else
                {
                    Logger.Debug($"{logPrefix} Node with Id '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' is already monitored.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"{logPrefix} Exception while trying to add node '{(expandedNodeId == null ? nodeId.ToString() : expandedNodeId.ToString())}' for monitoring.");
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
        public async Task<HttpStatusCode> RequestMonitorItemRemovalAsync(NodeId nodeId, ExpandedNodeId expandedNodeId, CancellationToken ct, bool takeLock = true)
        {
            HttpStatusCode result = HttpStatusCode.Gone;
            bool sessionLocked = false;
            try
            {
                if (takeLock)
                {
                    sessionLocked = await LockSessionAsync().ConfigureAwait(false);

                    if (!sessionLocked || ct.IsCancellationRequested)
                    {
                        return HttpStatusCode.Gone;
                    }
                }

                // create objects for publish check
                ExpandedNodeId expandedNodeIdCheck = expandedNodeId;
                NodeId nodeIdCheck = nodeId;
                if (State == SessionState.Connected)
                {
                    if (expandedNodeId == null)
                    {
                        string namespaceUri = _namespaceTable.ToArray().ElementAtOrDefault(nodeId.NamespaceIndex);
                        expandedNodeIdCheck = new ExpandedNodeId(nodeIdCheck, namespaceUri, 0);
                    }
                    if (nodeId == null)
                    {
                        nodeIdCheck = new NodeId(expandedNodeId.Identifier, (ushort)_namespaceTable.GetIndex(expandedNodeId.NamespaceUri));
                    }

                }

                // if node is not published return success
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
                ConnectAndMonitorSession.Set();
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
                Logger.Error(e, "Exception");
            }
            return false;
        }

        /// <summary>
        /// Checks if the node specified by either the given NodeId or ExpandedNodeId on the given endpoint is published in the session.
        /// </summary>
        public bool IsNodePublishedInSession(NodeId nodeId, ExpandedNodeId expandedNodeId)
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
                Logger.Error(e, "Exception");
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
        public static bool IsNodePublished(NodeId nodeId, ExpandedNodeId expandedNodeId, string endpointUrl)
        {
            try
            {
                NodeConfiguration.OpcSessionsListSemaphore.Wait();

                // itereate through all sessions, subscriptions and monitored items and create config file entries
                foreach (var opcSession in NodeConfiguration.OpcSessions)
                {
                    if (opcSession.EndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase))
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
                Logger.Error(e, "Exception");
            }
            finally
            {
                NodeConfiguration.OpcSessionsListSemaphore.Release();
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
                sessionLocked = await LockSessionAsync().ConfigureAwait(false);

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
                            IOpcSubscription opcSubscription = OpcSubscriptions.ElementAt(0);
                            OpcSubscriptions.RemoveAt(0);
                            IOpcUaSubscription opcUaClientSubscription = opcSubscription.OpcUaClientSubscription;
                            opcUaClientSubscription.Delete(true);
                        }
                        Logger.Information($"Closing session to endpoint URI '{EndpointUrl}' closed successfully.");
                        OpcUaClientSession.Close();
                        State = SessionState.Disconnected;
                        Logger.Information($"Session to endpoint URI '{EndpointUrl}' closed successfully.");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Exception while closing session to endpoint '{EndpointUrl}'.");
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
                    _opcSessionSemaphore.Release();
                    _opcSessionSemaphore.Dispose();
                    _opcSessionSemaphore = null;
                }
            }
        }

        /// <summary>
        /// Create a subscription in the session.
        /// </summary>
        private IOpcUaSubscription CreateSubscription(int requestedPublishingInterval, out int revisedPublishingInterval)
        {
            IOpcUaSubscription subscription = new OpcUaSubscription
            {
                PublishingInterval = requestedPublishingInterval,
            };
            // need to happen before the create to set the Session property.
            OpcUaClientSession.AddSubscription(subscription);
            subscription.Create();
            Logger.Information($"Created subscription with id {subscription.Id} on endpoint '{EndpointUrl}'");
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
        private void StandardClient_KeepAlive(Session session, KeepAliveEventArgs eventArgs)
        {
            // Ignore if we are shutting down.
            if (ShutdownTokenSource.IsCancellationRequested == true)
            {
                return;
            }

            if (eventArgs != null && session != null && session.ConfiguredEndpoint != null && OpcUaClientSession != null)
            {
                try
                {
                    if (!ServiceResult.IsGood(eventArgs.Status))
                    {
                        Logger.Warning($"Session endpoint: {session.ConfiguredEndpoint.EndpointUrl} has Status: {eventArgs.Status}");
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
                                Task t = Task.Run(() => DisconnectAsync().ConfigureAwait(false));
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
                catch (Exception e)
                {
                    Logger.Error(e, $"Exception in keep alive handling for endpoint '{session.ConfiguredEndpoint.EndpointUrl}'. ('{e.Message}'");
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
            try
            {
                await _opcSessionSemaphore.WaitAsync(_sessionCancelationToken).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
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
            _opcSessionSemaphore?.Release();
        }

        private SemaphoreSlim _opcSessionSemaphore;
        private CancellationTokenSource _sessionCancelationTokenSource;
        private readonly CancellationToken _sessionCancelationToken;
        private readonly NamespaceTable _namespaceTable;
        private readonly EndpointTelemetryConfigurationModel _telemetryConfiguration;
        private readonly Task _connectAndMonitorAsync;
    }
}
