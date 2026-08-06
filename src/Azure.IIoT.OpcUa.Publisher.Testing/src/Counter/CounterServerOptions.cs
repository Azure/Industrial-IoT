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
    }
}
