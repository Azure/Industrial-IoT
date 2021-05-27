// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using System;
    using System.Collections.Generic;
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
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Time interval for sending pending interval updates
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
        /// Time interval for sending pending interval snapshot
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
        public bool Dirty { get; set; } = false;

        /// <summary>
        /// Should we compress using GZip when sending pending alarm messages for this item?
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public bool CompressMessages { get; set; } = false;

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public PendingAlarmsOptionsModel Clone() {
            return new PendingAlarmsOptionsModel {
                IsEnabled = IsEnabled,
                UpdateInterval = UpdateInterval,
                SnapshotInterval = SnapshotInterval,
                ConditionIdIndex = ConditionIdIndex,
                RetainIndex = RetainIndex,
                Dirty = Dirty,
                CompressMessages = CompressMessages
            };
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object obj) {
            return obj is PendingAlarmsOptionsModel model &&
                   IsEnabled == model.IsEnabled &&
                   UpdateInterval == model.UpdateInterval &&
                   SnapshotInterval == model.SnapshotInterval &&
                   ConditionIdIndex == model.ConditionIdIndex &&
                   RetainIndex == model.RetainIndex &&
                   Dirty == model.Dirty &&
                   CompressMessages == model.CompressMessages;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns>Return the hash code for this object</returns>
        public override int GetHashCode() {
            return HashCode.Combine(IsEnabled, UpdateInterval, SnapshotInterval, ConditionIdIndex, RetainIndex, Dirty, CompressMessages);
        }

        /// <summary>
        /// operator==
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are equal</returns>
        public static bool operator ==(PendingAlarmsOptionsModel left, PendingAlarmsOptionsModel right) => EqualityComparer<PendingAlarmsOptionsModel>.Default.Equals(left, right);

        /// <summary>
        /// operator!=
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are not equal</returns>
        public static bool operator !=(PendingAlarmsOptionsModel left, PendingAlarmsOptionsModel right) => !(left == right);
    }
}
