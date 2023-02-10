// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Industrial iot diagnostics
    /// </summary>
    public static class Diagnostics {

        /// <summary>
        /// Helper - remove when moving to 7.0, then update below
        /// </summary>
        public static void Meter_CreateObservableUpDownCounter<T>(string name,
            Func<Measurement<T>> observeValue, string unit = null,
            string description = null) where T : struct {
#if NET6_0
            Meter.CreateObservableGauge(name, observeValue, unit, description);
#else
            Meter.CreateObservableUpDownCounter(name, observeValue, unit, description);
#endif
        }

        /// <summary>
        /// Metrics
        /// </summary>
        public static readonly Meter Meter = new Meter("Azure.Industrial-IoT", "2.9");
    }
}
