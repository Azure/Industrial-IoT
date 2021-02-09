// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic {
    using AsyncAwaitBestPractices;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Processor;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Validates the value changes within IoT Hub Methods
    /// </summary>
    public class TelemetryValidator : ITelemetryValidator
    {
        private CancellationTokenSource _cancellationTokenSource;
        private EventProcessorClient _client = null;
        private TimeSpan _opcDiffToNow = TimeSpan.Zero;
        private DateTime _startTime = DateTime.MinValue;
        private int _totalValueChangesCount = 0;
        private int _shuttingDown;

        /// <summary>
        /// Dictionary containing all sequence numbers related to a timestamp
        /// </summary>
        private ConcurrentDictionary<DateTime, int> _valueChangesPerTimestamp;

        /// <summary>
        /// Dictionary that contains the number of value changes (value) by Node Id (key)
        /// </summary>
        private ConcurrentDictionary<string, int> _valueChangesPerNodeId;

        /// <summary>
        /// Dictionary containing timestamps the were observed
        /// </summary>
        private ConcurrentQueue<DateTime> _observedTimestamps;

        private ConcurrentDictionary<DateTime, DateTime> _iotHubMessageEnqueuedTimes;

        /// <summary>
        /// Format to be used for Timestamps
        /// </summary>
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffffZ";
        /// <summary>
        /// Instance to write logs
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The current configuration the validator is using.
        /// </summary>
        private ValidatorConfiguration _currentConfiguration;

        public static readonly int kMissingValueCheckDelayMilliseconds = 10 * 1000;
        public static readonly int kMissingTimestampsCheckDelayMilliseconds = 10 * 1000;
        public static readonly int kStopDelayMilliseconds = 5 * 1000;

        public static readonly int kConcurrencyLevel = 4;
        public static readonly int kDefaultCapacity = 500;

        /// <summary>
        /// Create instance of TelemetryValidator
        /// </summary>
        /// <param name="logger">Instance to write logs</param>
        public TelemetryValidator(ILogger<TelemetryValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Method that runs asynchronously to connect to event hub and check
        /// a) if all expected value changes are delivered
        /// b) that time between value changes is expected
        /// </summary>
        /// <param name="token">Token to cancel the operation</param>
        /// <returns>Task that run until token is canceled</returns>
        public async Task<StartResult> StartAsync(ValidatorConfiguration configuration)
        {
            if (_cancellationTokenSource != null)
            {
                return new StartResult();
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.ExpectedValueChangesPerTimestamp < 0) {
                throw new ArgumentNullException("Invalid configuration detected, expected value changes per timestamp can't be lower than zero");
            }
            if (configuration.ExpectedIntervalOfValueChanges < 0) {
                throw new ArgumentNullException("Invalid configuration detected, expected interval of value changes can't be lower than zero");
            }
            if (configuration.ExpectedMaximalDuration < 0) {
                throw new ArgumentNullException("Invalid configuration detected, maximal total duration can't be lower than zero");
            }
            if (configuration.ThresholdValue <= 0) {
                throw new ArgumentNullException("Invalid configuration detected, threshold can't be negative or zero");
            }

            if (string.IsNullOrWhiteSpace(configuration.IoTHubEventHubEndpointConnectionString)) throw new ArgumentNullException(nameof(configuration.IoTHubEventHubEndpointConnectionString));
            if (string.IsNullOrWhiteSpace(configuration.StorageConnectionString)) throw new ArgumentNullException(nameof(configuration.StorageConnectionString));
            if (string.IsNullOrWhiteSpace(configuration.BlobContainerName)) throw new ArgumentNullException(nameof(configuration.BlobContainerName));
            if (string.IsNullOrWhiteSpace(configuration.EventHubConsumerGroup)) throw new ArgumentNullException(nameof(configuration.EventHubConsumerGroup));

            if (configuration.ExpectedMaximalDuration == 0) {
                configuration.ExpectedMaximalDuration = uint.MaxValue;
            }

            Interlocked.Exchange(ref _shuttingDown, 0);
            _currentConfiguration = configuration;

            _valueChangesPerTimestamp = new ConcurrentDictionary<DateTime, int>(kConcurrencyLevel, kDefaultCapacity);
            _valueChangesPerNodeId = new ConcurrentDictionary<string, int>(kConcurrencyLevel, kDefaultCapacity);
            _iotHubMessageEnqueuedTimes = new ConcurrentDictionary<DateTime, DateTime>(kConcurrencyLevel, kDefaultCapacity);
            _observedTimestamps = new ConcurrentQueue<DateTime>();
            Interlocked.Exchange(ref _totalValueChangesCount, 0);

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            token.ThrowIfCancellationRequested();
            _logger.LogInformation("Connecting to blob storage...");

            var blobContainerClient = new BlobContainerClient(configuration.StorageConnectionString, configuration.BlobContainerName);

            _logger.LogInformation("Connecting to IoT Hub...");

            _client = new EventProcessorClient(blobContainerClient, configuration.EventHubConsumerGroup, configuration.IoTHubEventHubEndpointConnectionString);
            _client.PartitionInitializingAsync += Client_PartitionInitializingAsync;
            _client.ProcessEventAsync += Client_ProcessEventAsync;
            _client.ProcessErrorAsync += Client_ProcessErrorAsync;

            _logger.LogInformation("Starting monitoring of events...");
            await _client.StartProcessingAsync(token);

            _startTime = DateTime.UtcNow;

            CheckForMissingValueChangesAsync(token).Start();
            CheckForMissingTimestampsAsync(token).Start();

            return new StartResult();
        }

        /// <summary>
        /// Stop monitoring of events.
        /// </summary>
        /// <returns></returns>
        public async Task<StopResult> StopAsync()
        {
            if (_observedTimestamps == null) {
                return new StopResult();
            }

            Interlocked.Exchange(ref _shuttingDown, 1);

            var endTime = DateTime.UtcNow;

            // to finish up messages related to a timestamp, we collect some more time
            await Task.Delay(kStopDelayMilliseconds);

            // check one last time if all messages related for timestamp are received
            CheckForMissingTimestamps();
            bool allInExpectedInterval = _observedTimestamps.IsEmpty;
            _logger.LogInformation("Number of incomplete timestamps while stopping: {IncompleteTimestamps}", _observedTimestamps.Count);

            if (_cancellationTokenSource != null) {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            // the stop procedure takes about a minute, so we fire and forget.
            StopEventProcessorClientAsync().SafeFireAndForget(e => _logger.LogError(e, "Error while stopping event monitoring."));

            bool allExpectedValueChanges = true;

            if (_currentConfiguration.ExpectedValueChangesPerTimestamp > 0) {

                // TODO collect "expected" parameter as groups related to OPC UA nodes
                allExpectedValueChanges = _valueChangesPerNodeId?.All(kvp =>
                                                   (kvp.Value / _totalValueChangesCount) == _currentConfiguration
                                                       .ExpectedValueChangesPerTimestamp) ??
                                               false;
                _logger.LogInformation("All expected value changes received: {AllExpectedValueChanges}",
                    allExpectedValueChanges);
            }

            var stopResult =  new StopResult() {
                ValueChangesByNodeId = new ReadOnlyDictionary<string, int>(_valueChangesPerNodeId ?? new ConcurrentDictionary<string, int>()),
                AllExpectedValueChanges = allExpectedValueChanges,
                TotalValueChangesCount = _totalValueChangesCount,
                AllInExpectedInterval = allInExpectedInterval,
                StartTime = _startTime,
                EndTime = endTime,
            };

            return stopResult;
        }

        /// <summary>
        /// Stops the Event Hub Client and deregisters event handlers.
        /// </summary>
        /// <returns></returns>
        private async Task StopEventProcessorClientAsync()
        {
            _logger.LogInformation("Stopping monitoring of events...");

            if (_client != null)
            {
                var tempClient = _client;
                _client = null;
                await tempClient.StopProcessingAsync();
                tempClient.PartitionInitializingAsync -= Client_PartitionInitializingAsync;
                tempClient.ProcessEventAsync -= Client_ProcessEventAsync;
                tempClient.ProcessErrorAsync -= Client_ProcessErrorAsync;
            }

            _logger.LogInformation("Stopped monitoring of events.");
        }

        /// <summary>
        /// Running a thread that analyze the value changes per timestamp
        /// </summary>
        /// <param name="token">Token to cancel the thread</param>
        /// <returns>Task that run until token is canceled</returns>
        private Task CheckForMissingValueChangesAsync(CancellationToken token)
        {
            return new Task(() => {
                try
                {
                    var formatInfoProvider = new DateTimeFormatInfo();
                    token.ThrowIfCancellationRequested();
                    while (!token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Currently, {total} value changes were received, currently waiting for {incompletedTimestamps} timestamp to be completed.", _totalValueChangesCount, _valueChangesPerTimestamp.Count);

                        var entriesToDelete = new List<DateTime>(50);
                        foreach (var missingSequence in _valueChangesPerTimestamp) {
                            var numberOfValueChanges = missingSequence.Value;
                            if (numberOfValueChanges >= _currentConfiguration.ExpectedValueChangesPerTimestamp) {
                                _logger.LogInformation(
                                    "Received {NumberOfValueChanges} value changes for timestamp {Timestamp}",
                                    numberOfValueChanges, missingSequence.Key);

                                // don't check for gaps of sequence numbers because they reflect the for number of messages
                                // send from OPC server to OPC publisher, it should be internally handled in OPCF stack

                                entriesToDelete.Add(missingSequence.Key);
                            }

                            // Check the total duration from OPC UA Server until IoT Hub

                            var iotHubEnqueuedTime = _iotHubMessageEnqueuedTimes[missingSequence.Key];
                            var durationDifference = iotHubEnqueuedTime.Subtract(missingSequence.Key);

                            if (durationDifference.TotalMilliseconds < 0)
                            {
                                _logger.LogWarning("Total duration is negative number, OPC UA Server time {OPCUATime}, IoTHub enqueue time {IoTHubTime}, delta {Diff}",
                                    missingSequence.Key.ToString(_dateTimeFormat, formatInfoProvider),
                                    iotHubEnqueuedTime.ToString(_dateTimeFormat, formatInfoProvider),
                                    durationDifference);
                            }
                            if (Math.Round(durationDifference.TotalMilliseconds) > _currentConfiguration.ExpectedMaximalDuration)
                            {
                                _logger.LogInformation("Total duration exceeded limit, OPC UA Server time {OPCUATime}, IoTHub enqueue time {IoTHubTime}, delta {Diff}",
                                    missingSequence.Key.ToString(_dateTimeFormat, formatInfoProvider),
                                    iotHubEnqueuedTime.ToString(_dateTimeFormat, formatInfoProvider),
                                    durationDifference);
                            }

                            // don'T check for duration between enqueued in IoTHub until processed here
                            // IoT Hub publish notifications slower than they can be received by IoT Hub
                            // ==> with longer runtime the difference between enqueued time and processing time will increase
                        }

                        // Remove all timestamps that are completed (all value changes received)
                        foreach (var entry in entriesToDelete)
                        {
                            var success = _valueChangesPerTimestamp.TryRemove(entry, out var values);
                            success &= _iotHubMessageEnqueuedTimes.TryRemove(entry, out var enqueuedTime);

                            if (!success)
                            {
                                _logger.LogError(
                                    "Could not remove timestamp {Timestamp} with all value changes from internal list",
                                    entry);
                            }
                            else
                            {
                                _logger.LogInformation("[Success] All value changes received for {Timestamp}", entry);
                            }
                        }

                        // Log total amount of missing value changes for each timestamp that already reported 80% of value changes
                        foreach (var missingSequence in _valueChangesPerTimestamp)
                        {
                            if (missingSequence.Value > (int)(_currentConfiguration.ExpectedValueChangesPerTimestamp * 0.8))
                            {
                                _logger.LogInformation(
                                    "For timestamp {Timestamp} there are {NumberOfMissing} value changes missing",
                                    missingSequence.Key,
                                    _currentConfiguration.ExpectedValueChangesPerTimestamp - missingSequence.Value);
                            }
                        }

                        Task.Delay(kMissingValueCheckDelayMilliseconds, token).Wait(token);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    if (oce.CancellationToken == token)
                    {
                        return;
                    }
                    throw;
                }
            }, token);
        }

        /// <summary>
        /// Running a thread that analyze that timestamps continually received (with expected interval)
        /// </summary>
        /// <param name="token">Token to cancel the thread</param>
        /// <returns>Task that run until token is canceled</returns>
        private Task CheckForMissingTimestampsAsync(CancellationToken token)
        {
            return new Task(() => {
                try
                {
                    while (!token.IsCancellationRequested) {

                        CheckForMissingTimestamps();
                        Task.Delay(kMissingTimestampsCheckDelayMilliseconds, token).Wait(token);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    if (oce.CancellationToken == token)
                    {
                        return;
                    }
                    throw;
                }
            }, token);
        }

        private void CheckForMissingTimestamps()
        {
            while (_observedTimestamps.Count >= 2)
            {
                bool success = _observedTimestamps.TryDequeue(out var older);
                success &= _observedTimestamps.TryDequeue(out var newer);

                if (!success) {
                    _logger.LogError("Can't dequeue timestamps from internal storage");
                }

                // compare on milliseconds isn't useful, instead try time window of 100 milliseconds
                var expectedTime = older.AddMilliseconds(_currentConfiguration.ExpectedIntervalOfValueChanges);

                var expectedMin = expectedTime.AddMilliseconds(-_currentConfiguration.ThresholdValue);
                var expectedMax = expectedTime.AddMilliseconds(_currentConfiguration.ThresholdValue);

                if (newer < expectedMin || newer > expectedMax) {
                    var expectedTS = expectedTime.ToString(_dateTimeFormat);
                    var olderTS = older.ToString(_dateTimeFormat);
                    var newerTS = newer.ToString(_dateTimeFormat);
                    _logger.LogWarning(
                        "Missing timestamp, value changes for {ExpectedTs} not received, predecessor {Older} successor {Newer}",
                        expectedTS,
                        olderTS,
                        newerTS);
                }
            }
        }

        /// <summary>
        /// Analyze payload of IoTHub message, adding timestamp and related sequence numbers into temporary
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>Task that run until token is canceled</returns>
        private async Task Client_ProcessEventAsync(ProcessEventArgs arg)
        {
            if (_cancellationTokenSource == null) //we already in stop process, so do not do anything new.
            {
                _logger.LogWarning("Received Events but nothing to do, because already stopped");
                return;
            }

            if (!arg.HasEvent)
            {
                _logger.LogWarning("Received partition event without content");
                return;
            }

            var body = arg.Data.Body.ToArray();
            var content = Encoding.UTF8.GetString(body);
            dynamic json = JsonConvert.DeserializeObject(content);
            int valueChangesCount = 0;

            // TODO build variant that works with PubSub

            foreach (dynamic entry in json)
            {
                DateTime entrySourceTimestamp;
                string entryNodeId = null;

                try
                {
                    entrySourceTimestamp = (DateTime)entry.Value.SourceTimestamp;
                    entryNodeId = entry.NodeId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not read sequence number, nodeId and/or timestamp from message. Please make sure that publisher is running with samples format and with --fm parameter set.");
                    return;
                }

                // don't process new timestamps when shutdown is triggered and even number of timestamps observed
                if (_shuttingDown != 0 && _observedTimestamps.Count % 2 == 0 && !_observedTimestamps.Contains(entrySourceTimestamp)) {
                    _logger.LogInformation("Ignore timestamp {TimeStamp} because Stop is already called", entrySourceTimestamp);
                    continue;
                }

                // check and report if processing delay has changed considerably, meaning more that the threshold
                var newOpcDiffToNow = DateTime.UtcNow - entrySourceTimestamp;
                var diffFromLastTime = newOpcDiffToNow - _opcDiffToNow;

                if (diffFromLastTime.TotalMilliseconds > _currentConfiguration.ThresholdValue)
                {
                    _logger.LogWarning("The different between UtcNow and Opc Source Timestamp has changed by {diff}", diffFromLastTime);
                }

                _opcDiffToNow = newOpcDiffToNow;

                if (_currentConfiguration.ExpectedValueChangesPerTimestamp > 0) {
                    _valueChangesPerTimestamp.AddOrUpdate(
                        entrySourceTimestamp,
                        (ts) => 0,
                        (ts, value) => ++value);
                }

                _valueChangesPerNodeId.AddOrUpdate(entryNodeId, 1, (k, v) => ++v);

                Interlocked.Increment(ref _totalValueChangesCount);

                valueChangesCount++;

                if (_currentConfiguration.ExpectedIntervalOfValueChanges > 0 && !_observedTimestamps.Contains(entrySourceTimestamp))
                {
                    _observedTimestamps.Enqueue(entrySourceTimestamp);

                    _iotHubMessageEnqueuedTimes.AddOrUpdate(
                        entrySourceTimestamp,
                        (_) => arg.Data.EnqueuedTime.UtcDateTime,
                        (ts, _) => arg.Data.EnqueuedTime.UtcDateTime);
                }
            }

            _logger.LogDebug("Received {NumberOfValueChanges} from IoTHub, partition {PartitionId}, ",
                valueChangesCount,
                arg.Partition.PartitionId);
        }

        /// <summary>
        /// Event handler that ensures only newest events are processed
        /// </summary>
        /// <param name="arg">Init event args</param>
        /// <returns>Completed Task, no async work needed</returns>
        private Task Client_PartitionInitializingAsync(PartitionInitializingEventArgs arg)
        {
            _logger.LogInformation("EventProcessorClient initializing, start with latest position for partition {PartitionId}", arg.PartitionId);
            arg.DefaultStartingPosition = EventPosition.Latest;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Event handler that logs errors from EventProcessorClient
        /// </summary>
        /// <param name="arg">Error event args</param>
        /// <returns>Completed Task, no async work needed</returns>
        private Task Client_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception, "Issue reported by EventProcessorClient, partition {PartitionId}, operation {Operation}",
                arg.PartitionId,
                arg.Operation);
            return Task.CompletedTask;
        }

    }
}
