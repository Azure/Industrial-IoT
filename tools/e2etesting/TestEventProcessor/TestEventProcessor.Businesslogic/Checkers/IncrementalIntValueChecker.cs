// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Checker to validate that values for nodeIds are incrementally increasing integers.
    /// It will return number of values that were either duplicates or indicate that dropped messages.
    /// </summary>
    class IncrementalIntValueChecker {

        private readonly IDictionary<string, int> _latestValuePerNodeId;
        private readonly IDictionary<string, DateTime> _latestDateTimePerNodeId;
        private uint _duplicateValues = 0;
        private uint _droppedValues = 0;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for IncrementalIntValueChecker.
        /// </summary>
        /// <param name="logger"></param>
        public IncrementalIntValueChecker(
            ILogger logger
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _latestValuePerNodeId = new Dictionary<string, int>();
            _latestDateTimePerNodeId = new Dictionary<string, DateTime>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        public void ProcessEvent(
            string nodeId,
            DateTime sourceTimestamp,
            JToken value
        ) {
            int curValue;

            try {
                curValue = value.ToObject<int>();
            }
            catch (Exception) {
                _logger.LogError("Failed to extract int value from {value}", value);
                return;
            }

            _lock.Wait();
            try {
                if (!_latestValuePerNodeId.ContainsKey(nodeId)) {
                    // There is no previous value.
                    _latestValuePerNodeId.Add(nodeId, curValue);
                    _latestDateTimePerNodeId.Add(nodeId, sourceTimestamp);
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId] + 1) {
                    _latestValuePerNodeId[nodeId] = curValue;
                    _latestDateTimePerNodeId[nodeId] = sourceTimestamp;
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId]) {
                    _duplicateValues++;
                    _logger.LogWarning("Duplicate value detected for {nodeId}: {value}, {prevTimestamp} -> {curTimestamp}",
                        nodeId, curValue, _latestDateTimePerNodeId[nodeId], sourceTimestamp);
                    _latestDateTimePerNodeId[nodeId] = sourceTimestamp;
                }
                else {
                    _droppedValues++;
                    _logger.LogWarning("Dropped value detected for {nodeId}, " +
                        "previous value is {prevValue} at {prevTimestamp} " +
                        "and current value is {curValue} at {curTimestamp}.",
                        nodeId, _latestValuePerNodeId[nodeId], _latestDateTimePerNodeId[nodeId],
                        curValue, sourceTimestamp);

                    _latestValuePerNodeId[nodeId] = curValue;
                    _latestDateTimePerNodeId[nodeId] = sourceTimestamp;
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