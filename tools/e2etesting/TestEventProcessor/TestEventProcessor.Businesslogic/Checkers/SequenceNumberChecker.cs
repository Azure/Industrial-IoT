// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Checker to validate that values of sequenceNumber are incrementally increasing.
    /// It will return number duplicates, dropped and reset counts in the recorded progression.
    /// </summary>
    class SequenceNumberChecker {

        private Dictionary<string, uint> _latestValue = new Dictionary<string, uint>();
        private Dictionary<string, uint> _duplicateValues = new Dictionary<string, uint>();
        private Dictionary<string, uint> _droppedValues = new Dictionary<string, uint>();
        private Dictionary<string, uint> _resetValues = new Dictionary<string, uint>();
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for SequenceNumberChecker.
        /// </summary>
        /// <param name="logger"></param>
        public SequenceNumberChecker(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Processing the received event.
        /// </summary>
        /// <param name="dataSetWriterId"> DataSetWriterId associated with sequence number. </param>
        /// <param name="sequenceNumber"> Value of sequence number. </param>
        public void ProcessEvent(string dataSetWriterId, uint? sequenceNumber) {
            uint curValue = sequenceNumber ?? throw new ArgumentNullException(nameof(sequenceNumber));

            // Default value when dataSetWriterId is not detected.
            if (dataSetWriterId is null) {
                dataSetWriterId = "$null";
            }

            _lock.Wait();
            try {
                if (!_latestValue.ContainsKey(dataSetWriterId)) {
                    // There is no previous value.
                    _latestValue.Add(dataSetWriterId, curValue);
                    _duplicateValues.Add(dataSetWriterId, 0);
                    _droppedValues.Add(dataSetWriterId, 0);
                    _resetValues.Add(dataSetWriterId, 0);
                    return;
                }

                if (curValue == _latestValue[dataSetWriterId] + 1) {
                    _latestValue[dataSetWriterId] = curValue;
                    return;
                }

                if (curValue == _latestValue[dataSetWriterId]) {
                    _duplicateValues[dataSetWriterId]++;
                    _logger.LogWarning("Duplicate SequenceNumber for {dataSetWriterId} dataSetWriter detected: {value}",
                        dataSetWriterId, curValue);
                    return;
                }

                if (curValue < _latestValue[dataSetWriterId]) {
                    _resetValues[dataSetWriterId]++;
                    _logger.LogWarning("Reset SequenceNumber for {dataSetWriterId} dataSetWriter detected: previous {prevValue} vs current {value}",
                        dataSetWriterId, _latestValue[dataSetWriterId], curValue);
                    _latestValue[dataSetWriterId] = curValue;
                    return;
                }

                _droppedValues[dataSetWriterId] += curValue - _latestValue[dataSetWriterId];
                _logger.LogWarning("Dropped SequenceNumbers for {dataSetWriterId} dataSetWriter detected: {count}: previous {prevValue} vs current {curValue}",
                    dataSetWriterId, curValue - _latestValue[dataSetWriterId], _latestValue[dataSetWriterId], curValue);
                _latestValue[dataSetWriterId] = curValue;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop processing and return the stored metrics.
        /// </summary>
        /// <returns></returns>
        public SequenceNumberCheckerResult Stop() {
            _lock.Wait();
            try {
                var result = new SequenceNumberCheckerResult() {
                    DroppedValueCount = (uint) _droppedValues.Sum(kvp => kvp.Value),
                    DuplicateValueCount = (uint) _duplicateValues.Sum(kvp => kvp.Value),
                    ResetsValueCount = (uint) _resetValues.Sum(kvp => kvp.Value),
                };

                return result;
            }
            finally {
                _lock.Release();
            }
        }
    }
}
