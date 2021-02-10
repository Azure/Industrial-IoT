// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Class to check and report considerable deviations in message delivery duration.
    /// </summary>
    class DelayChangeChecker {

        private readonly TimeSpan _threshold;
        private readonly ILogger _logger;

        private TimeSpan _lastOpcDiffToNow = TimeSpan.Zero;

        /// <summary>
        /// Constructor for the checker.
        /// </summary>
        /// <param name="threshold"></param>
        /// <param name="logger"></param>
        public DelayChangeChecker(
            TimeSpan threshold,
            ILogger logger
        ) {
            if (threshold.Ticks < 0) {
                throw new ArgumentException($"{nameof(threshold)} cannot be negative");
            }

            _threshold = threshold;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="_0"></param>
        /// <param name="sourceTimestamp"></param>
        /// <param name="_1"></param>
        public void ProcessEvent(
            string _0,
            DateTime sourceTimestamp,
            object _1
        ) {
            // Check and report if processing delay has changed considerably, meaning more that the threshold.
            var newOpcDiffToNow = DateTime.UtcNow - sourceTimestamp;
            var diffDelta = newOpcDiffToNow - _lastOpcDiffToNow;

            if (diffDelta.Duration() > _threshold) {
                _logger.LogWarning("The different between UtcNow and Opc Source Timestamp has changed by {diff}", diffDelta);
            }

            _lastOpcDiffToNow = newOpcDiffToNow;
        }
    }
}