using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using System.Threading;

    public interface IPublisherNodeConfiguration
    {
        /// <summary>
        /// Number of configured OPC UA sessions.
        /// </summary>
        int NumberOfOpcSessionsConfigured { get; }

        /// <summary>
        /// Number of connected OPC UA session.
        /// </summary>
        int NumberOfOpcSessionsConnected { get; }

        /// <summary>
        /// Number of configured OPC UA subscriptions.
        /// </summary>
        int NumberOfOpcSubscriptionsConfigured { get; }

        /// <summary>
        /// Number of connected OPC UA subscriptions.
        /// </summary>
        int NumberOfOpcSubscriptionsConnected { get; }

        /// <summary>
        /// Number of OPC UA nodes configured to monitor.
        /// </summary>
        int NumberOfOpcMonitoredItemsConfigured { get; }

        /// <summary>
        /// Number of monitored OPC UA nodes.
        /// </summary>
        int NumberOfOpcMonitoredItemsMonitored { get; }

        /// <summary>
        /// Number of OPC UA nodes requested to stop monitoring.
        /// </summary>
        int NumberOfOpcMonitoredItemsToRemove { get; }

        /// <summary>
        /// Semaphore to protect the node configuration data structures.
        /// </summary>
        SemaphoreSlim PublisherNodeConfigurationSemaphore { get; set; }

        /// <summary>
        /// Semaphore to protect the node configuration file.
        /// </summary>
        SemaphoreSlim PublisherNodeConfigurationFileSemaphore { get; set; }

        /// <summary>
        /// Semaphore to protect the OPC UA sessions list.
        /// </summary>
        SemaphoreSlim OpcSessionsListSemaphore { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        /// <summary>
        /// List of configured OPC UA sessions.
        /// </summary>
        List<IOpcSession> OpcSessions { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Initialize the node configuration.
        /// </summary>
        /// <returns></returns>
        Task InitAsync();

        /// <summary>
        /// Read and parse the publisher node configuration file.
        /// </summary>
        /// <returns></returns>
        Task<bool> ReadConfigAsync();

        /// <summary>
        /// Create the publisher data structures to manage OPC sessions, subscriptions and monitored items.
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateOpcPublishingDataAsync();

        /// <summary>
        /// Returns a list of all published nodes for a specific endpoint in config file format.
        /// </summary>
        /// <returns></returns>
        List<PublisherConfigurationFileEntryModel> GetPublisherConfigurationFileEntries(string endpointUrl, bool getAll, out uint nodeConfigVersion);

        /// <summary>
        /// Returns a list of all configured nodes in NodeId format.
        /// </summary>
        /// <returns></returns>
        Task<List<PublisherConfigurationFileEntryLegacyModel>> GetPublisherConfigurationFileEntriesAsNodeIdsAsync(string endpointUrl);

        /// <summary>
        /// Updates the configuration file to persist all currently published nodes
        /// </summary>
        Task UpdateNodeConfigurationFileAsync();

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose();
    }
}
