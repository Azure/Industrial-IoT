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
            //
            // The early-heartbeat check compares the heartbeat's MessageTimestamp
            // against the preceding value's SourceTimestamp, not its
            // MessageTimestamp. The SourceTimestamp is set by the OPC UA server at
            // the instant the value was produced; in an in-process test the server
            // and publisher share the same clock, so SourceTimestamp ≈ T_recv —
            // the exact instant the publisher armed the heartbeat timer. Using it
            // as the reference eliminates the batching-delay bias: the WriterGroup
            // stamps MessageTimestamps at batch time (up to one publishing interval
            // after receipt), which made the old MessageTimestamp-based gap appear
            // shorter than the real elapsed time and produced false positives.
            // The heartbeat's MessageTimestamp is still a WriterGroup batch time,
            // so the measured gap is EffectiveDeadline + hb_skew (hb_skew ≥ 0),
            // which is never less than EffectiveDeadline. A genuine defect
            // (grace period removed) shrinks EffectiveDeadline from
            // heartbeatInterval + grace to heartbeatInterval, making the measured
            // gap roughly heartbeatInterval + hb_skew, which falls below the
            // threshold for roughly half of all heartbeats and is therefore
            // reliably detectable over a multi-second run.
        }

        /// <summary>
        /// Enumerate the counter samples a message carries, accepting either a
        /// PubSub network message or a bare data set message.
        /// </summary>
        /// <remarks>
        /// Exposed so tests that analyse samples differently - comparing them
        /// against what the server recorded stamping, rather than scoring
        /// stream quality - decode the wire the same way this validator does.
        /// The decoding is not obvious: a field is a bare number under raw
        /// encoding, an object under DataValue encoding, wrapped once more in a
        /// Type/Body variant under reversible encoding, and a 64 bit integer
        /// arrives as a JSON string because its range exceeds an IEEE-754
        /// double. A second, independent parser would get some of that wrong.
        /// </remarks>
        /// <param name="message"></param>
        public static IEnumerable<(string NodeId, long Value, DateTime? SourceTimestamp)>
            ReadCounterSamples(JsonElement message)
        {
            if (message.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }
            if (message.TryGetProperty("Messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var dataSetMessage in messages.EnumerateArray())
                {
                    foreach (var sample in ReadDataSetSamples(dataSetMessage))
                    {
                        yield return sample;
                    }
                }
                yield break;
            }
            foreach (var sample in ReadDataSetSamples(message))
            {
                yield return sample;
            }
        }

        private static IEnumerable<(string, long, DateTime?)> ReadDataSetSamples(
            JsonElement dataSetMessage)
        {
            if (dataSetMessage.ValueKind != JsonValueKind.Object ||
                !dataSetMessage.TryGetProperty("Payload", out var payload) ||
                payload.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }
            foreach (var field in payload.EnumerateObject())
            {
                if (TryReadDataValue(field.Value, out var value, out var sourceTimestamp))
                {
                    yield return (field.Name, value, sourceTimestamp);
                }
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

            if (!_nodes.TryGetValue(sample.NodeId, out var state))
            {
                _nodes.Add(sample.NodeId, state = new NodeState());
            }

            //
            // Key-frame messages republish a snapshot of all current field values
            // unconditionally. A key-frame that repeats both the counter value and
            // the SourceTimestamp of the last observed sample for this node is a
            // snapshot confirmation, not a new event. Skip it so it does not affect
            // gap, ordering, or heartbeat metrics.
            //
            // When MonotonicSource is active, matching the SourceTimestamp is not
            // required: for a strictly increasing source the same value always
            // denotes the same logical sample regardless of timestamp. A WatchdogLKV
            // heartbeat may have already advanced state.LastSampleTimestamp away from
            // the server's original timestamp, so the timestamp comparison would fail
            // to suppress the key-frame even though it carries no new information.
            //
            if (sample.IsKeyFrame && state.HasSample
                && sample.Value == state.LastSampleValue
                && (sample.SourceTimestamp == state.LastSampleTimestamp
                    || _options.MonotonicSource))
            {
                return;
            }

            _totalSamples++;

            // Infer whether this sample is a heartbeat even when the wire indicator
            // (sample.IsHeartbeat) is absent. The wire indicator is authoritative when
            // present, preserving correct behaviour for 2.9-shaped messages. For
            // 3.0-shaped messages, which are published through the standards-compliant
            // OPC UA Part 14 encoder and carry no Heartbeat member, a heartbeat is
            // detected structurally: a WatchdogLKV heartbeat resends the last known
            // value unchanged with its original SourceTimestamp.
            //
            // This comparison must precede the state update below; after the update,
            // state.LastSampleValue and state.LastSampleTimestamp reflect the current
            // sample and the comparison would always be true for consecutive samples.
            //
            // Two inference paths are available:
            //
            // Conservative (default, MonotonicSource = false):
            //   Both the value AND the SourceTimestamp must match the previous sample.
            //   This is correct for any source, but cannot detect
            //   HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps heartbeats, which
            //   advance the SourceTimestamp on each resend. Those heartbeats will be
            //   counted as UnflaggedRepeats unless the wire indicator is present.
            //
            // Monotonic (MonotonicSource = true):
            //   A repeated value alone is sufficient evidence of a heartbeat,
            //   regardless of what the SourceTimestamp does. This stronger rule is
            //   sound only for a strictly increasing source: a genuinely new sample
            //   would always carry a strictly higher value, so a repeated value cannot
            //   be a real data update. It correctly identifies both WatchdogLKV and
            //   WatchdogLKVWithUpdatedTimestamps heartbeats.
            //
            // Key-frame repeats with the same value and source timestamp are silently
            // dropped above before reaching this point, so structural inference here
            // only fires for delta-frame repeats, which are genuine heartbeat resends.
            //
            var isHeartbeat = sample.IsHeartbeat
                || (state.HasSample
                    && sample.Value == state.LastSampleValue
                    && (sample.SourceTimestamp == state.LastSampleTimestamp
                        || _options.MonotonicSource));

            if (isHeartbeat)
            {
                _heartbeatSamples++;
            }
            else
            {
                _valueSamples++;
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
                        $"heartbeat={isHeartbeat})");
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
                    if (!isHeartbeat)
                    {
                        _outOfOrderValues++;
                    }
                    AddExample(
                        $"{sample.NodeId}: value went backwards {state.LastSampleValue} -> " +
                        $"{sample.Value} (heartbeat={isHeartbeat})");
                }
                else if (sample.Value == state.LastSampleValue)
                {
                    _repeatedValues++;
                    if (isHeartbeat)
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

            if (isHeartbeat)
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
                //
                // The value interval check requires a reliable SourceTimestamp
                // baseline. The very first real sample a node delivers in an
                // analysis window cannot be confirmed as a genuine value versus
                // a WatchdogLKVWithUpdatedTimestamps heartbeat that was treated
                // as real because state.HasSample was still false: such a
                // heartbeat carries a publisher-shifted SourceTimestamp, not the
                // original server-stamped one. Using a shifted timestamp as the
                // baseline causes a false violation for the very next real value.
                //
                // state.HasTwoValues is set after the second real value has been
                // processed, so the interval check only runs from the second
                // transition onward — at which point the baseline (set by the
                // second real value) is guaranteed to come from a sample whose
                // state.HasSample was already true and could therefore be inferred
                // as a heartbeat if applicable.
                //
                if (state.HasTwoValues && state.LastValueTimestamp != null
                    && sample.SourceTimestamp != null)
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
                if (state.HasValue)
                {
                    // After the second real value we have a reliable baseline.
                    state.HasTwoValues = true;
                }
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

            if (_earliestHeartbeat != null && state.LastValueTimestamp != null)
            {
                // Use the value's SourceTimestamp as the reference rather than its
                // MessageTimestamp. The SourceTimestamp reflects when the server
                // produced the value (≈ T_recv in the in-process soak tests), so
                // sinceValue ≈ EffectiveDeadline + hb_skew (always ≥ EffectiveDeadline).
                // The MessageTimestamp-based reference carried a val_queue_delay bias
                // that could make a legitimate heartbeat appear early and, when
                // compensated by widening the tolerance, made real defects invisible.
                var sinceValue = sample.MessageTimestamp.Value -
                    state.LastValueTimestamp.Value;
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
            // Key-frame messages republish all current field values regardless
            // of whether they changed. A key-frame that repeats the last known
            // value is a snapshot, not a WatchdogLKV heartbeat, so structural
            // heartbeat inference must be suppressed for those samples.
            //
            var isKeyFrame = dataSetMessage.TryGetProperty("MessageType", out var mt) &&
                mt.ValueKind == JsonValueKind.String &&
                mt.GetString() == "ua-keyframe";
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
                        timestamp, sequenceNumber, isKeyFrame));
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
            if (inner.ValueKind == JsonValueKind.Number)
            {
                return inner.TryGetInt64(out value);
            }
            //
            // The 3.0 native PubSub stack encodes UInt64 (and Int64) as JSON
            // strings because their range exceeds the IEEE-754 double that
            // JSON numbers normally carry. Accept a string representation of
            // any integer the counter can produce.
            //
            if (inner.ValueKind == JsonValueKind.String)
            {
                var s = inner.GetString();
                if (long.TryParse(s, out value))
                {
                    return true;
                }
                // Also accept unsigned values that fit a signed long.
                if (ulong.TryParse(s, out var u) && u <= (ulong)long.MaxValue)
                {
                    value = (long)u;
                    return true;
                }
            }
            return false;
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
            /// <summary>
            /// True once this node has delivered at least two real values so
            /// that <see cref="LastValueTimestamp"/> is a reliable interval
            /// baseline (set by the second real value, which was processed
            /// when <see cref="HasSample"/> was already true and heartbeat
            /// inference was therefore active).
            /// </summary>
            public bool HasTwoValues { get; set; }
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
