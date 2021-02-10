// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;

    /// <summary>
    /// Class to check and report on delay between message source timestamp and time when it was received.
    /// Considerable deviations in this delay will also be reported.
    /// </summary>
    class MessageProcessingDelayChecker {

        private readonly TimeSpan _threshold;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _lock;
        private TimeSpan _lastOpcDiffToNow = TimeSpan.Zero;
        private TimeSpan _maxOpcDiffToNow = TimeSpan.Zero;

        /// <summary>
        /// Constructor for the checker.
        /// </summary>
        /// <param name="threshold"></param>
        /// <param name="logger"></param>
        public MessageProcessingDelayChecker(
            TimeSpan threshold,
            ILogger logger
        ) {
            if (threshold.Ticks < 0) {
                throw new ArgumentException($"{nameof(threshold)} cannot be negative");
            }

            _threshold = threshold;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _lock = new SemaphoreSlim(1, 1);
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
            _lock.Wait();
            try {
                // Check and report if processing delay has changed considerably, meaning more that the threshold.
                var newOpcDiffToNow = DateTime.UtcNow - sourceTimestamp;
                var diffDelta = newOpcDiffToNow - _lastOpcDiffToNow;

                if (diffDelta.Duration() > _threshold) {
                    _logger.LogWarning("The different between UtcNow and Opc Source Timestamp has changed by {diff}", diffDelta);
                }

                _lastOpcDiffToNow = newOpcDiffToNow;

                // Check if we need to update _maxOpcDiffToNow
                if (newOpcDiffToNow > _maxOpcDiffToNow) {
                    _maxOpcDiffToNow = newOpcDiffToNow;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop monitoring and return max observed delay.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Stop() {
            _lock.Wait();
            try {
                return _maxOpcDiffToNow;
            }
            finally {
                _lock.Release();
            }
        }
    }
}