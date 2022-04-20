// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestEventProcessor {

    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents the result of the Stop-Command of the TelemetryValidator.
    /// </summary>
    public class StopResult {
        /// <summary>
        /// The total number of value changes
        /// </summary>
        public int TotalValueChangesCount { get; set; }

        /// <summary>
        /// The start time of the current monitoring cycle.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time of the current monitoring cycle.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Indicates if all received timestamps received all required value changes
        /// </summary>
        public bool AllExpectedValueChanges { get; set; }

        /// <summary>
        /// The duration of the current monitoring cycle.
        /// </summary>
        public string Duration => (EndTime - StartTime).ToString();

        /// <summary>
        /// The number of value changes by Node Id.
        /// </summary>
        public ReadOnlyDictionary<string, int> ValueChangesByNodeId { get; set; }

        /// <summary>
        /// Indicates if all timestamps received have expected interval between them
        /// </summary>
        public bool AllInExpectedInterval { get; set; }

        /// <summary>
        /// Indicates max observed delay between message source timestamp and the time when it was received.
        /// </summary>
        public string MaxDelayToNow { get; set; }

        /// <summary>
        /// Indicates max observed delay between message source timestamp and the time when it was delivered
        /// to IoT Hub.
        /// </summary>
        public string MaxDeliveyDuration { get; set; }

        /// <summary>
        /// Indicates the number of dropped messages detected by IncrementalIntValueChecker.
        /// </summary>
        public uint DroppedValueCount { get; set; }

        /// <summary>
        /// Indicates the number of duplicate messages detected by IncrementalIntValueChecker.
        /// </summary>
        public uint DuplicateValueCount { get; set; }

        /// <summary>
        /// Indicates the number of dropped sequence numbers detected by SequenceNumberChecker.
        /// </summary>
        public uint DroppedSequenceCount { get; set; }

        /// <summary>
        /// Indicates the number of duplicate sequence numbers detected by SequenceNumberChecker.
        /// </summary>
        public uint DuplicateSequenceCount { get; set; }

        /// <summary>
        /// Indicates the number of times the sequence number was reset.
        /// </summary>
        public uint ResetSequenceCount { get; set; }

        /// <summary>
        /// Indicates whether restart announcement was received.
        /// </summary>
        public bool RestartAnnouncementReceived { get; set; }
    }
}
