namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Settings for pending alarms feature
    /// </summary>
    [DataContract]
    public class PendingAlarmModel {
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
                TimeSpan.FromMilliseconds(UpdateInterval.Value) : (TimeSpan?)null;
            set => UpdateInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
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
                TimeSpan.FromMilliseconds(SnapshotInterval.Value) : (TimeSpan?)null;
            set => SnapshotInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public PendingAlarmModel Clone() {
            return new PendingAlarmModel {
                IsEnabled = IsEnabled,
                UpdateInterval = UpdateInterval,
                SnapshotInterval = SnapshotInterval
            };
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object obj) {
            return obj is PendingAlarmModel model &&
                   IsEnabled == model.IsEnabled &&
                   UpdateInterval == model.UpdateInterval &&
                   SnapshotInterval == model.SnapshotInterval;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns>Return the hash code for this object</returns>
        public override int GetHashCode() {
            return HashCode.Combine(IsEnabled, UpdateInterval, SnapshotInterval);
        }

        /// <summary>
        /// operator==
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are equal</returns>
        public static bool operator ==(PendingAlarmModel left, PendingAlarmModel right) => EqualityComparer<PendingAlarmModel>.Default.Equals(left, right);

        /// <summary>
        /// operator!=
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are not equal</returns>
        public static bool operator !=(PendingAlarmModel left, PendingAlarmModel right) => !(left == right);
    }
}
