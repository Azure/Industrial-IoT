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
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TestEventProcessor.Businesslogic;
    using TestEventProcessor.BusinessLogic.Checkers;

    /// <summary>
    /// Validates the value changes within IoT Hub Methods
    /// </summary>
    public class TelemetryValidator : ITelemetryValidator
    {
        private CancellationTokenSource _cancellationTokenSource;
        private EventProcessorClient _client = null;
        private DateTime _startTime = DateTime.MinValue;
        private int _totalValueChangesCount = 0;
        private int _shuttingDown;

        // Checkers
        private MissingTimestampsChecker _missingTimestampsChecker;
        private MessageProcessingDelayChecker _messageProcessingDelayChecker;
        private MessageDeliveryDelayChecker _messageDeliveryDelayChecker;
        private ValueChangeCounterPerNodeId _valueChangeCounterPerNodeId;
        private MissingValueChangesChecker _missingValueChangesChecker;
        private IncrementalIntValueChecker _incrementalIntValueChecker;

        /// <summary>
        /// Instance to write logs
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The current configuration the validator is using.
        /// </summary>
        private ValidatorConfiguration _currentConfiguration;

        public const int kCheckerDelayMilliseconds = 10_000;

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
            // Check if already started.
            if (_cancellationTokenSource != null) {
                return new StartResult();
            }

            // Check provided configuration.
            if (configuration == null) {
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

            Interlocked.Exchange(ref _totalValueChangesCount, 0);

            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize EventProcessorClient
            _logger.LogInformation("Connecting to blob storage...");
            var blobContainerClient = new BlobContainerClient(configuration.StorageConnectionString, configuration.BlobContainerName);

            _logger.LogInformation("Connecting to IoT Hub...");
            _client = new EventProcessorClient(blobContainerClient, configuration.EventHubConsumerGroup, configuration.IoTHubEventHubEndpointConnectionString);
            _client.PartitionInitializingAsync += Client_PartitionInitializingAsync;
            _client.ProcessEventAsync += Client_ProcessEventAsync;
            _client.ProcessErrorAsync += Client_ProcessErrorAsync;

            _logger.LogInformation("Starting monitoring of events...");
            await _client.StartProcessingAsync(_cancellationTokenSource.Token);

            _startTime = DateTime.UtcNow;

            // Initialize checkers.
            _missingTimestampsChecker = new MissingTimestampsChecker(
                TimeSpan.FromMilliseconds(_currentConfiguration.ExpectedIntervalOfValueChanges),
                TimeSpan.FromMilliseconds(_currentConfiguration.ThresholdValue),
                _logger
            );
            _missingTimestampsChecker.StartAsync(
                TimeSpan.FromMilliseconds(kCheckerDelayMilliseconds),
                _cancellationTokenSource.Token
            ).Start();

            _messageProcessingDelayChecker = new MessageProcessingDelayChecker(
                TimeSpan.FromMilliseconds(_currentConfiguration.ThresholdValue),
                _logger
            );

            _messageDeliveryDelayChecker = new MessageDeliveryDelayChecker(
                TimeSpan.FromMilliseconds(_currentConfiguration.ExpectedMaximalDuration),
                _logger
            );

            _valueChangeCounterPerNodeId = new ValueChangeCounterPerNodeId(_logger);

            _missingValueChangesChecker = new MissingValueChangesChecker(
                _currentConfiguration.ExpectedValueChangesPerTimestamp,
                _logger
            );
            _missingValueChangesChecker.StartAsync(
                TimeSpan.FromMilliseconds(kCheckerDelayMilliseconds),
                _cancellationTokenSource.Token
            ).Start();

            _incrementalIntValueChecker = new IncrementalIntValueChecker(_logger);

            return new StartResult();
        }

        /// <summary>
        /// Stop monitoring of events.
        /// </summary>
        /// <returns></returns>
        public Task<StopResult> StopAsync()
        {
            // Check if already stopped.
            if (_cancellationTokenSource == null) {
                return Task.FromResult(new StopResult());
            }

            Interlocked.Exchange(ref _shuttingDown, 1);

            var endTime = DateTime.UtcNow;

            if (_cancellationTokenSource != null) {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            // the stop procedure takes about a minute, so we fire and forget.
            StopEventProcessorClientAsync().SafeFireAndForget(e => _logger.LogError(e, "Error while stopping event monitoring."));

            // Stop checkers and collect resutls.
            var missingTimestampsCounter = _missingTimestampsChecker.Stop();
            var maxMessageProcessingDelay = _messageProcessingDelayChecker.Stop();
            var maxMessageDeliveryDelay = _messageDeliveryDelayChecker.Stop();

            var valueChangesPerNodeId = _valueChangeCounterPerNodeId.Stop();
            var allExpectedValueChanges = true;
            if (_currentConfiguration.ExpectedValueChangesPerTimestamp > 0) {

                // TODO collect "expected" parameter as groups related to OPC UA nodes
                allExpectedValueChanges = valueChangesPerNodeId?
                    .All(kvp => (_totalValueChangesCount / kvp.Value ) ==
                        _currentConfiguration.ExpectedValueChangesPerTimestamp
                    ) ?? false;
                _logger.LogInformation("All expected value changes received: {AllExpectedValueChanges}",
                    allExpectedValueChanges);
            }

            var incompleteTimestamps = _missingValueChangesChecker.Stop();

            var incrCheckerResult = _incrementalIntValueChecker.Stop();

            var stopResult =  new StopResult() {
                ValueChangesByNodeId = new ReadOnlyDictionary<string, int>(valueChangesPerNodeId ?? new Dictionary<string, int>()),
                AllExpectedValueChanges = allExpectedValueChanges,
                TotalValueChangesCount = _totalValueChangesCount,
                AllInExpectedInterval = missingTimestampsCounter == 0,
                StartTime = _startTime,
                EndTime = endTime,
                MaxDelayToNow = maxMessageProcessingDelay.ToString(),
                MaxDeliveyDuration = maxMessageDeliveryDelay.ToString(),
                DroppedValueCount = incrCheckerResult.DroppedValueCount,
                DuplicateValueCount = incrCheckerResult.DuplicateValueCount,
            };

            return Task.FromResult(stopResult);
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
        /// Analyze payload of IoTHub message, adding timestamp and related sequence numbers into temporary
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>Task that run until token is canceled</returns>
        private Task Client_ProcessEventAsync(ProcessEventArgs arg)
        {
            var eventReceivedTimestamp = DateTime.UtcNow;

            // Check if already stopped.
            if (_cancellationTokenSource == null) {
                _logger.LogWarning("Received Events but nothing to do, because already stopped");
                return Task.CompletedTask;
            }

            if (!arg.HasEvent) {
                _logger.LogWarning("Received partition event without content");
                return Task.CompletedTask;
            }

            var properties = arg.Data.Properties;
            var hasPubSubJsonHeader = properties.TryGetValue("$$ContentType", out var schema) ? schema.ToString() == MessageSchemaTypes.NetworkMessageJson : false;

            var body = arg.Data.Body.ToArray();
            var content = Encoding.UTF8.GetString(body);
            dynamic json = JsonConvert.DeserializeObject(content);
            var valueChangesCount = 0;

            foreach (dynamic entry in json){

                try {
                    // validate if the message has an OPC UA PubSub message type signature
                    if (entry.MessageType == "ua-data") {
                        if (!hasPubSubJsonHeader) {
                            _logger.LogInformation("Received event with \"ua-data\" signature but invalid content type header");
                        }

                        foreach (dynamic message in entry.Messages) {
                            var payload = message.Payload as JObject;
                            foreach (JProperty property in payload.Properties()) {
                                dynamic propertyValue = property.Value.ToObject<dynamic>();
                                FeedDataCheckers((string)property.Name, (DateTime)propertyValue.SourceTimestamp, arg.Data.EnqueuedTime.UtcDateTime, eventReceivedTimestamp, propertyValue.Value);
                                valueChangesCount++;
                            }
                        }
                    }
                    else {
                        FeedDataCheckers((string)entry.NodeId, (DateTime)entry.Value.SourceTimestamp, arg.Data.EnqueuedTime.UtcDateTime, eventReceivedTimestamp, entry.Value.Value);
                        valueChangesCount++;
                    }
                }
                catch (Exception ex){
                    _logger.LogError(ex, "Could not read sequence number, nodeId and/or timestamp from " +
                        "message. Please make sure that publisher is running with samples format and with " +
                        "--fm parameter set.");
                    continue;
                }
            }

            _logger.LogDebug("Received {NumberOfValueChanges} messages from IoT Hub, partition {PartitionId}.",
                valueChangesCount, arg.Partition.PartitionId);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Feed the checkers for the Data Change within the reveived event
        /// </summary>
        /// <param name="nodeId">Identifeir of the data source</param>
        /// <param name="sourceTimestamp">Timestamp at the Data Source </param>
        /// <param name="enqueuedTimestamp">IoT Hub message enqueue timestamp.</param>
        /// <param name="receivedTimestamp">Timestamp of arrival in the telemetry processor.</param>
        /// <param name="value">the actual value of the data change</param>
        private void FeedDataCheckers(string nodeId, DateTime sourceTimestamp, DateTime enqueuedTimestamp, DateTime receivedTimestamp, object value) {

            // OPC PLC contains bad fast and slow nodes that drop messages by design.
            // We will ignore entries that do not have a value.
            if (value is null) {
                return;
            }

            // Feed data to checkers.
            _missingTimestampsChecker.ProcessEvent(nodeId, sourceTimestamp, value);
            _messageProcessingDelayChecker.ProcessEvent(sourceTimestamp, receivedTimestamp);
            _messageDeliveryDelayChecker.ProcessEvent(sourceTimestamp, enqueuedTimestamp);
            _valueChangeCounterPerNodeId.ProcessEvent(nodeId, sourceTimestamp, value);
            _missingValueChangesChecker.ProcessEvent(sourceTimestamp);
            _incrementalIntValueChecker.ProcessEvent(nodeId, value);

            Interlocked.Increment(ref _totalValueChangesCount);
        }

        /// <summary>
        /// Event handler that ensures only newest events are processed
        /// </summary>
        /// <param name="arg">Init event args</param>
        /// <returns>Completed Task, no async work needed</returns>
        private Task Client_PartitionInitializingAsync(PartitionInitializingEventArgs arg)
        {
            _logger.LogInformation("EventProcessorClient initializing, start with latest position for " +
                "partition {PartitionId}", arg.PartitionId);
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
            _logger.LogError(arg.Exception, "Issue reported by EventProcessorClient, partition " +
                "{PartitionId}, operation {Operation}", arg.PartitionId, arg.Operation);
            return Task.CompletedTask;
        }

    }
}
