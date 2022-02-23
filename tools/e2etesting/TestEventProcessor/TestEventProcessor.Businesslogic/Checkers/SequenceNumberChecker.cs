// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;

    /// <summary>
    /// Checker to validate that values of sequenceNumber are incrementally increasing.
    /// It will return number duplicates, dropped and reset counts in the recorded progression.
    /// </summary>
    class SequenceNumberChecker {

        private uint? _latestValue = null;
        private uint _duplicateValues = 0;
        private uint _droppedValues = 0;
        private uint _resetValues = 0;
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
        /// <param name="value"></param>
        public void ProcessEvent(uint? value) {
            uint curValue = value ?? throw new ArgumentNullException(nameof(value));
            _lock.Wait();
            try {
                if (!_latestValue.HasValue) {
                    // There is no previous value.
                    _latestValue = curValue;
                    return;
                }

                if (curValue == _latestValue + 1) {
                    _latestValue = curValue;
                    return;
                }

                if (curValue == _latestValue) {
                    _duplicateValues++;
                    _logger.LogWarning("Duplicate SequenceNumber {value} detected ", curValue);
                    return;
                }

                if (curValue < _latestValue) {
                    _resetValues++;
                    _logger.LogWarning("Reset SequenceNumber: previous {prevValue} vs current {value} detected.",
                        _latestValue, curValue);
                    _latestValue = curValue;
                    return;
                }

                _droppedValues += curValue - _latestValue.Value;
                _logger.LogWarning("Dropped SequenceNumbers {count}: previous {prevValue} vs current {curValue}" +
                    " detected.", curValue - _latestValue.Value, _latestValue, curValue);
                _latestValue = curValue;
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
                    DroppedValueCount = _droppedValues,
                    DuplicateValueCount = _duplicateValues,
                    ResetsValueCount = _resetValues,
                };

                return result;
            }
            finally {
                _lock.Release();
            }
        }
    }
}
