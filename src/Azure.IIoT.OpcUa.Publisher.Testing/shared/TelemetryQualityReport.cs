// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// One value observed in the telemetry stream for a single counter node.
    /// </summary>
    /// <param name="NodeId">Identity of the node the value belongs to</param>
    /// <param name="Value">The counter value, which doubles as its sequence</param>
    /// <param name="SourceTimestamp">Source timestamp as reported by the server</param>
    /// <param name="IsHeartbeat">Whether the message was flagged a heartbeat</param>
    /// <param name="MessageTimestamp">
    /// Time at which the notification was produced by the publisher. A
    /// heartbeat repeats the source timestamp of the value it resends, so
    /// only this timestamp can be used to measure heartbeat cadence.
    /// </param>
    /// <param name="SequenceNumber">
    /// Writer sequence number, used to recognize a redelivered message.
    /// </param>
    public readonly record struct TelemetrySample(string NodeId, long Value,
        DateTime? SourceTimestamp, bool IsHeartbeat,
        DateTime? MessageTimestamp = null, uint? SequenceNumber = null);

    /// <summary>
    /// <para>
    /// Aggregated verdict over a telemetry stream produced by a server whose
    /// variables count up by exactly one every update interval. Because every
    /// counter value is also its own sequence number, each of the four
    /// symptoms that motivated these tests maps onto a counter that is either
    /// zero or not:
    /// </para>
    /// <list type="bullet">
    /// <item>(a) missing values -> <see cref="MissingValues"/></item>
    /// <item>(b) out of order -> <see cref="OutOfOrderValues"/></item>
    /// <item>(c) source timestamp distance -> <see cref="ValueIntervalViolations"/>
    /// for real value changes and <see cref="MessageIntervalViolations"/> for
    /// what a consumer that ignores the heartbeat flag observes</item>
    /// <item>(d) old value shortly after a new one -> <see cref="UnflaggedRepeats"/>
    /// and <see cref="OutOfOrderIncludingHeartbeats"/></item>
    /// </list>
    /// </summary>
    public sealed record class TelemetryQualityReport
    {
        /// <summary> Total number of samples that were analysed </summary>
        public long TotalSamples { get; init; }
        /// <summary> Samples that were not flagged as heartbeat </summary>
        public long ValueSamples { get; init; }
        /// <summary> Samples that were flagged as heartbeat </summary>
        public long HeartbeatSamples { get; init; }
        /// <summary> Distinct nodes that produced at least one sample </summary>
        public int NodesSeen { get; init; }
        /// <summary> Nodes that never produced a sample </summary>
        public int NodesMissing { get; init; }

        /// <summary>
        /// (a) Counter values that were never delivered, counted as the gap
        /// between two consecutive delivered values.
        /// </summary>
        public long MissingValues { get; init; }

        /// <summary>
        /// (b) Real value changes that arrived after a higher value had
        /// already been delivered for the same node.
        /// </summary>
        public long OutOfOrderValues { get; init; }

        /// <summary>
        /// (b)/(d) Same as <see cref="OutOfOrderValues"/> but counting
        /// heartbeats as well, which is what a consumer sees when it does
        /// not inspect the heartbeat flag.
        /// </summary>
        public long OutOfOrderIncludingHeartbeats { get; init; }

        /// <summary>
        /// (d) Samples that repeated the value that was delivered
        /// immediately before for the same node.
        /// </summary>
        public long RepeatedValues { get; init; }

        /// <summary>
        /// (d) Subset of <see cref="RepeatedValues"/> that carried the
        /// heartbeat flag.
        /// </summary>
        public long RepeatedValuesFromHeartbeat { get; init; }

        /// <summary>
        /// (d) Repeats that did <em>not</em> carry the heartbeat flag. A
        /// consumer that honours the flag can filter heartbeats out, so this
        /// is the number of stale values it cannot defend itself against and
        /// must always be zero.
        /// </summary>
        public long UnflaggedRepeats => RepeatedValues - RepeatedValuesFromHeartbeat;

        /// <summary>
        /// (c) Distance between the source timestamps of two consecutive
        /// real value changes that was not the expected multiple of the
        /// update interval.
        /// </summary>
        public long ValueIntervalViolations { get; init; }

        /// <summary>
        /// (c) Distance between the source timestamps of two consecutive
        /// samples, heartbeats included, that was outside tolerance. This is
        /// expected to be non zero whenever heartbeats are supposed to fire,
        /// because a heartbeat deliberately repeats a source timestamp.
        /// </summary>
        public long MessageIntervalViolations { get; init; }

        /// <summary>
        /// <para>
        /// (d) Samples whose source timestamp was <em>earlier</em> than that
        /// of the preceding sample for the same node.
        /// </para>
        /// <para>
        /// This is symptom (d) as a consumer sees it when it cannot evaluate
        /// the heartbeat indicator: an "old" message arriving shortly after a
        /// newer one. It is deliberately distinct from
        /// <see cref="OutOfOrderIncludingHeartbeats"/>, which compares the
        /// counter values and therefore cannot trip on a repeated value no
        /// matter what timestamp it carries.
        /// </para>
        /// <para>
        /// Must be zero for every heartbeat behavior. Even
        /// <c>WatchdogLKVWithUpdatedTimestamps</c>, which shifts the resent
        /// timestamp, only ever shifts it forward.
        /// </para>
        /// </summary>
        public long SourceTimestampRegressions { get; init; }

        /// <summary> Samples that carried no source timestamp at all </summary>
        public long SamplesWithoutSourceTimestamp { get; init; }

        /// <summary>
        /// Heartbeats whose source timestamp differed from the source
        /// timestamp of the value they resend. Must be zero for the
        /// <c>WatchdogLKV</c> behavior, which resends the last known value
        /// unchanged.
        /// </summary>
        public long HeartbeatsWithChangedTimestamp { get; init; }

        /// <summary>
        /// Heartbeats that arrived earlier after the preceding value than the
        /// watchdog grace period allows. A watchdog may only report a node
        /// silent once the heartbeat interval plus one publishing interval
        /// passed without data.
        /// </summary>
        public long EarlyHeartbeats { get; init; }

        /// <summary>
        /// Gaps between two consecutive heartbeats of the same node that did
        /// not match the configured heartbeat interval.
        /// </summary>
        public long HeartbeatCadenceViolations { get; init; }

        /// <summary> Lowest number of heartbeats observed on any node </summary>
        public long MinHeartbeatsPerNode { get; init; }

        /// <summary> Highest number of heartbeats observed on any node </summary>
        public long MaxHeartbeatsPerNode { get; init; }

        /// <summary>
        /// Samples that were ignored because their writer sequence number had
        /// already been seen, i.e. the message was redelivered by the
        /// transport. Diagnostic only.
        /// </summary>
        public long DuplicateDeliveries { get; init; }

        /// <summary> First few observations, for diagnosis </summary>
        public IReadOnlyList<string> Examples { get; init; } = [];

        /// <summary>
        /// Whether the stream was complete, ordered and evenly spaced. Only
        /// meaningful for a scenario in which no heartbeat is expected.
        /// </summary>
        public bool IsClean =>
            MissingValues == 0 &&
            OutOfOrderValues == 0 &&
            OutOfOrderIncludingHeartbeats == 0 &&
            RepeatedValues == 0 &&
            ValueIntervalViolations == 0 &&
            MessageIntervalViolations == 0 &&
            SamplesWithoutSourceTimestamp == 0 &&
            NodesMissing == 0;

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder()
                .AppendLine(CultureInfo.InvariantCulture, $"Samples             : {TotalSamples} ({ValueSamples} values, {HeartbeatSamples} heartbeats)")
                .AppendLine(CultureInfo.InvariantCulture, $"Nodes               : {NodesSeen} seen, {NodesMissing} never reported")
                .AppendLine(CultureInfo.InvariantCulture, $"(a) missing values  : {MissingValues}")
                .AppendLine(CultureInfo.InvariantCulture, $"(b) out of order    : {OutOfOrderValues} (incl. heartbeats: {OutOfOrderIncludingHeartbeats})")
                .AppendLine(CultureInfo.InvariantCulture, $"(c) value interval  : {ValueIntervalViolations} violations")
                .AppendLine(CultureInfo.InvariantCulture, $"(c) message interval: {MessageIntervalViolations} violations")
                .AppendLine(CultureInfo.InvariantCulture, $"(d) ts regressions  : {SourceTimestampRegressions}")
                .AppendLine(CultureInfo.InvariantCulture, $"(d) repeated values : {RepeatedValues} ({RepeatedValuesFromHeartbeat} heartbeat, {UnflaggedRepeats} unflagged)")
                .AppendLine(CultureInfo.InvariantCulture, $"    no timestamp    : {SamplesWithoutSourceTimestamp}")
                .AppendLine(CultureInfo.InvariantCulture, $"    heartbeats/node : {MinHeartbeatsPerNode} min, {MaxHeartbeatsPerNode} max")
                .AppendLine(CultureInfo.InvariantCulture, $"    hb timestamp    : {HeartbeatsWithChangedTimestamp} changed")
                .AppendLine(CultureInfo.InvariantCulture, $"    hb too early    : {EarlyHeartbeats}")
                .AppendLine(CultureInfo.InvariantCulture, $"    hb cadence      : {HeartbeatCadenceViolations} violations")
                .AppendLine(CultureInfo.InvariantCulture, $"    redelivered     : {DuplicateDeliveries}");
            foreach (var example in Examples)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  ! {example}");
            }
            return builder.ToString();
        }
    }
}
