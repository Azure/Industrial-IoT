// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Class to check and report on delay between message source timestamp and time when it was received.
    /// Considerable deviations in this delay will also be reported.
    /// </summary>
    class MessageDeliveryDelayChecker {

        /// <summary>
        /// Format to be used for Timestamps
        /// </summary>
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffffZ";

        private readonly TimeSpan _expectedMaximalDuration;
        private readonly ILogger _logger;
        private readonly DateTimeFormatInfo _dateTimeFormatInfo;
        private readonly SemaphoreSlim _lock;
        private TimeSpan _maxMessageDeliveryDuration = TimeSpan.Zero;

        /// <summary>
        /// Constructor for the checker.
        /// </summary>
        /// <param name="expectedMaximalDuration"></param>
        /// <param name="logger"></param>
        public MessageDeliveryDelayChecker(
            TimeSpan expectedMaximalDuration,
            ILogger logger
        ) {
            if (expectedMaximalDuration.Ticks < 0) {
                throw new ArgumentException($"{nameof(expectedMaximalDuration)} cannot be negative");
            }

            _expectedMaximalDuration = expectedMaximalDuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _dateTimeFormatInfo = new DateTimeFormatInfo();

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId">Identifeir of the data source.</param>
        /// <param name="sourceTimestamp">Timestamp at the Data Source.</param>
        /// <param name="enqueuedTimestamp">IoT Hub message enqueue timestamp.</param>
        public void ProcessEvent(
            string nodeId,
            DateTime sourceTimestamp,
            DateTime enqueuedimestamp
        ) {
            // Do not process if _expectedMaximalDuration is set to zero.
            if (_expectedMaximalDuration.Equals(TimeSpan.Zero)) {
                return;
            }

            // Check the total duration from OPC UA Server until IoT Hub
            var messageDeliveryDuration = enqueuedimestamp - sourceTimestamp;

            if (messageDeliveryDuration.TotalMilliseconds < 0) {
                _logger.LogWarning("Total duration is negative number for {nodeId} node, " +
                    "OPC UA Server time {OPCUATime}, IoTHub enqueue time {IoTHubTime}, delta {Diff}",
                    nodeId,
                    sourceTimestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                    enqueuedimestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                    messageDeliveryDuration);
            }

            if (messageDeliveryDuration > _expectedMaximalDuration) {
                _logger.LogInformation("Total duration exceeded limit for {nodeId} node, " +
                    "OPC UA Server time {OPCUATime}, IoTHub enqueue time {IoTHubTime}, delta {Diff}",
                    nodeId,
                    sourceTimestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                    enqueuedimestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                    messageDeliveryDuration);
            }

            _lock.Wait();
            try {
                // Check if we need to update _maxOpcDiffToNow
                if (messageDeliveryDuration > _maxMessageDeliveryDuration) {
                    _maxMessageDeliveryDuration = messageDeliveryDuration;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop monitoring and return max observed delivery delay.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Stop() {
            _lock.Wait();
            try {
                return _maxMessageDeliveryDuration;
            }
            finally {
                _lock.Release();
            }
        }
    }
}