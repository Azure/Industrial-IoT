// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Checker to validate that values for nodeIds are incrementally increasing integers.
    /// It will return number of values that were either duplicates or indicate that dropped messages.
    /// </summary>
    class IncrementalIntValueChecker {

        private readonly IDictionary<string, int> _latestValuePerNodeId;
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
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        public void ProcessEvent(
            string nodeId,
            object value
        ) {
            int curValue;

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
                    _latestValuePerNodeId[nodeId] = curValue;
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId] + 1) {
                    _latestValuePerNodeId[nodeId] = curValue;
                    return;
                }

                if (curValue == _latestValuePerNodeId[nodeId]) {
                    _duplicateValues++;
                    _logger.LogWarning("Duplicate value detected for {nodeId}: {value}", nodeId, curValue);
                } else {
                    _droppedValues++;
                    _logger.LogWarning("Dropped value detected for {nodeId}, previous value is {prevValue} " +
                        "and current value is {curValue}.", nodeId, _latestValuePerNodeId[nodeId], curValue);

                    _latestValuePerNodeId[nodeId] = curValue;
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