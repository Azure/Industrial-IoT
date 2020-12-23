// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;

    /// <summary>
    /// Metric logger
    /// </summary>
    public sealed class MetricsLogger : IMetricsLogger  {

        /// <summary>
        /// Create metric logger
        /// </summary>
        /// <param name="logger"></param>
        public MetricsLogger(ILogger logger) {
            _logger = logger;
        }

         /// <inheritdoc/>
        public void TrackEvent(string name) {
            _logger.Information("Event {event}", name);
        }

        /// <inheritdoc/>
        public void TrackValue(string name, int value) {
            _logger.Information("Event {event} {value}", name, value);
        }

        /// <inheritdoc/>
        public void TrackDuration(string name, double milliseconds) {
            _logger.Information("Duration {event} {duration}", name, milliseconds);
        }

        private readonly ILogger _logger;
    }
}