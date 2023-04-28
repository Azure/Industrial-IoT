// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Industrial iot diagnostics
    /// </summary>
    public static class Diagnostics
    {
        /// <summary>
        /// Version
        /// </summary>
        public const string Version = "2.9";

        /// <summary>
        /// namespace
        /// </summary>
        public const string Namespace = "Azure.Industrial-IoT";

        /// <summary>
        /// Metrics
        /// </summary>
        public static readonly Meter Meter = NewMeter();

        /// <summary>
        /// Metrics
        /// </summary>
        public static Meter NewMeter() => new(Namespace, Version);

        /// <summary>
        /// Tracing
        /// </summary>
        public static ActivitySource NewActivitySource() => new(Namespace, Version);
    }
}
