// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.TestEventProcessor
{
    using IIoTPlatformE2ETests.TestEventProcessor.Checkers;
    using IIoTPlatformE2ETests;
    using IIoTPlatformE2ETests.TestExtensions;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
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

    public sealed class TelemetryValidator : IDisposable
    {
        /// <summary>
        /// Create instance of TelemetryValidator
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="logger">Instance to write logs</param>
        private TelemetryValidator(IIoTPlatformTestContext context,
            ValidatorConfiguration configuration, ILogger<TelemetryValidator> logger)
        {
            // Check provided configuration.
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;

            if (configuration.ThresholdValue <= 0)
            {
                throw new ArgumentException("Invalid configuration detected, threshold can't be negative or zero");
            }
            if (configuration.ExpectedMaximalDuration == 0)
            {
                configuration.ExpectedMaximalDuration = uint.MaxValue;
            }

            _configuration = configuration;
            _client = context.GetEventHubConsumerClient();
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize checkers.
            _missingTimestampsChecker = new MissingTimestampsChecker(
                TimeSpan.FromMilliseconds(_configuration.ExpectedIntervalOfValueChanges),
                TimeSpan.FromMilliseconds(_configuration.ThresholdValue),
                _logger
            );
            _missingTimestampsChecker.StartAsync(
                TimeSpan.FromMilliseconds(kCheckerDelayMilliseconds),
                _cancellationTokenSource.Token
            ).Start();

            _messageProcessingDelayChecker = new MessageProcessingDelayChecker(
                TimeSpan.FromMilliseconds(_configuration.ThresholdValue),
                _logger
            );

            _messageDeliveryDelayChecker = new MessageDeliveryDelayChecker(
                TimeSpan.FromMilliseconds(_configuration.ExpectedMaximalDuration),
                _logger
            );

            _valueChangeCounterPerNodeId = new ValueChangeCounterPerNodeId();

            _missingValueChangesChecker = new MissingValueChangesChecker(
                _configuration.ExpectedValueChangesPerTimestamp,
                _logger
            );
            _missingValueChangesChecker.StartAsync(
                TimeSpan.FromMilliseconds(kCheckerDelayMilliseconds),
                _cancellationTokenSource.Token
            ).Start();

            _incrementalIntValueChecker = new IncrementalIntValueChecker(_logger);
            _incrementalSequenceChecker = new SequenceNumberChecker(_logger);

            _startTime = DateTime.UtcNow;
            _runner = RunAsync(_cancellationTokenSource.Token);

            async Task RunAsync(CancellationToken ct)
            {
                try
                {
                    await foreach (var evt in _client.ReadEventsAsync(false, cancellationToken: ct))
                    {
                        Client_ProcessEvent(evt);
                    }
                }
                catch (OperationCanceledException) { }
            }
        }

        /// <summary>
        /// Starts monitoring the incoming messages of the IoT Hub and checks for missing values.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="expectedValuesChangesPerTimestamp">The expected number of value changes per timestamp</param>
        /// <param name="expectedIntervalOfValueChanges">The expected time difference between values changes in milliseconds</param>
        /// <param name="expectedMaximalDuration">The time difference between OPC UA Server fires event until Changes Received in IoT Hub in milliseconds </param>
        /// <returns></returns>
        public static TelemetryValidator Start(IIoTPlatformTestContext context,
            uint expectedValuesChangesPerTimestamp, uint expectedIntervalOfValueChanges,
            uint expectedMaximalDuration)
        {
            var configuration = new ValidatorConfiguration
            {
                ExpectedValueChangesPerTimestamp = expectedValuesChangesPerTimestamp,
                ExpectedIntervalOfValueChanges = expectedIntervalOfValueChanges,
                ThresholdValue = expectedIntervalOfValueChanges > 0
                        ? expectedIntervalOfValueChanges / 10
                        : 100u,
                ExpectedMaximalDuration = expectedMaximalDuration
            };
            return new TelemetryValidator(context, configuration, context.CreateLogger<TelemetryValidator>());
        }

        /// <summary>
        /// Stop the monitoring and disposes all related resources..
        /// </summary>
        /// <returns></returns>
        public async Task<ValidationResult> StopAsync()
        {
            var endTime = DateTime.UtcNow;

            await _cancellationTokenSource.CancelAsync();
            await _runner.ConfigureAwait(false);

            // Stop checkers and collect resutls.
            var missingTimestampsCounter = _missingTimestampsChecker.Stop();
            var maxMessageProcessingDelay = _messageProcessingDelayChecker.Stop();
            var maxMessageDeliveryDelay = _messageDeliveryDelayChecker.Stop();
            var valueChangesPerNodeId = _valueChangeCounterPerNodeId.Stop();
            var allExpectedValueChanges = true;
            if (_configuration?.ExpectedValueChangesPerTimestamp > 0)
            {
                // TODO collect "expected" parameter as groups related to OPC UA nodes
                allExpectedValueChanges = valueChangesPerNodeId
                    .All(kvp => (_totalValueChangesCount / kvp.Value) ==
                        _configuration.ExpectedValueChangesPerTimestamp);
                _logger.LogInformation("All expected value changes received: {AllExpectedValueChanges}",
                    allExpectedValueChanges);
            }

            var incompleteTimestamps = _missingValueChangesChecker.Stop();
            var incrCheckerResult = _incrementalIntValueChecker.Stop();
            var incrSequenceResult = _incrementalSequenceChecker.Stop();

            return new ValidationResult
            {
                ValueChangesByNodeId = new ReadOnlyDictionary<string, int>(valueChangesPerNodeId ?? new Dictionary<string, int>()),
                AllExpectedValueChanges = allExpectedValueChanges,
                TotalValueChangesCount = _totalValueChangesCount,
                AllInExpectedInterval = missingTimestampsCounter == 0,
                StartTime = _startTime,
                EndTime = endTime,
                MaxDelayToNow = maxMessageProcessingDelay,
                MaxDeliveryDuration = maxMessageDeliveryDelay,
                DroppedValueCount = incrCheckerResult.DroppedValueCount,
                DuplicateValueCount = incrCheckerResult.DuplicateValueCount,
                DroppedSequenceCount = incrSequenceResult.DroppedValueCount,
                DuplicateSequenceCount = incrSequenceResult.DuplicateValueCount,
                ResetSequenceCount = incrSequenceResult.ResetsValueCount,
                RestartAnnouncementReceived = _restartAnnouncementReceived
            };
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            // Stop event monitoring and deregister handler.
            _cancellationTokenSource.Cancel();
            if (!_runner.IsCompleted)
            {
                _runner.GetAwaiter().GetResult();
            }
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();

            _missingTimestampsChecker.Dispose();
            _messageProcessingDelayChecker.Dispose();
            _messageDeliveryDelayChecker.Dispose();
            _valueChangeCounterPerNodeId.Dispose();
            _missingValueChangesChecker.Dispose();
            _incrementalIntValueChecker.Dispose();
            _incrementalSequenceChecker.Dispose();
            _cancellationTokenSource.Dispose();
        }

        /// <summary>
        /// Analyze payload of IoTHub message, adding timestamp and related sequence numbers into temporary
        /// </summary>
        /// <param name="partitionEvent"></param>
        /// <returns>Task that run until token is canceled</returns>
        private void Client_ProcessEvent(PartitionEvent partitionEvent)
        {
            var sw = Stopwatch.StartNew();

            var eventReceivedTimestamp = DateTime.UtcNow;

            try
            {
                if (partitionEvent.Data == null)
                {
                    _logger.LogWarning("Received partition event without content");
                    return;
                }

                var enqueuedTime = (DateTime)partitionEvent.Data.SystemProperties[MessageSystemPropertyNames.EnqueuedTime];
                if (enqueuedTime < _startTime)
                {
                    _logger.LogDebug("Received message enqueued before starting....");
                    return;
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _logger.LogDebug("Received message after stopping....");
                    return;
                }

                var properties = partitionEvent.Data.Properties;
                var hasPubSubJsonHeader = properties.TryGetValue("$$ContentType", out var schema)
                    && schema.ToString() == MessageSchemaTypes.NetworkMessageJson;

                var body = partitionEvent.Data.Body.ToArray();
                var content = Encoding.UTF8.GetString(body);
                var json = JToken.Parse(content);

                if (json is JObject o && CheckRestartAnnouncement(o))
                {
                    return;
                }

                var valueChangesCount = 0;

                if (json is JArray batch)
                {
                    foreach (var entry in batch)
                    {
                        ProcessMessage(entry);
                    }
                }
                else
                {
                    ProcessMessage(json);
                }

                void ProcessMessage(JToken messageToken)
                {
                    if (messageToken is not JObject entry)
                    {
                        return;
                    }
                    try
                    {
                        // validate if the message has an OPC UA PubSub message type signature
                        if (entry.TryGetValue("MessageType", out var messageType) && ((string)messageType) == "ua-data")
                        {
                            if (!hasPubSubJsonHeader)
                            {
                                _logger.LogInformation("Received {Message} with \"ua-data\" signature but invalid content type header",
                                    entry.ToString());
                            }

                            if (!entry.TryGetValue("Messages", out var a) || a is not JArray messages)
                            {
                                _logger.LogInformation("Received {Message} without messages.", entry.ToString());
                                return;
                            }

                            foreach (var token in messages)
                            {
                                if (token is not JObject message)
                                {
                                    _logger.LogInformation("Message {Message} is not an object.", token.ToString());
                                    continue;
                                }
                                if (!message.TryGetValue("DataSetWriterId", out var dataSetWriterId))
                                {
                                    _logger.LogInformation("Message {Message} does not contain writer id.", message.ToString());
                                    continue;
                                }
                                if (!message.TryGetValue("SequenceNumber", out var sequenceNumber))
                                {
                                    _logger.LogInformation("Message {Message} does not contain sequence number.", message.ToString());
                                    sequenceNumber = JValue.CreateNull();
                                }

                                FeedDataChangeCheckers((string)dataSetWriterId, sequenceNumber.ToObject<uint?>());

                                if (!message.TryGetValue("Payload", out var p) || p is not JObject payload)
                                {
                                    _logger.LogInformation("Message {Message} does not have any 'Payload' object.", entry.ToString());
                                    continue;
                                }
                                foreach (var property in payload.Properties())
                                {
                                    if (property.Value is not JObject dataValue)
                                    {
                                        _logger.LogInformation("Payload property {Property} does not have data value.",
                                            property.Value.ToString());
                                        continue;
                                    }
                                    if (!dataValue.TryGetValue("SourceTimestamp", out var st) || st is not JValue sourceTimeStamp)
                                    {
                                        _logger.LogInformation("Value {Value} is missing source timestamp.", dataValue.ToString());
                                        continue;
                                    }
                                    if (!dataValue.TryGetValue("Value", out var value))
                                    {
                                        value = JValue.CreateNull();
                                    }
                                    FeedDataCheckers(
                                        property.Name,
                                        sourceTimeStamp.ToObject<DateTime>(),
                                        partitionEvent.Data.EnqueuedTime.UtcDateTime,
                                        eventReceivedTimestamp,
                                        value);
                                    valueChangesCount++;
                                }
                            }
                        }
                        else if (entry.TryGetValue("NodeId", out var nodeId))
                        {
                            if (!entry.TryGetValue("Value", out var v) || v is not JObject dataValue)
                            {
                                _logger.LogInformation("Message {Message} does not have data value.", entry.ToString());
                                return;
                            }
                            if (!dataValue.TryGetValue("SourceTimestamp", out var st) || st is not JValue sourceTimeStamp)
                            {
                                _logger.LogInformation("Value {Value} is missing source timestamp.", dataValue.ToString());
                                return;
                            }
                            if (!dataValue.TryGetValue("Value", out var value))
                            {
                                value = JValue.CreateNull();
                            }
                            if (entry.TryGetValue("Status", out var status) && status.ToString() != "Good")
                            {
                                _logger.LogInformation("Value has status {Status}.", status.ToString());
                            }
                            FeedDataCheckers(
                                (string)nodeId,
                                sourceTimeStamp.ToObject<DateTime>(),
                                partitionEvent.Data.EnqueuedTime.UtcDateTime,
                                eventReceivedTimestamp,
                                value);
                            valueChangesCount++;
                        }
                        else
                        {
                            _logger.LogInformation("Message {Message} not a publisher message.", entry.ToString());
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not read sequence number, nodeId and/or timestamp from " +
                            "message. Please make sure that publisher is running with samples format and with " +
                            "--fm parameter set. Message: {Content}", content);
                    }
                }

                _logger.LogDebug("Received {NumberOfValueChanges} messages from IoT Hub, partition {PartitionId}.",
                    valueChangesCount, partitionEvent.Partition.PartitionId);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _logger.LogInformation("Processing of an event took: {Elapsed}", sw.Elapsed);
            }
        }

        private bool CheckRestartAnnouncement(JObject jsonObj)
        {
            try
            {
                if (!jsonObj.TryGetValue("MessageType", out var messageType))
                {
                    return false;
                }
                if (!jsonObj.TryGetValue("MessageVersion", out var messageVersion))
                {
                    return false;
                }

                if (messageType.Value<string>() == "RestartAnnouncement" && messageVersion.Value<int>() == 1)
                {
                    _restartAnnouncementReceived = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
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
            JToken value)
        {
            // OPC PLC contains bad fast and slow nodes that drop messages by design.
            // We will ignore entries that do not have a value.
            if (value is null || value.Type == JTokenType.Null)
            {
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
        /// <param name="dataSetWriterId"></param>
        /// <param name="sequenceNumber">The actual sequence number of the data change</param>
        private void FeedDataChangeCheckers(string dataSetWriterId, uint? sequenceNumber)
        {
            if (!sequenceNumber.HasValue)
            {
                _logger.LogWarning("Sequance number is null");
                return;
            }
            _incrementalSequenceChecker.ProcessEvent(dataSetWriterId, sequenceNumber);
        }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DateTime _startTime;
        private int _totalValueChangesCount;
        private bool _restartAnnouncementReceived;

        private readonly MissingTimestampsChecker _missingTimestampsChecker;
        private readonly MessageProcessingDelayChecker _messageProcessingDelayChecker;
        private readonly MessageDeliveryDelayChecker _messageDeliveryDelayChecker;
        private readonly ValueChangeCounterPerNodeId _valueChangeCounterPerNodeId;
        private readonly MissingValueChangesChecker _missingValueChangesChecker;
        private readonly IncrementalIntValueChecker _incrementalIntValueChecker;
        private readonly SequenceNumberChecker _incrementalSequenceChecker;

        public const int kCheckerDelayMilliseconds = 10_000;
        private readonly EventHubConsumerClient _client;
        private readonly Task _runner;
        private readonly ValidatorConfiguration _configuration;
        private readonly ILogger<TelemetryValidator> _logger;
    }
}
