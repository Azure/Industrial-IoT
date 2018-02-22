
using Newtonsoft.Json;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using static OpcMonitoredItem;
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static OpcStackConfiguration;

    public static class PublisherNodeConfiguration
    {
        public static SemaphoreSlim PublisherNodeConfigurationSemaphore;
        public static SemaphoreSlim PublisherNodeConfigurationFileSemaphore;
        public static List<OpcSession> OpcSessions;
        public static SemaphoreSlim OpcSessionsListSemaphore;

        public static string PublisherNodeConfigurationFilename
        {
            get => _publisherNodeConfigurationFilename;
            set => _publisherNodeConfigurationFilename = value;
        }

        public static int NumberOfOpcSessions
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

        public static int NumberOfConnectedOpcSessions
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

        public static int NumberOfConnectedOpcSubscriptions
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

        public static int NumberOfMonitoredItems
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
                        result += opcSession.GetNumberOfOpcMonitoredItems();
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
            OpcSessions = new List<OpcSession>();
            _nodePublishingConfiguration = new List<NodePublishingConfiguration>();
            _configurationFileEntries = new List<PublisherConfigurationFileEntry>();
        }

        /// <summary>
        /// Frees resources for the node configuration.
        /// </summary>
        public static void Deinit()
        {
            OpcSessions = null;
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
                await PublisherNodeConfigurationSemaphore.WaitAsync();
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                {
                    Trace("Publishing node configuration file path read from environment.");
                    _publisherNodeConfigurationFilename = Environment.GetEnvironmentVariable("_GW_PNFP");
                }
                Trace($"The name of the configuration file for published nodes is: {_publisherNodeConfigurationFilename}");

                // if the file exists, read it, if not just continue 
                if (File.Exists(_publisherNodeConfigurationFilename))
                {
                    Trace($"Attemtping to load node configuration from: {_publisherNodeConfigurationFilename}");
                    try
                    {
                        await PublisherNodeConfigurationFileSemaphore.WaitAsync();
                        _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntry>>(File.ReadAllText(_publisherNodeConfigurationFilename));
                    }
                    finally
                    {
                        PublisherNodeConfigurationFileSemaphore.Release();
                    }
                    Trace($"Loaded {_configurationFileEntries.Count} config file entry/entries.");

                    foreach (var publisherConfigFileEntry in _configurationFileEntries)
                    {
                        if (publisherConfigFileEntry.NodeId == null)
                        {
                            // new node configuration syntax.
                            foreach (var opcNode in publisherConfigFileEntry.OpcNodes)
                            {
                                ExpandedNodeId expandedNodeId = ExpandedNodeId.Parse(opcNode.ExpandedNodeId);
                                _nodePublishingConfiguration.Add(new NodePublishingConfiguration(expandedNodeId, publisherConfigFileEntry.EndpointUri, publisherConfigFileEntry.UseSecurity, opcNode.OpcSamplingInterval ?? OpcSamplingInterval, opcNode.OpcPublishingInterval ?? OpcPublishingInterval));
                            }
                        }
                        else
                        {
                            // NodeId (ns=) format node configuration syntax using default sampling and publishing interval.
                            _nodePublishingConfiguration.Add(new NodePublishingConfiguration(publisherConfigFileEntry.NodeId, publisherConfigFileEntry.EndpointUri, publisherConfigFileEntry.UseSecurity, OpcSamplingInterval, OpcPublishingInterval));
                            // give user a warning that the syntax is obsolete
                            Trace($"Please update the syntax of the configuration file and use ExpandedNodeId instead of NodeId property name for node with identifier '{publisherConfigFileEntry.NodeId.ToString()}' on EndpointUrl '{publisherConfigFileEntry.EndpointUri.AbsoluteUri}'.");

                        }
                    }
                }
                else
                {
                    Trace($"The node configuration file '{_publisherNodeConfigurationFilename}' does not exist. Starting up and wait for remote configuration requests.");
                }
            }
            catch (Exception e)
            {
                Trace(e, "Loading of the node configuration file failed. Does the file exist and has correct syntax?");
                Trace("exiting...");
                return false;
            }
            finally
            {
                PublisherNodeConfigurationSemaphore.Release();
            }
            Trace($"There are {_nodePublishingConfiguration.Count.ToString()} nodes to publish.");
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
                await PublisherNodeConfigurationSemaphore.WaitAsync();
                await OpcSessionsListSemaphore.WaitAsync();

                var uniqueEndpointUris = _nodePublishingConfiguration.Select(n => n.EndpointUri).Distinct();
                foreach (var endpointUri in uniqueEndpointUris)
                {
                    // create new session info.
                    OpcSession opcSession = new OpcSession(endpointUri, _nodePublishingConfiguration.Where(n => n.EndpointUri == endpointUri).First().UseSecurity, OpcSessionCreationTimeout);

                    // create a subscription for each distinct publishing inverval
                    var nodesDistinctPublishingInterval = _nodePublishingConfiguration.Where(n => n.EndpointUri.AbsoluteUri.Equals(endpointUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)).Select(c => c.OpcPublishingInterval).Distinct();
                    foreach (var nodeDistinctPublishingInterval in nodesDistinctPublishingInterval)
                    {
                        // create a subscription for the publishing interval and add it to the session.
                        OpcSubscription opcSubscription = new OpcSubscription(nodeDistinctPublishingInterval);

                        // add all nodes with this OPC publishing interval to this subscription.
                        var nodesWithSamePublishingInterval = _nodePublishingConfiguration.Where(n => n.EndpointUri.AbsoluteUri.Equals(endpointUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)).Where(n => n.OpcPublishingInterval == nodeDistinctPublishingInterval);
                        foreach (var nodeInfo in nodesWithSamePublishingInterval)
                        {
                            // differentiate if NodeId or ExpandedNodeId format is used
                            if (nodeInfo.NodeId == null)
                            {
                                // create a monitored item for the node, we do not have the namespace index without a connected session. 
                                // so request a namespace update.
                                OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.ExpandedNodeId, opcSession.EndpointUri)
                                {
                                    RequestedSamplingInterval = nodeInfo.OpcSamplingInterval,
                                    SamplingInterval = nodeInfo.OpcSamplingInterval
                                };
                                opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                            }
                            else
                            {
                                // create a monitored item for the node with the configured or default sampling interval
                                OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.NodeId, opcSession.EndpointUri)
                                {
                                    RequestedSamplingInterval = nodeInfo.OpcSamplingInterval,
                                    SamplingInterval = nodeInfo.OpcSamplingInterval
                                };
                                opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
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
                Trace(e, "Creation of the internal OPC data managment structures failed.");
                Trace("exiting...");
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
        public static async Task<List<PublisherConfigurationFileEntry>> GetPublisherConfigurationFileEntriesAsync(Uri endpointUri, OpcMonitoredItemConfigurationType? requestedType, bool getAll)
        {
            List<PublisherConfigurationFileEntry> publisherConfigurationFileEntries = new List<PublisherConfigurationFileEntry>();
            try
            {
                await PublisherNodeConfigurationSemaphore.WaitAsync();

                try
                {
                    await OpcSessionsListSemaphore.WaitAsync();

                    // itereate through all sessions, subscriptions and monitored items and create config file entries
                    foreach (var session in OpcSessions)
                    {
                        bool sessionLocked = false;
                        try
                        {
                            sessionLocked = await session.LockSessionAsync();
                            if (sessionLocked && (endpointUri == null || session.EndpointUri.AbsoluteUri.Equals(endpointUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)))
                            {
                                PublisherConfigurationFileEntry publisherConfigurationFileEntry = new PublisherConfigurationFileEntry();

                                publisherConfigurationFileEntry.EndpointUri = session.EndpointUri;
                                publisherConfigurationFileEntry.UseSecurity = session.UseSecurity;
                                publisherConfigurationFileEntry.NodeId = null;
                                publisherConfigurationFileEntry.OpcNodes = null;

                                foreach (var subscription in session.OpcSubscriptions)
                                {
                                    foreach (var monitoredItem in subscription.OpcMonitoredItems)
                                    {
                                        // ignore items tagged to stop
                                        if (monitoredItem.State != OpcMonitoredItemState.RemovalRequested || getAll == true)
                                        {
                                            OpcNodeOnEndpointUrl opcNodeOnEndpointUrl = new OpcNodeOnEndpointUrl();
                                            if (monitoredItem.ConfigType == OpcMonitoredItemConfigurationType.ExpandedNodeId)
                                            {
                                                // for certain scenarios we support returning the NodeId format even so the
                                                // actual configuration of the node was in ExpandedNodeId format
                                                if (requestedType == OpcMonitoredItemConfigurationType.NodeId)
                                                {
                                                    PublisherConfigurationFileEntry legacyPublisherConfigFileEntry = new PublisherConfigurationFileEntry();
                                                    legacyPublisherConfigFileEntry.EndpointUri = session.EndpointUri;
                                                    legacyPublisherConfigFileEntry.UseSecurity = session.UseSecurity;
                                                    legacyPublisherConfigFileEntry.NodeId = new NodeId(monitoredItem.ConfigExpandedNodeId.Identifier, (ushort)(session.GetNamespaceIndexUnlocked(monitoredItem.ConfigExpandedNodeId?.NamespaceUri)));
                                                    publisherConfigurationFileEntries.Add(legacyPublisherConfigFileEntry);
                                                }
                                                else
                                                {
                                                    opcNodeOnEndpointUrl.ExpandedNodeId = monitoredItem.ConfigExpandedNodeIdOriginal.ToString();
                                                    opcNodeOnEndpointUrl.OpcPublishingInterval = (int)subscription.RequestedPublishingInterval;
                                                    opcNodeOnEndpointUrl.OpcSamplingInterval = monitoredItem.RequestedSamplingInterval;
                                                    if (publisherConfigurationFileEntry.OpcNodes == null)
                                                    {
                                                        publisherConfigurationFileEntry.OpcNodes = new List<OpcNodeOnEndpointUrl>();
                                                    }
                                                    publisherConfigurationFileEntry.OpcNodes.Add(opcNodeOnEndpointUrl);
                                                }
                                            }
                                            else
                                            {
                                                // we do not convert nodes with legacy configuration to the new format to keep backward
                                                // compatibility with external configurations.
                                                // the conversion would only be possible, if the session is connected, to have access to the
                                                // server namespace array.
                                                PublisherConfigurationFileEntry legacyPublisherConfigFileEntry = new PublisherConfigurationFileEntry();
                                                legacyPublisherConfigFileEntry.EndpointUri = session.EndpointUri;
                                                legacyPublisherConfigFileEntry.UseSecurity = session.UseSecurity;
                                                legacyPublisherConfigFileEntry.NodeId = monitoredItem.ConfigNodeId;
                                                publisherConfigurationFileEntries.Add(legacyPublisherConfigFileEntry);
                                            }
                                        }
                                    }
                                }
                                if (publisherConfigurationFileEntry.OpcNodes != null)
                                {
                                    publisherConfigurationFileEntries.Add(publisherConfigurationFileEntry);
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
                Trace(e, "Creation of configuration file entries failed.");
            }
            finally
            {
                PublisherNodeConfigurationSemaphore.Release();
            }
            return publisherConfigurationFileEntries;
        }

        /// <summary>
        /// Updates the configuration file to persist all currently published nodes
        /// </summary>
        public static async Task UpdateNodeConfigurationFileAsync()
        {
            try
            {
                // itereate through all sessions, subscriptions and monitored items and create config file entries
                List<PublisherConfigurationFileEntry> publisherNodeConfiguration = await GetPublisherConfigurationFileEntriesAsync(null, null, true);

                // update the config file
                try
                {
                    await PublisherNodeConfigurationFileSemaphore.WaitAsync();
                    await File.WriteAllTextAsync(PublisherNodeConfigurationFilename, JsonConvert.SerializeObject(publisherNodeConfiguration, Formatting.Indented));
                }
                finally
                {
                    PublisherNodeConfigurationFileSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                Trace(e, "Update of node configuration file failed.");
            }
        }

        private static string _publisherNodeConfigurationFilename = $"{System.IO.Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}publishednodes.json";
        private static List<NodePublishingConfiguration> _nodePublishingConfiguration;
        private static List<PublisherConfigurationFileEntry> _configurationFileEntries;
    }

    /// <summary>
    /// Class describing a list of nodes in the ExpandedNodeId format
    /// </summary>
    public class OpcNodeOnEndpointUrl
    {
        public string ExpandedNodeId;

        [DefaultValue(OpcSamplingIntervalDefault)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval;

        [DefaultValue(OpcPublishingIntervalDefault)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval;
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports three formats:
    /// - NodeId syntax using the namespace index (ns) syntax
    /// - ExpandedNodeId syntax, using the namespace URI (nsu) syntax
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public partial class PublisherConfigurationFileEntry
    {
        public PublisherConfigurationFileEntry()
        {
        }

        public PublisherConfigurationFileEntry(string nodeId, string endpointUrl)
        {
            NodeId = new NodeId(nodeId);
            EndpointUri = new Uri(endpointUrl);
        }

        [JsonProperty("EndpointUrl")]
        public Uri EndpointUri { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public NodeId NodeId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<OpcNodeOnEndpointUrl> OpcNodes { get; set; }
    }

    /// <summary>
    /// Describes the publishing information of a node.
    /// </summary>
    public class NodePublishingConfiguration
    {
        public Uri EndpointUri;
        public bool UseSecurity;
        public NodeId NodeId;
        public ExpandedNodeId ExpandedNodeId;
        public int OpcSamplingInterval;
        public int OpcPublishingInterval;

        public NodePublishingConfiguration(NodeId nodeId, Uri endpointUri, bool? useSecurity, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = nodeId;
            ExpandedNodeId = null;
            EndpointUri = endpointUri;
            UseSecurity = useSecurity ?? true;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }
        public NodePublishingConfiguration(ExpandedNodeId expandedNodeId, Uri endpointUri, bool? useSecurity, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = null;
            ExpandedNodeId = expandedNodeId;
            EndpointUri = endpointUri;
            UseSecurity = useSecurity ?? true;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }
    }
}
