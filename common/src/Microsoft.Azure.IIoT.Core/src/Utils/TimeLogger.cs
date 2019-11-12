// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Serilog;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Time execution
    /// </summary>
    public class TimeLogger : IDisposable {

        /// <summary>
        /// Create metric logger
        /// </summary>
        /// <param name="watchName"> Watch name eg. method name.</param>
        /// <param name="metrics"> Metrics Logger.</param>
        public TimeLogger(string watchName, IMetricsLogger metrics) {
            _stopWatch = Stopwatch.StartNew();
            _watchName = watchName;
            _metrics = metrics;
        }

        /// <inheritdoc/>
        public Stopwatch Stop() {
            if (_stopWatch != null) {
                _stopWatch.Stop();
            }
            return _stopWatch;
        }

        /// <inheritdoc/>
        public void Print() {
            if (_stopWatch != null) {
                if (_metrics != null && !string.IsNullOrEmpty(_watchName)) {
                    _metrics.TrackDuration(_watchName, _stopWatch.ElapsedMilliseconds);
                }
                else {
                    Log.Logger.Warning("Please provide name of the execution code you want to track.");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Stop();
            Print();
        }

        private readonly Stopwatch _stopWatch;
        private readonly string _watchName;
        private readonly IMetricsLogger _metrics;
    }
}