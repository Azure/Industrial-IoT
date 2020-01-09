// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Twin supervisor module registration
    /// </summary>
    [Serializable]
    public sealed class SupervisorRegistration : EntityRegistration {

        /// <inheritdoc/>
        public override string DeviceType => IdentityType.Supervisor;

        /// <summary>
        /// Device id for registration
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public SupervisorRegistration(string deviceId = null,
            string moduleId = null) {
            DeviceId = deviceId;
            ModuleId = moduleId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as SupervisorRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                LogLevel == registration.LogLevel;
        }

        /// <inheritdoc/>
        public static bool operator ==(SupervisorRegistration r1,
            SupervisorRegistration r2) => EqualityComparer<SupervisorRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(SupervisorRegistration r1,
            SupervisorRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TraceLogLevel?>.Default.GetHashCode(LogLevel);
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
