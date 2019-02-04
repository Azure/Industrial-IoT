
using Newtonsoft.Json;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using static OpcApplicationConfiguration;
    using static OpcMonitoredItem;
    using static OpcSession;
    using static Program;

    public static class PublisherNodeConfiguration
    {
        public static SemaphoreSlim PublisherNodeConfigurationSemaphore { get; set; }
        public static SemaphoreSlim PublisherNodeConfigurationFileSemaphore { get; set; }
        public static List<OpcSession> OpcSessions { get; } = new List<OpcSession>();
        public static SemaphoreSlim OpcSessionsListSemaphore { get; set; }

        public static string PublisherNodeConfigurationFilename { get; set; } = $"{System.IO.Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}publishednodes.json";

        public static int NumberOfOpcSessionsConfigured
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    result = OpcSessions.Count();
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        public static int NumberOfOpcSessionsConnected
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    result = OpcSessions.Count(s => s.State == OpcSession.SessionState.Connected);
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        public static int NumberOfOpcSubscriptionsConfigured
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    foreach (var opcSession in OpcSessions)
                    {

                        result += opcSession.GetNumberOfOpcSubscriptions();
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }
        public static int NumberOfOpcSubscriptionsConnected
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    var opcSessions = OpcSessions.Where(s => s.State == OpcSession.SessionState.Connected);
                    foreach (var opcSession in opcSessions)
                    {
                        result += opcSession.GetNumberOfOpcSubscriptions();
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        public static int NumberOfOpcMonitoredItemsConfigured
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    foreach (var opcSession in OpcSessions)
                    {
                        result += opcSession.GetNumberOfOpcMonitoredItemsConfigured();
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        public static int NumberOfOpcMonitoredItemsMonitored
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    var opcSessions = OpcSessions.Where(s => s.State == OpcSession.SessionState.Connected);
                    foreach (var opcSession in opcSessions)
                    {
                        result += opcSession.GetNumberOfOpcMonitoredItemsMonitored();
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        public static int NumberOfOpcMonitoredItemsToRemove
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    foreach (var opcSession in OpcSessions)
                    {
                        result += opcSession.GetNumberOfOpcMonitoredItemsToRemove();
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        /// <summary>
        /// Initialize resources for the node configuration.
        /// </summary>
        public static void Init()
        {
            OpcSessionsListSemaphore = new SemaphoreSlim(1);
            PublisherNodeConfigurationSemaphore = new SemaphoreSlim(1);
            PublisherNodeConfigurationFileSemaphore = new SemaphoreSlim(1);
            OpcSessions.Clear();
            _nodePublishingConfiguration = new List<NodePublishingConfigurationModel>();
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
        }

        /// <summary>
        /// Frees resources for the node configuration.
        /// </summary>
        public static void Deinit()
        {
            OpcSessions.Clear();
            _nodePublishingConfiguration = null;
            OpcSessionsListSemaphore.Dispose();
            OpcSessionsListSemaphore = null;
            PublisherNodeConfigurationSemaphore.Dispose();
            PublisherNodeConfigurationSemaphore = null;
            PublisherNodeConfigurationFileSemaphore.Dispose();
            PublisherNodeConfigurationFileSemaphore = null;
        }

        /// <summary>
        /// Read and parse the publisher node configuration file.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> ReadConfigAsync()
        {
            // get information on the nodes to publish and validate the json by deserializing it.
            try
            {
                await PublisherNodeConfigurationSemaphore.WaitAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                {
                    Logger.Information("Publishing node configuration file path read from environment.");
                    PublisherNodeConfigurationFilename = Environment.GetEnvironmentVariable("_GW_PNFP");
                }
                Logger.Information($"The name of the configuration file for published nodes is: {PublisherNodeConfigurationFilename}");

                // if the file exists, read it, if not just continue 
                if (File.Exists(PublisherNodeConfigurationFilename))
                {
                    Logger.Information($"Attemtping to load node configuration from: {PublisherNodeConfigurationFilename}");
                    try
                    {
                        await PublisherNodeConfigurationFileSemaphore.WaitAsync().ConfigureAwait(false);
                        _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
                    }
                    finally
                    {
                        PublisherNodeConfigurationFileSemaphore.Release();
                    }

                    if (_configurationFileEntries != null)
                    {
                        Logger.Information($"Loaded {_configurationFileEntries.Count} config file entry/entries.");
                        foreach (var publisherConfigFileEntryLegacy in _configurationFileEntries)
                        {
                            if (publisherConfigFileEntryLegacy.NodeId == null)
                            {
                                // new node configuration syntax.
                                foreach (var opcNode in publisherConfigFileEntryLegacy.OpcNodes)
                                {
                                    if (opcNode.ExpandedNodeId != null)
                                    {
                                        ExpandedNodeId expandedNodeId = ExpandedNodeId.Parse(opcNode.ExpandedNodeId);
                                        _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(expandedNodeId, opcNode.ExpandedNodeId,
                                            publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.UseSecurity,
                                            opcNode.OpcPublishingInterval, opcNode.OpcSamplingInterval, opcNode.DisplayName,
                                            opcNode.HeartbeatInterval, opcNode.SkipFirst));
                                    }
                                    else
                                    {
                                        // check Id string to check which format we have
                                        if (opcNode.Id.StartsWith("nsu=", StringComparison.InvariantCulture))
                                        {
                                            // ExpandedNodeId format
                                            ExpandedNodeId expandedNodeId = ExpandedNodeId.Parse(opcNode.Id);
                                            _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(expandedNodeId, opcNode.Id,
                                                publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.UseSecurity,
                                                opcNode.OpcPublishingInterval, opcNode.OpcSamplingInterval, opcNode.DisplayName,
                                                opcNode.HeartbeatInterval, opcNode.SkipFirst));
                                        }
                                        else
                                        {
                                            // NodeId format
                                            NodeId nodeId = NodeId.Parse(opcNode.Id);
                                            _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(nodeId, opcNode.Id,
                                                publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.UseSecurity,
                                                opcNode.OpcPublishingInterval, opcNode.OpcSamplingInterval, opcNode.DisplayName,
                                                opcNode.HeartbeatInterval, opcNode.SkipFirst));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // NodeId (ns=) format node configuration syntax using default sampling and publishing interval.
                                _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(publisherConfigFileEntryLegacy.NodeId,
                                    publisherConfigFileEntryLegacy.NodeId.ToString(), publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.UseSecurity,
                                    null, null, null,
                                    null, null));
                            }
                        }
                    }
                }
                else
                {
                    Logger.Information($"The node configuration file '{PublisherNodeConfigurationFilename}' does not exist. Continue and wait for remote configuration requests.");
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Loading of the node configuration file failed. Does the file exist and has correct syntax? Exiting...");
                return false;
            }
            finally
            {
                PublisherNodeConfigurationSemaphore.Release();
            }
            Logger.Information($"There are {_nodePublishingConfiguration.Count.ToString(CultureInfo.InvariantCulture)} nodes to publish.");
            return true;
        }

        /// <summary>
        /// Create the publisher data structures to manage OPC sessions, subscriptions and monitored items.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> CreateOpcPublishingDataAsync()
        {
            // create a list to manage sessions, subscriptions and monitored items.
            try
            {
                await PublisherNodeConfigurationSemaphore.WaitAsync().ConfigureAwait(false);
                await OpcSessionsListSemaphore.WaitAsync().ConfigureAwait(false);

                var uniqueEndpointUrls = _nodePublishingConfiguration.Select(n => n.EndpointUrl).Distinct();
                foreach (var endpointUrl in uniqueEndpointUrls)
                {
                    // create new session info.
                    OpcSession opcSession = new OpcSession(endpointUrl, _nodePublishingConfiguration.Where(n => n.EndpointUrl == endpointUrl).First().UseSecurity, OpcSessionCreationTimeout);

                    // create a subscription for each distinct publishing inverval
                    var nodesDistinctPublishingInterval = _nodePublishingConfiguration.Where(n => n.EndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase)).Select(c => c.OpcPublishingInterval).Distinct();
                    foreach (var nodeDistinctPublishingInterval in nodesDistinctPublishingInterval)
                    {
                        // create a subscription for the publishing interval and add it to the session.
                        OpcSubscription opcSubscription = new OpcSubscription(nodeDistinctPublishingInterval);

                        // add all nodes with this OPC publishing interval to this subscription.
                        var nodesWithSamePublishingInterval = _nodePublishingConfiguration.Where(n => n.EndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase)).Where(n => n.OpcPublishingInterval == nodeDistinctPublishingInterval);
                        foreach (var nodeInfo in nodesWithSamePublishingInterval)
                        {
                            // differentiate if NodeId or ExpandedNodeId format is used
                            if (nodeInfo.ExpandedNodeId != null)
                            {
                                // create a monitored item for the node, we do not have the namespace index without a connected session. 
                                // so request a namespace update.
                                OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.ExpandedNodeId, opcSession.EndpointUrl,
                                    nodeInfo.OpcSamplingInterval, nodeInfo.DisplayName, nodeInfo.HeartbeatInterval, nodeInfo.SkipFirst);
                                opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                                Interlocked.Increment(ref NodeConfigVersion);
                            }
                            else if (nodeInfo.NodeId != null)
                            {
                                // create a monitored item for the node with the configured or default sampling interval
                                OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.NodeId, opcSession.EndpointUrl,
                                    nodeInfo.OpcSamplingInterval, nodeInfo.DisplayName, nodeInfo.HeartbeatInterval, nodeInfo.SkipFirst);
                                opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                                Interlocked.Increment(ref NodeConfigVersion);
                            }
                            else
                            {
                                Logger.Error($"Node {nodeInfo.OriginalId} has an invalid format. Skipping...");
                            }
                        }

                        // add subscription to session.
                        opcSession.OpcSubscriptions.Add(opcSubscription);
                    }

                    // add session.
                    OpcSessions.Add(opcSession);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Creation of the internal OPC data managment structures failed. Exiting...");
                return false;
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
                PublisherNodeConfigurationSemaphore.Release();
            }
            return true;
        }

        /// <summary>
        /// Returns a list of all published nodes for a specific endpoint in config file format.
        /// </summary>
        /// <returns></returns>
        public static List<PublisherConfigurationFileEntryModel> GetPublisherConfigurationFileEntries(string endpointUrl, bool getAll, out uint nodeConfigVersion)
        {
            List<PublisherConfigurationFileEntryModel> publisherConfigurationFileEntries = new List<PublisherConfigurationFileEntryModel>();
            nodeConfigVersion = (uint)NodeConfigVersion;
            try
            {
                PublisherNodeConfigurationSemaphore.Wait();

                try
                {
                    OpcSessionsListSemaphore.Wait();

                    // itereate through all sessions, subscriptions and monitored items and create config file entries
                    foreach (var session in OpcSessions)
                    {
                        bool sessionLocked = false;
                        try
                        {
                            sessionLocked = session.LockSessionAsync().Result;
                            if (sessionLocked && (endpointUrl == null || session.EndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase)))
                            {
                                PublisherConfigurationFileEntryModel publisherConfigurationFileEntry = new PublisherConfigurationFileEntryModel();

                                publisherConfigurationFileEntry.EndpointUrl = new Uri(session.EndpointUrl);
                                publisherConfigurationFileEntry.UseSecurity = session.UseSecurity;
                                publisherConfigurationFileEntry.OpcNodes = new List<OpcNodeOnEndpointModel>();

                                foreach (var subscription in session.OpcSubscriptions)
                                {
                                    foreach (var monitoredItem in subscription.OpcMonitoredItems)
                                    {
                                        // ignore items tagged to stop
                                        if (monitoredItem.State != OpcMonitoredItemState.RemovalRequested || getAll == true)
                                        {
                                            OpcNodeOnEndpointModel opcNodeOnEndpoint = new OpcNodeOnEndpointModel(monitoredItem.OriginalId)
                                            {
                                                OpcPublishingInterval = subscription.RequestedPublishingIntervalFromConfiguration ? subscription.RequestedPublishingInterval : (int?)null,
                                                OpcSamplingInterval = monitoredItem.RequestedSamplingIntervalFromConfiguration ? monitoredItem.RequestedSamplingInterval : (int?)null,
                                                DisplayName = monitoredItem.DisplayNameFromConfiguration ? monitoredItem.DisplayName : null,
                                                HeartbeatInterval = monitoredItem.HeartbeatIntervalFromConfiguration ? (int?)monitoredItem.HeartbeatInterval : null,
                                                SkipFirst = monitoredItem.SkipFirstFromConfiguration ? (bool?)monitoredItem.SkipFirst : null
                                            };
                                            publisherConfigurationFileEntry.OpcNodes.Add(opcNodeOnEndpoint);
                                        }
                                    }
                                }
                                publisherConfigurationFileEntries.Add(publisherConfigurationFileEntry);
                            }
                        }
                        finally
                        {
                            if (sessionLocked)
                            {
                                session.ReleaseSession();
                            }
                        }
                    }
                    nodeConfigVersion = (uint)NodeConfigVersion;
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Reading configuration file entries failed.");
                publisherConfigurationFileEntries = null;
            }
            finally
            {
                PublisherNodeConfigurationSemaphore.Release();
            }
            return publisherConfigurationFileEntries;
        }

        /// <summary>
        /// Returns a list of all configured nodes in NodeId format.
        /// </summary>
        /// <returns></returns>
        public static async Task<List<PublisherConfigurationFileEntryLegacyModel>> GetPublisherConfigurationFileEntriesAsNodeIdsAsync(string endpointUrl)
        {
            List<PublisherConfigurationFileEntryLegacyModel> publisherConfigurationFileEntriesLegacy = new List<PublisherConfigurationFileEntryLegacyModel>();
            try
            {
                await PublisherNodeConfigurationSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    await OpcSessionsListSemaphore.WaitAsync().ConfigureAwait(false);

                    // itereate through all sessions, subscriptions and monitored items and create config file entries
                    foreach (var session in OpcSessions)
                    {
                        bool sessionLocked = false;
                        try
                        {
                            sessionLocked = await session.LockSessionAsync().ConfigureAwait(false);
                            if (sessionLocked && (endpointUrl == null || session.EndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase)))
                            {

                                foreach (var subscription in session.OpcSubscriptions)
                                {
                                    foreach (var monitoredItem in subscription.OpcMonitoredItems)
                                    {
                                        // ignore items tagged to stop
                                        if (monitoredItem.State != OpcMonitoredItemState.RemovalRequested)
                                        {
                                            PublisherConfigurationFileEntryLegacyModel publisherConfigurationFileEntryLegacy = new PublisherConfigurationFileEntryLegacyModel();
                                            publisherConfigurationFileEntryLegacy.EndpointUrl = new Uri(session.EndpointUrl);
                                            publisherConfigurationFileEntryLegacy.NodeId = null;
                                            publisherConfigurationFileEntryLegacy.OpcNodes = null;

                                            if (monitoredItem.ConfigType == OpcMonitoredItemConfigurationType.ExpandedNodeId)
                                            {
                                                // for certain scenarios we support returning the NodeId format even so the
                                                // actual configuration of the node was in ExpandedNodeId format
                                                publisherConfigurationFileEntryLegacy.EndpointUrl = new Uri(session.EndpointUrl);
                                                publisherConfigurationFileEntryLegacy.NodeId = new NodeId(monitoredItem.ConfigExpandedNodeId.Identifier, (ushort)session.GetNamespaceIndexUnlocked(monitoredItem.ConfigExpandedNodeId?.NamespaceUri));
                                                publisherConfigurationFileEntriesLegacy.Add(publisherConfigurationFileEntryLegacy);
                                            }
                                            else
                                            {
                                                // we do not convert nodes with legacy configuration to the new format to keep backward
                                                // compatibility with external configurations.
                                                // the conversion would only be possible, if the session is connected, to have access to the
                                                // server namespace array.
                                                publisherConfigurationFileEntryLegacy.EndpointUrl = new Uri(session.EndpointUrl);
                                                publisherConfigurationFileEntryLegacy.NodeId = monitoredItem.ConfigNodeId;
                                                publisherConfigurationFileEntriesLegacy.Add(publisherConfigurationFileEntryLegacy);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (sessionLocked)
                            {
                                session.ReleaseSession();
                            }
                        }
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Creation of configuration file entries failed.");
                publisherConfigurationFileEntriesLegacy = null;
            }
            finally
            {
                PublisherNodeConfigurationSemaphore.Release();
            }
            return publisherConfigurationFileEntriesLegacy;
        }

        /// <summary>
        /// Updates the configuration file to persist all currently published nodes
        /// </summary>
        public static async Task UpdateNodeConfigurationFileAsync()
        {
            try
            {
                // itereate through all sessions, subscriptions and monitored items and create config file entries
                uint nodeConfigVersion = 0;
                List<PublisherConfigurationFileEntryModel> publisherNodeConfiguration = GetPublisherConfigurationFileEntries(null, true, out nodeConfigVersion);

                // update the config file
                try
                {
                    await PublisherNodeConfigurationFileSemaphore.WaitAsync().ConfigureAwait(false);
                    await File.WriteAllTextAsync(PublisherNodeConfigurationFilename, JsonConvert.SerializeObject(publisherNodeConfiguration, Formatting.Indented)).ConfigureAwait(false);
                }
                finally
                {
                    PublisherNodeConfigurationFileSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Update of node configuration file failed.");
            }
        }

        private static List<NodePublishingConfigurationModel> _nodePublishingConfiguration;
        private static List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;
    }
}
