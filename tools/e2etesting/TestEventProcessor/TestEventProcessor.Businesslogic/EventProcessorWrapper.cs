// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Businesslogic {

    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Processor;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wrapper for EventProcessorClient class.
    /// </summary>
    public class EventProcessorWrapper : IDisposable {

        private readonly ILogger _logger;
        private readonly IEventProcessorConfig _config;
        private bool _monitoringEnabled = false;

        private EventProcessorClient _client;
        private Dictionary<string, bool> _initializedPartitions;
        private SemaphoreSlim _lockInitializedPartitions;

        public event Func<ProcessEventArgs, Task> ProcessEventAsync;

        public EventProcessorWrapper(
            IEventProcessorConfig configuration,
            ILogger logger) {

            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrWhiteSpace(configuration.IoTHubEventHubEndpointConnectionString)) {
                throw new ArgumentNullException(nameof(configuration.IoTHubEventHubEndpointConnectionString));
            }
            if (string.IsNullOrWhiteSpace(configuration.StorageConnectionString)) {
                throw new ArgumentNullException(nameof(configuration.StorageConnectionString));
            }
            if (string.IsNullOrWhiteSpace(configuration.BlobContainerName)) {
                throw new ArgumentNullException(nameof(configuration.BlobContainerName));
            }
            if (string.IsNullOrWhiteSpace(configuration.EventHubConsumerGroup)) {
                throw new ArgumentNullException(nameof(configuration.EventHubConsumerGroup));
            }

            _config = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate hash based on the configuration.
        /// </summary>
        public static int GetHashCode(IEventProcessorConfig configuration) {
            var hash = new HashCode();
            hash.Add(configuration.IoTHubEventHubEndpointConnectionString);
            hash.Add(configuration.StorageConnectionString);
            hash.Add(configuration.BlobContainerName);
            hash.Add(configuration.EventHubConsumerGroup);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return GetHashCode(_config);
        }

        /// <summary>
        /// Initialize EventProcessorClient.
        /// </summary>
        public async Task InitializeClient(CancellationToken ct) {
            if (_client != null) {
                return;
            }

            // Initialize EventProcessorClient
            _logger.LogInformation("Connecting to blob storage...");
            var blobContainerClient = new BlobContainerClient(
                _config.StorageConnectionString,
                _config.BlobContainerName
            );

            // Get number of partitions and initialize _initializedPartitions.
            var eventHubConsumerClient = new EventHubConsumerClient(
                _config.EventHubConsumerGroup,
                _config.IoTHubEventHubEndpointConnectionString
            );

            var partitions = await eventHubConsumerClient
                .GetPartitionIdsAsync(ct)
                .ConfigureAwait(false);
            _initializedPartitions = partitions.ToDictionary(item => item, _ => false);
            _lockInitializedPartitions = new SemaphoreSlim(1, 1);

            _logger.LogInformation("Connecting to IoT Hub...");
            _client = new EventProcessorClient(
                blobContainerClient,
                _config.EventHubConsumerGroup,
                _config.IoTHubEventHubEndpointConnectionString
            );

            _client.PartitionClosingAsync += Client_PartitionClosingAsync;
            _client.PartitionInitializingAsync += Client_PartitionInitializingAsync;
            _client.ProcessEventAsync += Client_ProcessEventAsync;
            _client.ProcessErrorAsync += Client_ProcessErrorAsync;
        }

        /// <summary>
        /// Start processing of events. If the method was called previously, then it will only
        /// re-enable processing.
        /// </summary>
        public async Task StartProcessingAsync(CancellationToken ct) {
            if (_client == null) {
                throw new InvalidOperationException("EventProcessorWrapper has not been initialized.");
            }

            // If processing is active for all partitions then we only need to enable message propagation.
            await _lockInitializedPartitions.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (_initializedPartitions.Count(kvp => kvp.Value) == _initializedPartitions.Count) {
                    _logger.LogInformation("Enabling monitoring of events...");
                    _monitoringEnabled = true;
                    return;
                }
            }
            finally {
                _lockInitializedPartitions.Release();
            }

            if (!_client.IsRunning) {
                _logger.LogInformation("Starting monitoring of events...");
                await _client.StartProcessingAsync(ct).ConfigureAwait(false);
            }

            await WaitForPartitionInitialization(ct).ConfigureAwait(false);

            _monitoringEnabled = true;
        }

        /// <summary>
        /// Stop propagation of messages to downstream event handler.
        /// This will not close connection to EventHub.
        /// </summary>
        public void StopProcessing() {
            _logger.LogInformation("Disabling monitoring of events...");
            _monitoringEnabled = false;
        }

        /// <summary>
        /// Wait until we receive confirmation that monitoring of each partition has started.
        /// </summary>
        private async Task WaitForPartitionInitialization(CancellationToken ct) {
            var sw = Stopwatch.StartNew();

            while (!ct.IsCancellationRequested) {
                await _lockInitializedPartitions.WaitAsync(ct).ConfigureAwait(false);
                try {
                    if (_initializedPartitions.Count(kvp => kvp.Value) == _initializedPartitions.Count) {
                        _logger.LogInformation("Partition initialization took: {elapsed}", sw.Elapsed);
                        return;
                    }
                }
                finally {
                    _lockInitializedPartitions.Release();
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Event handler for monitoring partition closing events.
        /// </summary>
        private async Task Client_PartitionClosingAsync(PartitionClosingEventArgs arg) {
            _logger.LogInformation("EventProcessorClient partition closing: {PartitionId}", arg.PartitionId);

            await _lockInitializedPartitions.WaitAsync().ConfigureAwait(false);
            try {
                _initializedPartitions[arg.PartitionId] = false;
            }
            finally {
                _lockInitializedPartitions.Release();
            }
        }

        /// <summary>
        /// Event handler for monitoring partition initializing events.
        /// </summary>
        private async Task Client_PartitionInitializingAsync(PartitionInitializingEventArgs arg) {
            _logger.LogInformation("EventProcessorClient partition initializing, start with latest position for " +
                "partition {PartitionId}", arg.PartitionId);

            arg.DefaultStartingPosition = EventPosition.Latest;

            await _lockInitializedPartitions.WaitAsync().ConfigureAwait(false);
            try {
                _initializedPartitions[arg.PartitionId] = true;
            }
            finally {
                _lockInitializedPartitions.Release();
            }
        }

        /// <summary>
        /// Event handler for processing IoT Hub messages.
        /// </summary>
        private async Task Client_ProcessEventAsync(ProcessEventArgs arg) {
            if (!_monitoringEnabled) {
                return;
            }

            if (ProcessEventAsync != null) {
                await ProcessEventAsync.Invoke(arg).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Event handler that logs errors from EventProcessorClient.
        /// </summary>
        private Task Client_ProcessErrorAsync(ProcessErrorEventArgs arg) {
            _logger.LogError(arg.Exception, "EventProcessorClient issue reported, partition " +
                "{PartitionId}, operation {Operation}", arg.PartitionId, arg.Operation);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_client != null) {
                var tempClient = _client;
                _client = null;

                tempClient.StopProcessingAsync().Wait();
                tempClient.PartitionClosingAsync -= Client_PartitionClosingAsync;
                tempClient.PartitionInitializingAsync -= Client_PartitionInitializingAsync;
                tempClient.ProcessEventAsync -= Client_ProcessEventAsync;
                tempClient.ProcessErrorAsync -= Client_ProcessErrorAsync;
            }

            _logger.LogInformation("Stopped monitoring of events.");
        }
    }
}
