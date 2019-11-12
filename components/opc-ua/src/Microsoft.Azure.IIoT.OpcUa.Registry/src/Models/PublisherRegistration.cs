// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Publisher agent module registration
    /// </summary>
    [Serializable]
    public sealed class PublisherRegistration : BaseRegistration {

        /// <inheritdoc/>
        public override string DeviceType => "Publisher";

        /// <inheritdoc/>
        public override string ApplicationId => null;

        /// <summary>
        /// Publisher that owns the twin.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public override string SupervisorId =>
            SupervisorModelEx.CreateSupervisorId(DeviceId, ModuleId);

        /// <summary>
        /// Current log level
        /// </summary>
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Job orchestrator url
        /// </summary>
        public string JobOrchestratorUrl { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Workers to start
        /// </summary>
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Interval to check for updates
        /// </summary>
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Match capablities
        /// </summary>
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
            var registration = obj as PublisherRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                LogLevel == registration.LogLevel &&
                JobOrchestratorUrl == registration.JobOrchestratorUrl &&
                (MaxWorkers ?? 1) == (registration.MaxWorkers ?? 1) &&
                (JobCheckInterval ?? Timeout.InfiniteTimeSpan) ==
                    (registration.JobCheckInterval ?? Timeout.InfiniteTimeSpan) &&
                (HeartbeatInterval ?? Timeout.InfiniteTimeSpan) ==
                    (registration.HeartbeatInterval ?? Timeout.InfiniteTimeSpan) &&
                Capabilities.SetEqualsSafe(registration.Capabilities,
                    (x, y) => x.Key == y.Key && x.Value == y.Value);
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
