// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Checker to validate that expected number of value changes have been received per timestamp.
    /// </summary>
    class MissingValueChangesChecker {

        private readonly uint _expectedValueChangesPerTimestamp;
        private readonly ILogger _logger;

        private readonly IDictionary<DateTime, int> _valueChangesPerTimestamp;
        private bool _isStopped = false;
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// Constructor for MissingValueChangesChecker.
        /// Checking will be disabled if expectedValueChangesPerTimestamp is 0.
        /// </summary>
        /// <param name="expectedValueChangesPerTimestamp"></param>
        /// <param name="logger"></param>
        public MissingValueChangesChecker(
            uint expectedValueChangesPerTimestamp,
            ILogger logger
        ) {
            _expectedValueChangesPerTimestamp = expectedValueChangesPerTimestamp;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _valueChangesPerTimestamp = new Dictionary<DateTime, int>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="sourceTimestamp"></param>
        /// <param name="_"></param>
        public void ProcessEvent(DateTime sourceTimestamp) {
            // Do not process events after Stop() has been called.
            if (_isStopped) {
                return;
            }

            // Do not process if _expectedValueChangesPerTimestamp is set to zero.
            if (_expectedValueChangesPerTimestamp == 0) {
                return;
            }

            _lock.Wait();
            try {
                if (!_valueChangesPerTimestamp.ContainsKey(sourceTimestamp)) {
                    _valueChangesPerTimestamp.Add(sourceTimestamp, 0);
                }

                _valueChangesPerTimestamp[sourceTimestamp]++;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Start validation.
        /// </summary>
        /// <param name="checkDelay"> Delay between check runs. </param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task StartAsync(
            TimeSpan checkDelay,
            CancellationToken token = default
        ) {
            if (checkDelay.Ticks < 0) {
                throw new ArgumentException($"{nameof(checkDelay)} cannot be negative");
            }

            return new Task(() => {
                try {
                    while (!token.IsCancellationRequested) {

                        CheckForMissingValueChanges();
                        Task.Delay(checkDelay, token).Wait(token);
                    }
                }
                catch (OperationCanceledException oce) {
                    if (oce.CancellationToken == token) {
                        return;
                    }
                    throw;
                }
            }, token);
        }

        /// <summary>
        /// Stop validation and return number of incomplete timestamps.
        /// </summary>
        /// <returns></returns>
        public int Stop() {
            _isStopped = true;

            CheckForMissingValueChanges();

            _lock.Wait();
            try {
                return _valueChangesPerTimestamp.Count;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Check that expected number of value changes have been received and clean complete timestamps
        /// from _valueChangesPerTimestamp.
        /// </summary>
        private void CheckForMissingValueChanges() {
            _lock.Wait();
            try {
                _logger.LogInformation("Currently waiting for {incompletedTimestamps} timestamp to be completed.",
                    _valueChangesPerTimestamp.Count);

                var entriesToDelete = new List<DateTime>(50);

                foreach (var missingSequence in _valueChangesPerTimestamp) {
                    var numberOfValueChanges = missingSequence.Value;
                    if (numberOfValueChanges >= _expectedValueChangesPerTimestamp) {
                        _logger.LogInformation("Received {NumberOfValueChanges} value changes for timestamp {Timestamp}",
                            numberOfValueChanges, missingSequence.Key);

                        // don't check for gaps of sequence numbers because they reflect the for number of messages
                        // send from OPC server to OPC publisher, it should be internally handled in OPCF stack

                        entriesToDelete.Add(missingSequence.Key);
                    }
                }

                // Remove all timestamps that are completed (all value changes received)
                foreach (var entry in entriesToDelete) {
                    var success = _valueChangesPerTimestamp.Remove(entry);

                    if (!success) {
                        _logger.LogError("Could not remove timestamp {Timestamp} with all value changes from internal list",
                            entry);
                    }
                    else {
                        _logger.LogInformation("[Success] All value changes received for {Timestamp}", entry);
                    }
                }

                // Log total amount of missing value changes for each timestamp that already reported 80% of value changes
                foreach (var missingSequence in _valueChangesPerTimestamp) {
                    if (missingSequence.Value > (int)(_expectedValueChangesPerTimestamp * 0.8)) {
                        _logger.LogInformation("For timestamp {Timestamp} there are {NumberOfMissing} value changes missing",
                            missingSequence.Key, _expectedValueChangesPerTimestamp - missingSequence.Value);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }
    }
}