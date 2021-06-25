// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Checker to validate that values for nodeIds are incrementally increasing integers.
    /// It will return number of values that were either duplicates or indicate that dropped messages.
    /// </summary>
    class IncrementalIntValueChecker {

        /// <summary>
        /// Format to be used for Timestamps
        /// </summary>
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffffZ";

        private readonly IDictionary<string, Tuple<int, DateTime>> _latestValuePerNodeId;
        private uint _duplicateValues = 0;
        private uint _droppedValues = 0;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly DateTimeFormatInfo _dateTimeFormatInfo;

        /// <summary>
        /// Constructor for IncrementalIntValueChecker.
        /// </summary>
        /// <param name="logger"></param>
        public IncrementalIntValueChecker(
            ILogger logger
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _latestValuePerNodeId = new Dictionary<string, Tuple<int, DateTime>>();
            _lock = new SemaphoreSlim(1, 1);
            _dateTimeFormatInfo = new DateTimeFormatInfo();
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="timestamp"></param>
        /// <param name="value"></param>
        public void ProcessEvent(
            string nodeId,
            DateTime timestamp,
            object value
        ) {
            int curValue;

            // do not process if we miss data
            if (string.IsNullOrEmpty(nodeId) || value == null) {
                return;
            }

            try {
                curValue = Convert.ToInt32(value);
            }
            catch (Exception) {
                _logger.LogError("Failed to extract int value from {value}", value);
                return;
            }

            _lock.Wait();
            try {
                if (!_latestValuePerNodeId.ContainsKey(nodeId)) {
                    // There is no previous value.
                    _latestValuePerNodeId[nodeId] = Tuple.Create(curValue, timestamp);
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId].Item1 + 1) {
                    _latestValuePerNodeId[nodeId] = Tuple.Create(curValue, timestamp);
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId].Item1) {
                    _duplicateValues++;

                    _logger.LogWarning("Duplicate value detected for {nodeId}, previous timestamp is {prevTimestamp}, " +
                        "current timestamp is {curTimestamp}.",
                        nodeId,
                        _latestValuePerNodeId[nodeId].Item2.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                        timestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo));

                    _latestValuePerNodeId[nodeId] = Tuple.Create(curValue, timestamp);
                }
                else {
                    _droppedValues++;
                    _logger.LogWarning("Dropped value detected for {nodeId}, " +
                        "previous value is {prevValue} with timestamp {prevTimestamp} " +
                        "and current value is {curValue} with timestamp {curTimestamp}.",
                        nodeId, _latestValuePerNodeId[nodeId].Item1,
                        _latestValuePerNodeId[nodeId].Item2.ToString(_dateTimeFormat, _dateTimeFormatInfo),
                        curValue, timestamp.ToString(_dateTimeFormat, _dateTimeFormatInfo));

                    _latestValuePerNodeId[nodeId] = Tuple.Create(curValue, timestamp);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop processing and return number of detected duplicate or dropped messages.
        /// </summary>
        /// <returns></returns>
        public IncrementalIntValueCheckerResult Stop() {
            _lock.Wait();
            try {
                var result = new IncrementalIntValueCheckerResult() {
                    DroppedValueCount = _droppedValues,
                    DuplicateValueCount = _duplicateValues,
                };

                return result;
            }
            finally {
                _lock.Release();
            }
        }
    }
}