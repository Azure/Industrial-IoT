// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Settings for pending alarms feature
    /// </summary>
    [DataContract]
    public class PendingAlarmsOptionsModel {
        /// <summary>
        /// Is pending alarms enabled for this event node?
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Time interval for sending pending interval updates in seconds.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int? UpdateInterval { get; set; } = 30000;

        /// <summary>
        /// UpdateInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? UpdateIntervalTimespan {
            get => UpdateInterval.HasValue ?
                TimeSpan.FromSeconds(UpdateInterval.Value) : (TimeSpan?)null;
        }

        /// <summary>
        /// Time interval for sending pending interval snapshot in seconds.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int? SnapshotInterval { get; set; } = 60000;

        /// <summary>
        /// SnapshotInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? SnapshotIntervalTimespan {
            get => SnapshotInterval.HasValue ?
                TimeSpan.FromSeconds(SnapshotInterval.Value) : (TimeSpan?)null;
        }

        /// <summary>
        /// Index in the SelectClause array for Condition id field
        /// </summary>
        [IgnoreDataMember]
        public int? ConditionIdIndex { get; set; }

        /// <summary>
        /// Index in the SelectClause array for Retain field
        /// </summary>
        [IgnoreDataMember]
        public int? RetainIndex { get; set; }

        /// <summary>
        /// Has the pending alarms events been updated since las update message?
        /// </summary>
        [IgnoreDataMember]
        public bool Dirty { get; set; }
    }
}
