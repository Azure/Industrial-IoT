// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Counter
{
    using System;

    /// <summary>
    /// Options controlling the deterministic counter server. The server
    /// exposes <see cref="NodeCount"/> variables that all count up from
    /// zero in lockstep, one increment every <see cref="UpdateInterval"/>.
    /// </summary>
    public sealed record class CounterServerOptions
    {
        /// <summary>
        /// Number of counter variables to expose.
        /// </summary>
        public int NodeCount { get; init; } = 100;

        /// <summary>
        /// Interval at which every counter is incremented by exactly one.
        /// </summary>
        public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// <para>
        /// When set the source timestamp of every value is taken from a
        /// strictly monotonic schedule (<c>epoch + counter * interval</c>)
        /// rather than from the wall clock at the time the value is
        /// produced.
        /// </para>
        /// <para>
        /// This makes the expected distance between two consecutive source
        /// timestamps exactly <see cref="UpdateInterval"/>, so any deviation
        /// observed by a consumer is unambiguously introduced downstream of
        /// the server and not by timer jitter inside it.
        /// </para>
        /// </summary>
        public bool UseScheduledTimestamps { get; init; } = true;

        /// <summary>
        /// <para>
        /// When set to a non zero interval the server occasionally stamps a
        /// value one <em>slot</em> later than the schedule says, and then
        /// returns to the schedule.
        /// </para>
        /// <para>
        /// This reproduces the signature of a data source whose scan
        /// scheduler slips: the value sequence stays complete and correctly
        /// spaced, but individual source timestamps are displaced by a
        /// quantized amount. A consumer measuring the distance between
        /// consecutive source timestamps sees one distance that is too long
        /// followed by one that is too short, and the two sum to exactly two
        /// update intervals.
        /// </para>
        /// </summary>
        public TimeSpan SlotSlip { get; init; }

        /// <summary>
        /// How often a slipped run begins, counted in increments. Together
        /// with <see cref="SlotSlipDwell"/> this makes the schedule fully
        /// deterministic, so a test can predict exactly which values carry a
        /// displaced timestamp.
        /// </summary>
        public int SlotSlipPeriod { get; init; } = 17;

        /// <summary>
        /// How many consecutive increments stay displaced once a slipped run
        /// begins. One produces the isolated single sample excursion that
        /// dominates the reported field data.
        /// </summary>
        public int SlotSlipDwell { get; init; } = 1;

        /// <summary>
        /// Whether the given counter value is stamped one slot late.
        /// </summary>
        /// <param name="value"></param>
        public bool IsSlipped(long value)
        {
            return SlotSlip != TimeSpan.Zero && SlotSlipPeriod > 0 &&
                value % SlotSlipPeriod < SlotSlipDwell;
        }
    }
}
