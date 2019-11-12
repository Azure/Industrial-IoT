// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Crypto;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using static OpcApplicationConfiguration;
    using static OpcMonitoredItem;
    using static OpcSession;
    using static Program;
    using Opc.Ua.Extensions;

    public class PublisherNodeConfiguration : IPublisherNodeConfiguration, IDisposable
    {
        /// <summary>
        /// Keeps the version of the node configuration that has lastly been persisted
        /// </summary>
        private uint _lastNodeConfigVersion;

        /// <summary>
        /// Name of the node configuration file.
        /// </summary>
        public static string PublisherNodeConfigurationFilename { get; set; } = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}publishednodes.json";

        /// <summary>
        /// Number of configured OPC UA sessions.
        /// </summary>
        public int NumberOfOpcSessionsConfigured
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

        /// <summary>
        /// Number of connected OPC UA session.
        /// </summary>
        public int NumberOfOpcSessionsConnected
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    result = OpcSessions.Count(s => s.State == SessionState.Connected);
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                }
                return result;
            }
        }

        /// <summary>
        /// Number of configured OPC UA subscriptions.
        /// </summary>
        public int NumberOfOpcSubscriptionsConfigured
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
        /// <summary>
        /// Number of connected OPC UA subscriptions.
        /// </summary>
        public int NumberOfOpcSubscriptionsConnected
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    var opcSessions = OpcSessions.Where(s => s.State == SessionState.Connected);
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

        /// <summary>
        /// Number of OPC UA nodes configured to monitor.
        /// </summary>
        public int NumberOfOpcMonitoredItemsConfigured
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

        /// <summary>
        /// Number of monitored OPC UA nodes.
        /// </summary>
        public int NumberOfOpcMonitoredItemsMonitored
        {
            get
            {
                int result = 0;
                try
                {
                    OpcSessionsListSemaphore.Wait();
                    var opcSessions = OpcSessions.Where(s => s.State == SessionState.Connected);
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

        /// <summary>
        /// Number of OPC UA nodes requested to stop monitoring.
        /// </summary>
        public int NumberOfOpcMonitoredItemsToRemove
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
        /// Semaphore to protect the node configuration data structures.
        /// </summary>
        public SemaphoreSlim PublisherNodeConfigurationSemaphore { get; set; }

        /// <summary>
        /// Semaphore to protect the node configuration file.
        /// </summary>
        public SemaphoreSlim PublisherNodeConfigurationFileSemaphore { get; set; }

        /// <summary>
        /// Semaphore to protect the OPC UA sessions list.
        /// </summary>
        public SemaphoreSlim OpcSessionsListSemaphore { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        /// <summary>
        /// List of configured OPC UA sessions.
        /// </summary>
        public virtual List<IOpcSession> OpcSessions { get; set; } = new List<IOpcSession>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Get the singleton.
        /// </summary>
        public static IPublisherNodeConfiguration Instance
        {
            get
            {
                lock (_singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new PublisherNodeConfiguration();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Ctor to initialize resources for the telemetry configuration.
        /// </summary>
        public PublisherNodeConfiguration()
        {
            OpcSessionsListSemaphore = new SemaphoreSlim(1, 1);
            PublisherNodeConfigurationSemaphore = new SemaphoreSlim(1, 1);
            PublisherNodeConfigurationFileSemaphore = new SemaphoreSlim(1, 1);
            OpcSessions.Clear();
            _nodePublishingConfiguration = new List<NodePublishingConfigurationModel>();
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();

            // read the configuration from the configuration file
            if (!ReadConfigAsync().Result)
            {
                string errorMessage = $"Error while reading the node configuration file '{PublisherNodeConfigurationFilename}'";
                Logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // create the configuration data structures
            if (!CreateOpcPublishingDataAsync().Result)
            {
                string errorMessage = $"Error while creating node configuration data structures.";
                Logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                OpcSessionsListSemaphore.Wait();
                foreach (var opcSession in OpcSessions)
                {
                    opcSession.Dispose();
                }
                OpcSessions?.Clear();
                OpcSessionsListSemaphore?.Dispose();
                OpcSessionsListSemaphore = null;
                PublisherNodeConfigurationSemaphore?.Dispose();
                PublisherNodeConfigurationSemaphore = null;
                PublisherNodeConfigurationFileSemaphore?.Dispose();
                PublisherNodeConfigurationFileSemaphore = null;
                _nodePublishingConfiguration?.Clear();
                _nodePublishingConfiguration = null;
                lock (_singletonLock)
                {
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            // do cleanup
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize the node configuration.
        /// </summary>
        /// <returns></returns>
        public async Task InitAsync()
        {
            // read the configuration from the configuration file
            if (!await ReadConfigAsync().ConfigureAwait(false))
            {
                string errorMessage = $"Error while reading the node configuration file '{PublisherNodeConfigurationFilename}'";
                Logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // create the configuration data structures
            if (!await CreateOpcPublishingDataAsync().ConfigureAwait(false))
            {
                string errorMessage = $"Error while creating node configuration data structures.";
                Logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Read and parse the publisher node configuration file.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ReadConfigAsync()
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
                        string json = File.ReadAllText(PublisherNodeConfigurationFilename);
                        _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(json);
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
                                    _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(opcNode.ExpandedNodeId ?? opcNode.Id,
                                        publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.EndpointId,
                                        publisherConfigFileEntryLegacy.UseSecurity,
                                        publisherConfigFileEntryLegacy.SecurityProfileUri, publisherConfigFileEntryLegacy.SecurityMode,
                                        opcNode.OpcPublishingInterval, opcNode.OpcSamplingInterval, opcNode.DisplayName,
                                        opcNode.HeartbeatInterval, opcNode.SkipFirst, publisherConfigFileEntryLegacy.OpcAuthenticationMode, publisherConfigFileEntryLegacy.EncryptedAuthCredential));
                                }
                            }
                            else
                            {
                                // Legacy

                                // NodeId (ns=) format node configuration syntax using default sampling and publishing interval.
                                _nodePublishingConfiguration.Add(new NodePublishingConfigurationModel(publisherConfigFileEntryLegacy.NodeId,
                                    publisherConfigFileEntryLegacy.EndpointUrl.OriginalString, publisherConfigFileEntryLegacy.EndpointId,
                                    publisherConfigFileEntryLegacy.UseSecurity,
                                    publisherConfigFileEntryLegacy.SecurityProfileUri, publisherConfigFileEntryLegacy.SecurityMode,
                                    null, null, null,
                                    null, null, publisherConfigFileEntryLegacy.OpcAuthenticationMode, publisherConfigFileEntryLegacy.EncryptedAuthCredential));
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
        public async Task<bool> CreateOpcPublishingDataAsync()
        {
            // create a list to manage sessions, subscriptions and monitored items.
            try
            {
                await PublisherNodeConfigurationSemaphore.WaitAsync().ConfigureAwait(false);
                await OpcSessionsListSemaphore.WaitAsync().ConfigureAwait(false);

                var uniqueEndpointIds = _nodePublishingConfiguration
                    .Select(n => n.EndpointId)
                    .Distinct();
                foreach (string endpointId in uniqueEndpointIds)
                {
                    var currentNodePublishingConfiguration = _nodePublishingConfiguration
                        .First(n => n.EndpointId == endpointId);

                    EncryptedNetworkCredential encryptedAuthCredential = null;

                    if (currentNodePublishingConfiguration.OpcAuthenticationMode ==
                        OpcAuthenticationMode.UsernamePassword)
                    {
                        if (currentNodePublishingConfiguration.EncryptedAuthCredential == null)
                        {
                            throw new NullReferenceException(
                                "Could not retrieve credentials to authenticate to the server. " +
                                "Please check if 'OpcAuthenticationUsername' and 'OpcAuthenticationPassword' " +
                                "are set in configuration.");
                        }

                        encryptedAuthCredential = currentNodePublishingConfiguration.EncryptedAuthCredential;
                    }

                    // create new session info.
                    IOpcSession opcSession = new OpcSession(currentNodePublishingConfiguration.EndpointUrl, endpointId,
                        currentNodePublishingConfiguration.UseSecurity,
                        currentNodePublishingConfiguration.SecurityMode, currentNodePublishingConfiguration.SecurityProfileUri,
                        OpcSessionCreationTimeout,
                        currentNodePublishingConfiguration.OpcAuthenticationMode, encryptedAuthCredential);

                    // create a subscription for each distinct publishing inverval

                    // TODO: Rather consider a subscription id for partitioning

                    var nodesDistinctPublishingInterval = _nodePublishingConfiguration
                        .Where(n => n.EndpointId == endpointId)
                        .Select(c => c.OpcPublishingInterval).Distinct();
                    foreach (int? nodeDistinctPublishingInterval in nodesDistinctPublishingInterval)
                    {
                        // create a subscription for the publishing interval and add it to the session.
                        IOpcSubscription opcSubscription = new OpcSubscription(nodeDistinctPublishingInterval);

                        // add all nodes with this OPC publishing interval to this subscription.
                        var nodesWithSamePublishingInterval = _nodePublishingConfiguration
                            .Where(n => n.EndpointId == endpointId)
                            .Where(n => n.OpcPublishingInterval == nodeDistinctPublishingInterval);
                        foreach (var nodeInfo in nodesWithSamePublishingInterval)
                        {
                            if (NodeIdEx.IsValid(nodeInfo.OriginalId))
                            {
                                // create a monitored item for the node with the configured or default sampling interval
                                OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(
                                    nodeInfo.OriginalId, opcSession.Endpointuri, opcSession.EndpointId,
                                    nodeInfo.OpcSamplingInterval, nodeInfo.DisplayName,
                                    nodeInfo.HeartbeatInterval, nodeInfo.SkipFirst);
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
        public List<PublisherConfigurationFileEntryModel> GetPublisherConfigurationFileEntries(
            string endpointId, string endpointUrl, bool getAll, out uint nodeConfigVersion)
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
                            if (!sessionLocked)
                            {
                                continue;
                            }

                            if (endpointUrl != null && !session.Endpointuri.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (endpointId != null && session.EndpointId != endpointId)
                            {
                                continue;
                            }

                            PublisherConfigurationFileEntryModel publisherConfigurationFileEntry = new PublisherConfigurationFileEntryModel {
                                EndpointUrl = new Uri(session.Endpointuri),
                                EndpointId = session.EndpointId,
                                SecurityMode = session.SecurityMode,
                                SecurityProfileUri = session.SecurityProfileUri,
                                OpcAuthenticationMode = session.OpcAuthenticationMode,
                                EncryptedAuthCredential = session.EncryptedAuthCredential,
                                UseSecurity = session.UseSecurity,
                                OpcNodes = new List<OpcNodeOnEndpointModel>()
                            };

                            foreach (var subscription in session.OpcSubscriptions)
                            {
                                foreach (var monitoredItem in subscription.OpcMonitoredItems)
                                {
                                    // ignore items tagged to stop
                                    if (monitoredItem.State != OpcMonitoredItemState.RemovalRequested || getAll == true)
                                    {
                                        OpcNodeOnEndpointModel opcNodeOnEndpoint = new OpcNodeOnEndpointModel(monitoredItem.OriginalId) {
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
        public async Task<List<PublisherConfigurationFileEntryLegacyModel>> GetPublisherConfigurationFileEntriesAsNodeIdsAsync(
            string endpointId, string endpointUrl)
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

                            if (!sessionLocked)
                            {
                                continue;
                            }


                            if (endpointUrl != null &&
                                !session.Endpointuri.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }


                            if (endpointId != null && session.EndpointId != endpointId)
                            {
                                continue;
                            }

                            foreach (var subscription in session.OpcSubscriptions)
                            {
                                foreach (var monitoredItem in subscription.OpcMonitoredItems)
                                {
                                    // ignore items tagged to stop
                                    if (monitoredItem.State != OpcMonitoredItemState.RemovalRequested)
                                    {
                                        PublisherConfigurationFileEntryLegacyModel publisherConfigurationFileEntryLegacy = new PublisherConfigurationFileEntryLegacyModel {
                                            // Rest null - result only cares about the endpoint url and node id configuration
                                            EndpointUrl = new Uri(session.Endpointuri),
                                            NodeId = monitoredItem.ConfiguredNodeId
                                        };
                                        publisherConfigurationFileEntriesLegacy.Add(publisherConfigurationFileEntryLegacy);
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
        public async Task UpdateNodeConfigurationFileAsync()
        {
            if (NodeConfigVersion == _lastNodeConfigVersion)
            {
                return;
            }

            try
            {
                // iterate through all sessions, subscriptions and monitored items and create config file entries
                var publisherNodeConfiguration = GetPublisherConfigurationFileEntries(null, null, true, out _lastNodeConfigVersion);

                Logger.Debug($"Update node configuration file, version: {_lastNodeConfigVersion:X8}");

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

        private List<NodePublishingConfigurationModel> _nodePublishingConfiguration;
        private List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;

        private static readonly object _singletonLock = new object();
        private static IPublisherNodeConfiguration _instance;
    }
}
