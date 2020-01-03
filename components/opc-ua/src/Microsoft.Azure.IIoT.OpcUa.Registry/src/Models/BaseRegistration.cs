// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Base twin registration
    /// </summary>
    public abstract class BaseRegistration : DeviceRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public virtual string ModuleId { get; set; }

        /// <summary>
        /// Supervisor that owns the twin.
        /// </summary>
        public virtual string SupervisorId { get; set; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        public virtual string ApplicationId { get; set; }

        /// <summary>
        /// Searchable grouping (either supervisor or site id)
        /// </summary>
        public string SiteOrSupervisorId =>
            !string.IsNullOrEmpty(SiteId) ? SiteId : SupervisorId;

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as BaseRegistration;
            return registration != null &&
                base.Equals(registration) &&
                SupervisorId == registration.SupervisorId &&
                ApplicationId == registration.ApplicationId;
        }

        /// <inheritdoc/>
        public static bool operator ==(BaseRegistration r1, BaseRegistration r2) =>
            EqualityComparer<BaseRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(BaseRegistration r1, BaseRegistration r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(SupervisorId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            return hashCode;
        }
    }
}
