// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic {
    using Azure.Messaging.EventHubs.Processor;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TestEventProcessor.Businesslogic;
    using TestEventProcessor.BusinessLogic.Checkers;

    public class TelemetryValidator : ITelemetryValidator {
        private TelemetryValidatorProcessor _instance;
        private EventProcessorWrapper _lastClient;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ILogger<TelemetryValidator> _logger;

        /// <summary>
        /// Create instance of TelemetryValidator
        /// </summary>
        /// <param name="logger">Instance to write logs</param>
        public TelemetryValidator(ILogger<TelemetryValidator> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Method that runs asynchronously to connect to event hub and check
        /// a) if all expected value changes are delivered
        /// b) that time between value changes is expected
        /// </summary>
        /// <param name="token">Token to cancel the operation</param>
        /// <returns>Task that run until token is canceled</returns>
        public async Task<StartResult> StartAsync(ValidatorConfiguration configuration) {
            var sw = Stopwatch.StartNew();
            await _lock.WaitAsync();

            if (_instance != null) {
                _lock.Release();
                throw new InvalidOperationException("Already started telemetry validation processor.");
            }

            _logger.LogInformation("StartAsync called.");
            try {
                // Check if already started.
                Debug.Assert(_instance == null);

                _instance = new TelemetryValidatorProcessor(_logger, configuration);
                _lastClient = await _instance.StartAsync(_lastClient).ConfigureAwait(false);
                return new StartResult();
            }
            catch {
                if (_instance != null) {
                    // Failed to start, dispose and throw
                    _instance.Dispose();
                    _instance = null;
                }
                throw;
            }
            finally {
                _logger.LogInformation("StartAsync finished in {elapsed}", sw.Elapsed);
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop monitoring of events.
        /// </summary>
        /// <returns></returns>
        public async Task<StopResult> StopAsync() {
            var sw = Stopwatch.StartNew();
            await _lock.WaitAsync();
            try {
                if (_instance != null) {
                    _logger.LogInformation("StopAsync called.");
                    return _instance.Stop();
                }
                else {
                    // Alredy stopped, nothing to do
                    return new StopResult();
                }
            }
            finally {
                if (_instance != null) {
                    _instance.Dispose();
                    _instance = null;
                    _logger.LogInformation("StopAsync finished in {elapsed}", sw.Elapsed);
                }
                _lock.Release();
            }
        }

        /// <summary>
        /// Validates the value changes within IoT Hub Methods
        /// </summary>
        private sealed class TelemetryValidatorProcessor : IDisposable {
            private readonly CancellationTokenSource _cancellationTokenSource;
            private EventProcessorWrapper _clientWrapper;
            private DateTime _startTime = DateTime.MinValue;
            private int _totalValueChangesCount;
            private bool _restartAnnouncementReceived;

            // Checkers
            private readonly MissingTimestampsChecker _missingTimestampsChecker;
            private readonly MessageProcessingDelayChecker _messageProcessingDelayChecker;
            private readonly MessageDeliveryDelayChecker _messageDeliveryDelayChecker;
            private readonly ValueChangeCounterPerNodeId _valueChangeCounterPerNodeId;
            private readonly MissingValueChangesChecker _missingValueChangesChecker;
            private readonly IncrementalIntValueChecker _incrementalIntValueChecker;
            private readonly SequenceNumberChecker _incrementalSequenceChecker;

            /// <summary>
            /// Instance to write logs
            /// </summary>
            private readonly ILogger _logger;

            /// <summary>
            /// The current configuration the validator is using.
            /// </summary>
            private readonly ValidatorConfiguration _currentConfiguration;

            public const int kCheckerDelayMilliseconds = 10_000;

            /// <summary>
            /// Create instance of TelemetryValidator
            /// </summary>
            /// <param name="logger">Instance to write logs</param>
            public TelemetryValidatorProcessor(ILogger<TelemetryValidator> logger, ValidatorConfiguration configuration) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                if (configuration.ExpectedMaximalDuration == 0) {
                    configuration.ExpectedMaximalDuration = uint.MaxValue;
                }

                _currentConfiguration = configuration;

                Interlocked.Exchange(ref _totalValueChangesCount, 0);

                _cancellationTokenSource = new CancellationTokenSource();

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

                _incrementalSequenceChecker = new SequenceNumberChecker(_logger);

                _restartAnnouncementReceived = false;
            }

            public async Task<EventProcessorWrapper> StartAsync(EventProcessorWrapper clientWrapper) {
                if (clientWrapper == null || clientWrapper.GetHashCode() != EventProcessorWrapper.GetHashCode(_currentConfiguration)) {
                    if (clientWrapper != null) {
                        clientWrapper.Dispose();
                    }
                    _clientWrapper = new EventProcessorWrapper(_currentConfiguration, _logger);
                    await _clientWrapper.InitializeClient(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else {
                    _clientWrapper = clientWrapper;
                }
                _clientWrapper.ProcessEvent += Client_ProcessEventAsync;
                try {
                    await _clientWrapper.StartProcessingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    _startTime = DateTime.UtcNow;
                    return _clientWrapper;
                }
                catch {
                    _clientWrapper.ProcessEvent -= Client_ProcessEventAsync;
                    throw;
                }
            }

            /// <summary>
            /// Stop monitoring of events.
            /// </summary>
            /// <returns></returns>
            public StopResult Stop() {
                var endTime = DateTime.UtcNow;

                _cancellationTokenSource.Cancel();

                // Stop event monitoring and deregister handler.
                if (_clientWrapper != null) {
                    _clientWrapper.ProcessEvent -= Client_ProcessEventAsync;
                    _clientWrapper = null;
                }

                // Stop checkers and collect resutls.
                var missingTimestampsCounter = _missingTimestampsChecker.Stop();
                var maxMessageProcessingDelay = _messageProcessingDelayChecker.Stop();
                var maxMessageDeliveryDelay = _messageDeliveryDelayChecker.Stop();
                var valueChangesPerNodeId = _valueChangeCounterPerNodeId.Stop();
                var allExpectedValueChanges = true;
                if (_currentConfiguration?.ExpectedValueChangesPerTimestamp > 0) {

                    // TODO collect "expected" parameter as groups related to OPC UA nodes
                    allExpectedValueChanges = valueChangesPerNodeId?
                        .All(kvp => (_totalValueChangesCount / kvp.Value) ==
                            _currentConfiguration.ExpectedValueChangesPerTimestamp
                        ) ?? false;
                    _logger.LogInformation("All expected value changes received: {AllExpectedValueChanges}",
                        allExpectedValueChanges);
                }

                var incompleteTimestamps = _missingValueChangesChecker.Stop();
                var incrCheckerResult = _incrementalIntValueChecker.Stop();
                var incrSequenceResult = _incrementalSequenceChecker.Stop();

                var stopResult = new StopResult() {
                    ValueChangesByNodeId = new ReadOnlyDictionary<string, int>(valueChangesPerNodeId ?? new Dictionary<string, int>()),
                    AllExpectedValueChanges = allExpectedValueChanges,
                    TotalValueChangesCount = _totalValueChangesCount,
                    AllInExpectedInterval = missingTimestampsCounter == 0,
                    StartTime = _startTime,
                    EndTime = endTime,
                    MaxDelayToNow = maxMessageProcessingDelay.ToString(),
                    MaxDeliveyDuration = maxMessageDeliveryDelay.ToString(),
                    DroppedValueCount = incrCheckerResult?.DroppedValueCount ?? 0,
                    DuplicateValueCount = incrCheckerResult?.DuplicateValueCount ?? 0,
                    DroppedSequenceCount = incrSequenceResult?.DroppedValueCount ?? 0,
                    DuplicateSequenceCount = incrSequenceResult?.DuplicateValueCount ?? 0,
                    ResetSequenceCount = incrSequenceResult?.ResetsValueCount ?? 0,
                    RestartAnnouncementReceived = _restartAnnouncementReceived,
                };

                return stopResult;
            }

            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose() {
                // Stop event monitoring and deregister handler.
                if (_clientWrapper != null) {
                    _clientWrapper.ProcessEvent -= Client_ProcessEventAsync;
                    _clientWrapper = null;
                }
                _cancellationTokenSource.Dispose();
            }


            /// <summary>
            /// Analyze payload of IoTHub message, adding timestamp and related sequence numbers into temporary
            /// </summary>
            /// <param name="arg"></param>
            /// <returns>Task that run until token is canceled</returns>
            private void Client_ProcessEventAsync(ProcessEventArgs arg) {
                var sw = Stopwatch.StartNew();

                var eventReceivedTimestamp = DateTime.UtcNow;

                try {
                    if (!arg.HasEvent) {
                        _logger.LogWarning("Received partition event without content");
                        return;
                    }

                    if (arg.Data.EnqueuedTime.UtcDateTime < _startTime) {
                        _logger.LogDebug("Received message enqueued before starting....");
                        return;
                    }

                    if (_cancellationTokenSource.IsCancellationRequested) {
                        _logger.LogDebug("Received message after stopping....");
                        return;
                    }

                    var properties = arg.Data.Properties;
                    var hasPubSubJsonHeader = properties.TryGetValue("$$ContentType", out var schema)
                        ? schema.ToString() == MessageSchemaTypes.NetworkMessageJson : false;

                    var body = arg.Data.Body.ToArray();
                    var content = Encoding.UTF8.GetString(body);
                    var json = JToken.Parse(content);

                    if (json is JObject o && CheckRestartAnnouncement(o)) {
                        return;
                    }

                    var valueChangesCount = 0;

                    if (json is JArray batch) {
                        foreach (var entry in batch) {
                            ProcessMessage(entry);
                        }
                    }
                    else {
                        ProcessMessage(json);
                    }

                    void ProcessMessage(JToken messageToken) {
                        if (messageToken is not JObject entry) {
                            return;
                        }
                        try {
                            // validate if the message has an OPC UA PubSub message type signature
                            if (entry.TryGetValue("MessageType", out var messageType) && ((string)messageType) == "ua-data") {
                                if (!hasPubSubJsonHeader) {
                                    _logger.LogInformation("Received {item} with \"ua-data\" signature but invalid content type header",
                                        entry.ToString());
                                }

                                if (!entry.TryGetValue("Messages", out var a) || a is not JArray messages) {
                                    _logger.LogInformation("Received {item} without messages.", entry.ToString());
                                    return;
                                }

                                foreach (var token in messages) {
                                    if (token is not JObject message) {
                                        _logger.LogInformation("Message {message} is not an object.", token.ToString());
                                        continue;
                                    }
                                    if (!message.TryGetValue("DataSetWriterId", out var dataSetWriterId)) {
                                        _logger.LogInformation("Message {message} does not contain writer id.", message.ToString());
                                        continue;
                                    }
                                    if (!message.TryGetValue("SequenceNumber", out var sequenceNumber)) {
                                        _logger.LogInformation("Message {message} does not contain sequence number.", message.ToString());
                                        sequenceNumber = JValue.CreateNull();
                                    }

                                    FeedDataChangeCheckers((string)dataSetWriterId, sequenceNumber.ToObject<uint?>());

                                    if (!message.TryGetValue("Payload", out var p) || p is not JObject payload) {
                                        _logger.LogInformation("Message {item} does not have any 'Payload' object.", entry.ToString());
                                        continue;
                                    }
                                    foreach (JProperty property in payload.Properties()) {
                                        if (property.Value is not JObject dataValue) {
                                            _logger.LogInformation("Payload property {Property} does not have data value.",
                                                property.Value.ToString());
                                            continue;
                                        }
                                        if (!dataValue.TryGetValue("SourceTimestamp", out var st) || st is not JValue sourceTimeStamp) {
                                            _logger.LogInformation("Value is missing source timestamp.", dataValue.ToString());
                                            continue;
                                        }
                                        if (!dataValue.TryGetValue("Value", out var value)) {
                                            value = JValue.CreateNull();
                                        }
                                        FeedDataCheckers(
                                            property.Name,
                                            sourceTimeStamp.ToObject<DateTime>(),
                                            arg.Data.EnqueuedTime.UtcDateTime,
                                            eventReceivedTimestamp,
                                            value);
                                        valueChangesCount++;
                                    }
                                }
                            }
                            else if (entry.TryGetValue("NodeId", out var nodeId)) {
                                if (!entry.TryGetValue("Value", out var v) || v is not JObject dataValue) {
                                    _logger.LogInformation("Message {Message} does not have data value.", entry.ToString());
                                    return;
                                }
                                if (!dataValue.TryGetValue("SourceTimestamp", out var st) || st is not JValue sourceTimeStamp) {
                                    _logger.LogInformation("Value is missing source timestamp.", dataValue.ToString());
                                    return;
                                }
                                if (!dataValue.TryGetValue("Value", out var value)) {
                                    value = JValue.CreateNull();
                                }
                                if (entry.TryGetValue("Status", out var status) && status.ToString() != "Good") {
                                    _logger.LogInformation("Value has status {Status}.", status.ToString());
                                }
                                FeedDataCheckers(
                                    (string)nodeId,
                                    sourceTimeStamp.ToObject<DateTime>(),
                                    arg.Data.EnqueuedTime.UtcDateTime,
                                    eventReceivedTimestamp,
                                    value);
                                valueChangesCount++;
                            }
                            else {
                                _logger.LogInformation("Message {Message} not a publisher message.", entry.ToString());
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) {
                            _logger.LogError(ex, "Could not read sequence number, nodeId and/or timestamp from " +
                                "message. Please make sure that publisher is running with samples format and with " +
                                "--fm parameter set. Message: {Content}", content);
                        }
                    }

                    _logger.LogDebug("Received {NumberOfValueChanges} messages from IoT Hub, partition {PartitionId}.",
                        valueChangesCount, arg.Partition.PartitionId);
                }
                catch (OperationCanceledException) { }
                finally {
                    _logger.LogInformation("Processing of an event took: {elapsed}", sw.Elapsed);
                }
            }

            private bool CheckRestartAnnouncement(JObject jsonObj) {
                try {
                    if (!jsonObj.TryGetValue("MessageType", out var messageType)) {
                        return false;
                    }
                    if (!jsonObj.TryGetValue("MessageVersion", out var messageVersion)) {
                        return false;
                    }

                    if (messageType.Value<string>() == "RestartAnnouncement" && messageVersion.Value<int>() == 1) {
                        _restartAnnouncementReceived = true;
                        return true;
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error during deserialization of restart announcement.");
                }
                return false;
            }

            /// <summary>
            /// Feed the checkers for the Value Change (single Node value) within the received event
            /// </summary>
            /// <param name="nodeId">Identifeir of the data source.</param>
            /// <param name="sourceTimestamp">Timestamp at the Data Source.</param>
            /// <param name="enqueuedTimestamp">IoT Hub message enqueue timestamp.</param>
            /// <param name="receivedTimestamp">Timestamp of arrival in the telemetry processor.</param>
            /// <param name="value">The actual value of the data change.</param>
            private void FeedDataCheckers(
                string nodeId,
                DateTime sourceTimestamp,
                DateTime enqueuedTimestamp,
                DateTime receivedTimestamp,
                JToken value) {

                // OPC PLC contains bad fast and slow nodes that drop messages by design.
                // We will ignore entries that do not have a value.
                if (value is null || value.Type == JTokenType.Null) {
                    return;
                }

                var counter = Interlocked.Increment(ref _totalValueChangesCount);
                _logger.LogDebug("[{SourceTime}, {Enqueued}]{Count} {Node}='{Value}'", counter,
                    sourceTimestamp, enqueuedTimestamp, nodeId, value.ToString());

                // Feed data to checkers.
                _missingTimestampsChecker.ProcessEvent(nodeId, sourceTimestamp, value);
                _messageProcessingDelayChecker.ProcessEvent(nodeId, sourceTimestamp, receivedTimestamp);
                _messageDeliveryDelayChecker.ProcessEvent(nodeId, sourceTimestamp, enqueuedTimestamp);
                _valueChangeCounterPerNodeId.ProcessEvent(nodeId, sourceTimestamp, value);
                _missingValueChangesChecker.ProcessEvent(sourceTimestamp);
                _incrementalIntValueChecker.ProcessEvent(nodeId, sourceTimestamp, value);
            }

            /// <summary>
            /// Feed the checkers for the Data Change (one or more groupped node values) within the reveived event
            /// </summary>
            /// <param name="sequenceNumber">The actual sequence number of the data change</param>
            private void FeedDataChangeCheckers(string dataSetWriterId, uint? sequenceNumber) {
                if (!sequenceNumber.HasValue) {
                    _logger.LogWarning("Sequance number is null");
                    return;
                }
                _incrementalSequenceChecker.ProcessEvent(dataSetWriterId, sequenceNumber);
            }
        }
    }
}
