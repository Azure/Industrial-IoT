// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// <para>
    /// Analyses the telemetry produced for a server whose variables count up
    /// from zero by exactly one every update interval, so the value carries
    /// its own ordering and the expected source timestamp distance between
    /// two values <c>n</c> and <c>m</c> is exactly
    /// <c>(m - n) * updateInterval</c>.
    /// </para>
    /// <para>
    /// The validator is fed one sample at a time and only keeps per node
    /// state plus a bounded duplicate detection window, so it can run for
    /// hours without accumulating memory.
    /// </para>
    /// <para>
    /// It is shared by the in process soak tests and the IoT Edge end to end
    /// soak tests, which is why it has no dependency beyond
    /// <see cref="System.Text.Json"/>.
    /// </para>
    /// </summary>
    public sealed class TelemetryQualityValidator
    {
        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="options"></param>
        public TelemetryQualityValidator(TelemetryQualityOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
            _tolerance = options.Tolerance
                ?? TimeSpan.FromTicks(options.UpdateInterval.Ticks / 10);
            _heartbeatTolerance = options.HeartbeatTolerance
                ?? TimeSpan.FromTicks((options.HeartbeatInterval ?? TimeSpan.Zero).Ticks / 2);
            //
            // Earliest instant at which a watchdog heartbeat may legitimately
            // be emitted after a value was received. Values can only be
            // delivered on publish boundaries, so the absence of data cannot
            // be established earlier than one publishing interval after the
            // heartbeat interval expired. The grace is capped at the
            // heartbeat interval itself, mirroring the publisher.
            //
            if (options.HeartbeatInterval.HasValue)
            {
                var heartbeatInterval = options.HeartbeatInterval.Value;
                var publishingInterval = options.PublishingInterval ?? TimeSpan.Zero;
                var grace = publishingInterval > heartbeatInterval
                    ? heartbeatInterval : publishingInterval;
                _earliestHeartbeat = heartbeatInterval + grace;
            }
        }

        /// <summary>
        /// Add a sample to the analysis.
        /// </summary>
        /// <param name="sample"></param>
        public void Add(TelemetrySample sample)
        {
            if (IsDuplicate(sample))
            {
                _duplicateDeliveries++;
                return;
            }

            _totalSamples++;
            if (sample.IsHeartbeat)
            {
                _heartbeatSamples++;
            }
            else
            {
                _valueSamples++;
            }

            if (!_nodes.TryGetValue(sample.NodeId, out var state))
            {
                _nodes.Add(sample.NodeId, state = new NodeState());
            }

            if (sample.SourceTimestamp == null)
            {
                _samplesWithoutSourceTimestamp++;
            }

            //
            // Distance between the source timestamps of two consecutive
            // samples, which is what a consumer that does not look at the
            // heartbeat flag observes. A repeated value has a distance of
            // zero and therefore shows up here.
            //
            if (state.LastSampleTimestamp != null && sample.SourceTimestamp != null)
            {
                var delta = sample.SourceTimestamp.Value - state.LastSampleTimestamp.Value;
                if (!IsExpectedDistance(delta, 1))
                {
                    _messageIntervalViolations++;
                    AddExample(
                        $"{sample.NodeId}: message source timestamp distance {delta} " +
                        $"(value {state.LastSampleValue} -> {sample.Value}, " +
                        $"heartbeat={sample.IsHeartbeat})");
                }
                //
                // A source timestamp that moves backwards is symptom (d) as
                // a consumer sees it when it cannot evaluate the heartbeat
                // indicator: an "old" message arriving after a newer one.
                // Distinct from the value based ordering check below, which
                // a repeated value cannot trip.
                //
                if (delta < TimeSpan.Zero)
                {
                    _sourceTimestampRegressions++;
                    AddExample(
                        $"{sample.NodeId}: source timestamp went backwards by {-delta} " +
                        $"(value {state.LastSampleValue} -> {sample.Value}, " +
                        $"heartbeat={sample.IsHeartbeat})");
                }
            }

            if (state.HasSample)
            {
                if (sample.Value < state.LastSampleValue)
                {
                    _outOfOrderIncludingHeartbeats++;
                    if (!sample.IsHeartbeat)
                    {
                        _outOfOrderValues++;
                    }
                    AddExample(
                        $"{sample.NodeId}: value went backwards {state.LastSampleValue} -> " +
                        $"{sample.Value} (heartbeat={sample.IsHeartbeat})");
                }
                else if (sample.Value == state.LastSampleValue)
                {
                    _repeatedValues++;
                    if (sample.IsHeartbeat)
                    {
                        _repeatedValuesFromHeartbeat++;
                    }
                    else
                    {
                        AddExample(
                            $"{sample.NodeId}: value {sample.Value} repeated without the " +
                            "heartbeat indicator");
                    }
                }
            }

            state.HasSample = true;
            state.LastSampleValue = sample.Value;
            state.LastSampleTimestamp = sample.SourceTimestamp;

            if (sample.IsHeartbeat)
            {
                AnalyzeHeartbeat(sample, state);
                return;
            }

            //
            // Gaps and timestamp distances over real value changes. Heartbeats
            // repeat a value that was already accounted for and must not be
            // treated as a new value.
            //
            if (state.HasValue && sample.Value > state.LastValue)
            {
                var gap = sample.Value - state.LastValue - 1;
                if (gap > 0)
                {
                    _missingValues += gap;
                    AddExample(
                        $"{sample.NodeId}: {gap} value(s) missing between " +
                        $"{state.LastValue} and {sample.Value}");
                }
                if (state.LastValueTimestamp != null && sample.SourceTimestamp != null)
                {
                    var delta = sample.SourceTimestamp.Value - state.LastValueTimestamp.Value;
                    if (!IsExpectedDistance(delta, sample.Value - state.LastValue))
                    {
                        _valueIntervalViolations++;
                        AddExample(
                            $"{sample.NodeId}: value source timestamp distance {delta} " +
                            $"for {sample.Value - state.LastValue} increment(s) " +
                            $"({state.LastValue} -> {sample.Value})");
                    }
                }
            }

            if (!state.HasValue || sample.Value > state.LastValue)
            {
                state.HasValue = true;
                state.LastValue = sample.Value;
                state.LastValueTimestamp = sample.SourceTimestamp;
                state.LastValueMessageTimestamp = sample.MessageTimestamp;
            }
            state.LastHeartbeatMessageTimestamp = null;
        }

        /// <summary>
        /// Extract and add every sample carried by a samples mode
        /// (monitored item) message.
        /// </summary>
        /// <param name="message"></param>
        public void AddSamplesMessage(JsonElement message)
        {
            if (message.ValueKind != JsonValueKind.Object ||
                !message.TryGetProperty("NodeId", out var nodeId) ||
                nodeId.ValueKind != JsonValueKind.String)
            {
                return;
            }
            if (!message.TryGetProperty("Value", out var value))
            {
                return;
            }
            if (!TryReadDataValue(value, out var counter, out var sourceTimestamp))
            {
                return;
            }
            Add(new TelemetrySample(nodeId.GetString()!, counter, sourceTimestamp,
                IsHeartbeat(message), ReadTimestamp(message), ReadSequenceNumber(message)));
        }

        /// <summary>
        /// Extract and add every sample carried by a PubSub network message
        /// or a single data set message.
        /// </summary>
        /// <param name="message"></param>
        public void AddPubSubMessage(JsonElement message)
        {
            if (message.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            if (message.TryGetProperty("Messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var dataSetMessage in messages.EnumerateArray())
                {
                    AddDataSetMessage(dataSetMessage);
                }
                return;
            }
            AddDataSetMessage(message);
        }

        /// <summary>
        /// Build the report
        /// </summary>
        public TelemetryQualityReport CreateReport()
        {
            var minHeartbeats = 0L;
            var maxHeartbeats = 0L;
            if (_nodes.Count > 0)
            {
                minHeartbeats = _nodes.Values.Min(n => n.Heartbeats);
                maxHeartbeats = _nodes.Values.Max(n => n.Heartbeats);
            }
            return new TelemetryQualityReport
            {
                TotalSamples = _totalSamples,
                ValueSamples = _valueSamples,
                HeartbeatSamples = _heartbeatSamples,
                NodesSeen = _nodes.Count,
                NodesMissing = Math.Max(0, _options.ExpectedNodeCount - _nodes.Count),
                MissingValues = _missingValues,
                OutOfOrderValues = _outOfOrderValues,
                OutOfOrderIncludingHeartbeats = _outOfOrderIncludingHeartbeats,
                RepeatedValues = _repeatedValues,
                RepeatedValuesFromHeartbeat = _repeatedValuesFromHeartbeat,
                ValueIntervalViolations = _valueIntervalViolations,
                MessageIntervalViolations = _messageIntervalViolations,
                SourceTimestampRegressions = _sourceTimestampRegressions,
                SamplesWithoutSourceTimestamp = _samplesWithoutSourceTimestamp,
                HeartbeatsWithChangedTimestamp = _heartbeatsWithChangedTimestamp,
                EarlyHeartbeats = _earlyHeartbeats,
                HeartbeatCadenceViolations = _heartbeatCadenceViolations,
                MinHeartbeatsPerNode = minHeartbeats,
                MaxHeartbeatsPerNode = maxHeartbeats,
                DuplicateDeliveries = _duplicateDeliveries,
                Examples = _examples.ToList()
            };
        }

        /// <summary>
        /// Analyse a heartbeat: it must repeat the source timestamp of the
        /// value it resends, it must not arrive before the watchdog grace
        /// period elapsed, and consecutive heartbeats must be one heartbeat
        /// interval apart.
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="state"></param>
        private void AnalyzeHeartbeat(TelemetrySample sample, NodeState state)
        {
            state.Heartbeats++;

            if (state.HasValue && state.LastValueTimestamp != null &&
                sample.SourceTimestamp != null &&
                sample.SourceTimestamp != state.LastValueTimestamp)
            {
                _heartbeatsWithChangedTimestamp++;
                AddExample(
                    $"{sample.NodeId}: heartbeat source timestamp {sample.SourceTimestamp} " +
                    $"differs from the value it resends ({state.LastValueTimestamp})");
            }

            if (sample.MessageTimestamp == null)
            {
                return;
            }

            if (_earliestHeartbeat != null && state.LastValueMessageTimestamp != null)
            {
                var sinceValue = sample.MessageTimestamp.Value -
                    state.LastValueMessageTimestamp.Value;
                if (sinceValue + _heartbeatTolerance < _earliestHeartbeat.Value)
                {
                    _earlyHeartbeats++;
                    AddExample(
                        $"{sample.NodeId}: heartbeat {sinceValue} after the last value, " +
                        $"earlier than the {_earliestHeartbeat.Value} watchdog grace period");
                }
            }

            if (_options.HeartbeatInterval != null &&
                state.LastHeartbeatMessageTimestamp != null)
            {
                var sinceHeartbeat = sample.MessageTimestamp.Value -
                    state.LastHeartbeatMessageTimestamp.Value;
                if ((sinceHeartbeat - _options.HeartbeatInterval.Value).Duration() >
                    _heartbeatTolerance)
                {
                    _heartbeatCadenceViolations++;
                    AddExample(
                        $"{sample.NodeId}: {sinceHeartbeat} between heartbeats, expected " +
                        $"{_options.HeartbeatInterval.Value}");
                }
            }

            state.LastHeartbeatMessageTimestamp = sample.MessageTimestamp;
        }

        /// <summary>
        /// <para>
        /// Whether the sample was already seen. Delivery is at least once, so
        /// a redelivered message must not be reported as a repeated value.
        /// </para>
        /// <para>
        /// The key must include the node. A writer sequence number identifies
        /// a notification within its writer, not within the whole stream, so
        /// keying on the number alone makes the samples of different nodes
        /// collide - with thousands of nodes that silently discards most of
        /// the stream instead of just the redeliveries.
        /// </para>
        /// <para>
        /// Heartbeats are never suppressed. A heartbeat resends the last
        /// known value <em>including its sequence number</em>, so it collides
        /// with the value it repeats by construction, and suppressing it
        /// would hide the very thing these tests measure. The cost is that a
        /// genuinely redelivered heartbeat is counted twice, which errs
        /// towards over reporting rather than towards hiding a defect.
        /// </para>
        /// </summary>
        /// <param name="sample"></param>
        private bool IsDuplicate(TelemetrySample sample)
        {
            if (!_options.SuppressDuplicates || sample.SequenceNumber == null ||
                sample.IsHeartbeat)
            {
                return false;
            }
            var key = (sample.NodeId, sample.SequenceNumber.Value);
            if (!_seenSequenceNumbers.Add(key))
            {
                return true;
            }
            _seenSequenceNumberOrder.Enqueue(key);
            while (_seenSequenceNumberOrder.Count > _options.DuplicateWindow)
            {
                _seenSequenceNumbers.Remove(_seenSequenceNumberOrder.Dequeue());
            }
            return false;
        }

        /// <summary>
        /// Add a single PubSub data set message
        /// </summary>
        /// <param name="dataSetMessage"></param>
        private void AddDataSetMessage(JsonElement dataSetMessage)
        {
            if (dataSetMessage.ValueKind != JsonValueKind.Object ||
                !dataSetMessage.TryGetProperty("Payload", out var payload) ||
                payload.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            var heartbeat = IsHeartbeat(dataSetMessage);
            var timestamp = ReadTimestamp(dataSetMessage);
            //
            // A data set message carries one sequence number for all of its
            // fields, so it can only be used to deduplicate when the data set
            // holds a single field.
            //
            var fieldCount = payload.EnumerateObject().Count();
            var sequenceNumber = fieldCount == 1 ? ReadSequenceNumber(dataSetMessage) : null;
            foreach (var field in payload.EnumerateObject())
            {
                if (TryReadDataValue(field.Value, out var counter, out var sourceTimestamp))
                {
                    Add(new TelemetrySample(field.Name, counter, sourceTimestamp, heartbeat,
                        timestamp, sequenceNumber));
                }
            }
        }

        /// <summary>
        /// Read a counter value and its source timestamp from an encoded
        /// data value. Depending on the configured field content mask the
        /// value is either a bare number or an object wrapping it.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        /// <param name="sourceTimestamp"></param>
        private static bool TryReadDataValue(JsonElement element, out long value,
            out DateTime? sourceTimestamp)
        {
            value = 0;
            sourceTimestamp = null;
            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.TryGetInt64(out value);
            }
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }
            if (element.TryGetProperty("SourceTimestamp", out var timestamp) &&
                timestamp.ValueKind == JsonValueKind.String &&
                timestamp.TryGetDateTime(out var parsed))
            {
                sourceTimestamp = parsed.ToUniversalTime();
            }
            if (!element.TryGetProperty("Value", out var inner))
            {
                return false;
            }
            //
            // Reversible encoding wraps the value once more into a
            // { "Type": .., "Body": .. } variant envelope.
            //
            if (inner.ValueKind == JsonValueKind.Object &&
                inner.TryGetProperty("Body", out var body))
            {
                inner = body;
            }
            return inner.ValueKind == JsonValueKind.Number && inner.TryGetInt64(out value);
        }

        /// <summary>
        /// Whether the message carries the heartbeat indicator
        /// </summary>
        /// <param name="message"></param>
        private static bool IsHeartbeat(JsonElement message)
        {
            return message.TryGetProperty("Heartbeat", out var heartbeat) &&
                heartbeat.ValueKind == JsonValueKind.True;
        }

        /// <summary>
        /// Read the time at which the notification was produced
        /// </summary>
        /// <param name="message"></param>
        private static DateTime? ReadTimestamp(JsonElement message)
        {
            return message.TryGetProperty("Timestamp", out var timestamp) &&
                timestamp.ValueKind == JsonValueKind.String &&
                timestamp.TryGetDateTime(out var parsed) ? parsed.ToUniversalTime() : null;
        }

        /// <summary>
        /// Read the writer sequence number
        /// </summary>
        /// <param name="message"></param>
        private static uint? ReadSequenceNumber(JsonElement message)
        {
            return message.TryGetProperty("SequenceNumber", out var sequenceNumber) &&
                sequenceNumber.ValueKind == JsonValueKind.Number &&
                sequenceNumber.TryGetUInt32(out var parsed) ? parsed : null;
        }

        /// <summary>
        /// Whether the observed distance matches the expected number of
        /// update intervals within tolerance.
        /// </summary>
        /// <param name="observed"></param>
        /// <param name="increments"></param>
        private bool IsExpectedDistance(TimeSpan observed, long increments)
        {
            var expected = TimeSpan.FromTicks(_options.UpdateInterval.Ticks * increments);
            return (observed - expected).Duration() <= _tolerance;
        }

        /// <summary>
        /// Record an example observation, bounded so a broken stream cannot
        /// exhaust memory.
        /// </summary>
        /// <param name="example"></param>
        private void AddExample(string example)
        {
            if (_examples.Count < _options.MaxExamples)
            {
                _examples.Add(example);
            }
        }

        /// <summary>
        /// Per node analysis state
        /// </summary>
        private sealed class NodeState
        {
            public bool HasSample { get; set; }
            public long LastSampleValue { get; set; }
            public DateTime? LastSampleTimestamp { get; set; }
            public bool HasValue { get; set; }
            public long LastValue { get; set; }
            public DateTime? LastValueTimestamp { get; set; }
            public DateTime? LastValueMessageTimestamp { get; set; }
            public DateTime? LastHeartbeatMessageTimestamp { get; set; }
            public long Heartbeats { get; set; }
        }

        private readonly TelemetryQualityOptions _options;
        private readonly Dictionary<string, NodeState> _nodes = [];
        private readonly List<string> _examples = [];
        private readonly HashSet<(string, uint)> _seenSequenceNumbers = [];
        private readonly Queue<(string, uint)> _seenSequenceNumberOrder = new();
        private readonly TimeSpan _tolerance;
        private readonly TimeSpan _heartbeatTolerance;
        private readonly TimeSpan? _earliestHeartbeat;
        private long _totalSamples;
        private long _valueSamples;
        private long _heartbeatSamples;
        private long _missingValues;
        private long _outOfOrderValues;
        private long _outOfOrderIncludingHeartbeats;
        private long _repeatedValues;
        private long _repeatedValuesFromHeartbeat;
        private long _valueIntervalViolations;
        private long _messageIntervalViolations;
        private long _sourceTimestampRegressions;
        private long _samplesWithoutSourceTimestamp;
        private long _heartbeatsWithChangedTimestamp;
        private long _earlyHeartbeats;
        private long _heartbeatCadenceViolations;
        private long _duplicateDeliveries;
    }
}
