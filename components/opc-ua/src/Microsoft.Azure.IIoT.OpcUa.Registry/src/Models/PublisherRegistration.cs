// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Threading;

    /// <summary>
    /// Publisher agent module registration
    /// </summary>
    [DataContract]
    public sealed class PublisherRegistration : EntityRegistration {

        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => IdentityType.Publisher;

        /// <summary>
        /// Device id for registration
        /// </summary>
        [DataMember]
        public string ModuleId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Job orchestrator url
        /// </summary>
        [DataMember]
        public string JobOrchestratorUrl { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        [DataMember]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Workers to start
        /// </summary>
        [DataMember]
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Interval to check for updates
        /// </summary>
        [DataMember]
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Match capablities
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public PublisherRegistration(string deviceId = null,
            string moduleId = null) {
            DeviceId = deviceId;
            ModuleId = moduleId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is PublisherRegistration registration)) {
                return false;
            }
            if (!base.Equals(registration)) {
                return false;
            }
            if (ModuleId != registration.ModuleId) {
                return false;
            }
            if (LogLevel != registration.LogLevel) {
                return false;
            }
            if (JobOrchestratorUrl != registration.JobOrchestratorUrl) {
                return false;
            }
            if ((MaxWorkers ?? 1) != (registration.MaxWorkers ?? 1)) {
                return false;
            }
            if ((JobCheckInterval ?? Timeout.InfiniteTimeSpan) !=
                (registration.JobCheckInterval ?? Timeout.InfiniteTimeSpan)) {
                return false;
            }
            if ((HeartbeatInterval ?? Timeout.InfiniteTimeSpan) !=
                (registration.HeartbeatInterval ?? Timeout.InfiniteTimeSpan)) {
                return false;
            }
            if (!Capabilities.SetEqualsSafe(registration.Capabilities,
                    (x, y) => x.Key == y.Key && x.Value == y.Value)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(PublisherRegistration r1,
            PublisherRegistration r2) => EqualityComparer<PublisherRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(PublisherRegistration r1,
            PublisherRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TraceLogLevel?>.Default.GetHashCode(LogLevel);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(JobOrchestratorUrl);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(HeartbeatInterval ?? Timeout.InfiniteTimeSpan);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(MaxWorkers ?? 1);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(JobCheckInterval ?? Timeout.InfiniteTimeSpan);
            return hashCode;
        }

        internal bool IsInSync() {
            return _isInSync;
        }

        internal bool IsConnected() {
            return Connected;
        }

        internal bool _isInSync;
    }
}
