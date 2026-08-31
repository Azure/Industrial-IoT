// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Telemetry
{
    using System;

    /// <summary>
    /// Configuration of a <see cref="TelemetryQualityValidator"/>.
    /// </summary>
    public sealed record class TelemetryQualityOptions
    {
        /// <summary>
        /// Interval at which the server increments every counter. The
        /// expected source timestamp distance between the counter values
        /// <c>n</c> and <c>m</c> is <c>(m - n) * UpdateInterval</c>.
        /// </summary>
        public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Number of nodes that are expected to report. Used to detect nodes
        /// that never produced a single value.
        /// </summary>
        public int ExpectedNodeCount { get; init; }

        /// <summary>
        /// Tolerance applied when comparing source timestamp distances.
        /// Defaults to a tenth of <see cref="UpdateInterval"/>. Servers that
        /// derive timestamps from a wall clock timer need a larger value than
        /// servers that use a strictly monotonic schedule.
        /// </summary>
        public TimeSpan? Tolerance { get; init; }

        /// <summary>
        /// Configured heartbeat interval. When set, heartbeat cadence is
        /// analysed using the message timestamp.
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; init; }

        /// <summary>
        /// Publishing interval of the subscription. Together with
        /// <see cref="HeartbeatInterval"/> this yields the earliest instant
        /// at which a watchdog heartbeat may legitimately be emitted after a
        /// value was received.
        /// </summary>
        public TimeSpan? PublishingInterval { get; init; }

        /// <summary>
        /// Tolerance applied when comparing heartbeat cadence. Defaults to
        /// half of <see cref="HeartbeatInterval"/>.
        /// </summary>
        public TimeSpan? HeartbeatTolerance { get; init; }

        /// <summary>
        /// <para>
        /// Drop samples whose writer sequence number was already seen.
        /// </para>
        /// <para>
        /// Delivery through IoT Hub is at least once, so a redelivered
        /// message is indistinguishable from a value that the publisher
        /// repeated. Without suppression a transport redelivery would be
        /// reported as a stale value and fail the run for the wrong reason.
        /// </para>
        /// </summary>
        public bool SuppressDuplicates { get; init; } = true;

        /// <summary>
        /// How many sequence numbers are remembered for duplicate detection.
        /// Bounded so a multi hour run cannot exhaust memory.
        /// </summary>
        public int DuplicateWindow { get; init; } = 100000;

        /// <summary>
        /// How many example observations are recorded for diagnosis.
        /// </summary>
        public int MaxExamples { get; init; } = 25;

        /// <summary>
        /// <para>
        /// When <c>true</c>, the data source is known to be strictly
        /// monotonically increasing (e.g. a counter that only ever increments
        /// by one). In that case a sample that repeats the last observed value
        /// is definitionally a heartbeat, regardless of whether the
        /// <c>SourceTimestamp</c> also matches.
        /// </para>
        /// <para>
        /// This stronger rule is sound only for a strictly increasing source
        /// because a genuinely new sample would always carry a strictly higher
        /// value. A repeated value can therefore only arise from a watchdog
        /// heartbeat. For general sources — where a sensor can report the same
        /// reading twice at different wall-clock instants — a repeated value
        /// with a new timestamp is a real data update, not a heartbeat, so the
        /// conservative rule (value <em>and</em> timestamp must both match)
        /// must be applied instead.
        /// </para>
        /// <para>
        /// Defaults to <c>false</c> so the conservative rule is always active
        /// unless the caller explicitly opts in.
        /// </para>
        /// </summary>
        public bool MonotonicSource { get; init; }
    }
}
