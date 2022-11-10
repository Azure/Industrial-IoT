// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to check whether there are missing timestamps in the event timeline.
    /// The validation will be done per nodeId.
    /// </summary>
    class MissingTimestampsChecker {

        /// <summary>
        /// Format to be used for Timestamps
        /// </summary>
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffffZ";

        private readonly TimeSpan _expectedInterval;
        private readonly TimeSpan _threshold;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock;
        private readonly IDictionary<string, List<DateTime>> _sourceTimestamps;
        private readonly DateTimeFormatInfo _dateTimeFormatInfo;
        private bool _isStopped = false;
        private int _missingTimestampsCounter = 0;

        /// <summary>
        /// Constructor of the checker.
        /// </summary>
        /// <param name="expectedIntervalOfValueChanges"> Expected interval between value changes.
        /// Setting it to zero will allow for any interval to be valid </param>
        /// <param name="threshold"> Threshhold for expected interval comparison. </param>
        /// <param name="logger"> Logger. </param>
        public MissingTimestampsChecker(
            TimeSpan expectedIntervalOfValueChanges,
            TimeSpan threshold,
            ILogger logger
        ) {
            if (expectedIntervalOfValueChanges.Ticks < 0) {
                throw new ArgumentException($"{nameof(expectedIntervalOfValueChanges)} cannot be negative");
            }
            if (threshold.Ticks < 0) {
                throw new ArgumentException($"{nameof(threshold)} cannot be negative");
            }

            _expectedInterval = expectedIntervalOfValueChanges;
            _threshold = threshold;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _dateTimeFormatInfo = new DateTimeFormatInfo();

            _sourceTimestamps = new Dictionary<string, List<DateTime>>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="sourceTimestamp"></param>
        /// <param name="_"></param>
        public void ProcessEvent(
            string nodeId,
            DateTime sourceTimestamp,
            JToken _
        ) {
            // Do not process events after Stop() has been called.
            if (_isStopped) {
                return;
            }

            // Do not process if _expectedInterval is set to zero.
            if (_expectedInterval.Equals(TimeSpan.Zero)) {
                return;
            }

            _lock.Wait();
            try {
                if (!_sourceTimestamps.ContainsKey(nodeId)) {
                    _sourceTimestamps.Add(nodeId, new List<DateTime>());
                }
                _sourceTimestamps[nodeId].Add(sourceTimestamp);
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

                        CheckForMissingTimestamps();
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
        /// Stop validation.
        /// </summary>
        /// <returns></returns>
        public int Stop() {
            _isStopped = true;

            CheckForMissingTimestamps();

            _lock.Wait();
            try {
                return _missingTimestampsCounter;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Validate all timestamp intervals that have been received this far.
        /// </summary>
        private void CheckForMissingTimestamps() {
            _lock.Wait();
            try {
                foreach(var kvp in _sourceTimestamps) {
                    if (kvp.Value.Count < 2) {
                        // Nothing to check as there are no enough timestamps to compare.
                        continue;
                    }

                    var nodeId = kvp.Key;
                    var nodeSourceTimestamps = kvp.Value;

                    while (nodeSourceTimestamps.Count > 1) {
                        var older = nodeSourceTimestamps[0];
                        nodeSourceTimestamps.RemoveAt(0);

                        var newer = nodeSourceTimestamps[0];

                        // Compare on milliseconds isn't useful, instead we will try time window of with
                        // given threshhold
                        var expectedTime = older + _expectedInterval;

                        var expectedMin = expectedTime - _threshold;
                        var expectedMax = expectedTime + _threshold;

                        if (newer < expectedMin || newer > expectedMax) {
                            var expectedTS = expectedTime.ToString(_dateTimeFormat, _dateTimeFormatInfo);
                            var olderTS = older.ToString(_dateTimeFormat, _dateTimeFormatInfo);
                            var newerTS = newer.ToString(_dateTimeFormat, _dateTimeFormatInfo);
                            _logger.LogWarning(
                                "Missing timestamp for {nodeId}, value changes for {ExpectedTs} not " +
                                "received, predecessor {Older} successor {Newer}",
                                nodeId, expectedTS, olderTS, newerTS
                            );

                            _missingTimestampsCounter++;
                        }
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }
    }
}
