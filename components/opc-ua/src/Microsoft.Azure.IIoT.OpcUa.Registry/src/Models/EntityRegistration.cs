// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Device twin registration
    /// </summary>
    [DataContract]
    public abstract class EntityRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        [DataMember]
        public virtual string DeviceId { get; set; }

        /// <summary>
        /// Site of the registration
        /// </summary>
        [DataMember]
        public virtual string SiteId { get; set; }

        /// <summary>
        /// Searchable grouping (either device or site id)
        /// </summary>
        [DataMember]
        public virtual string SiteOrGatewayId =>
            !string.IsNullOrEmpty(SiteId) ? SiteId : DeviceId;

        /// <summary>
        /// Etag id
        /// </summary>
        [DataMember(Name = "_etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Registration type
        /// </summary>
        [DataMember]
        public abstract string DeviceType { get; }

        /// <summary>
        /// Whether registration is enabled or not
        /// </summary>
        [DataMember]
        public virtual bool? IsDisabled { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        [DataMember]
        public virtual DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Reported Type
        /// </summary>
        [DataMember]
        public virtual string Type { get; set; }

        /// <summary>
        /// Connected
        /// </summary>
        [DataMember]
        public virtual bool Connected { get; set; }

        /// <summary>
        /// Reported version of the entity
        /// </summary>
        [DataMember]
        public string Version { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is EntityRegistration registration)) {
                return false;
            }
            if (DeviceId != registration.DeviceId) {
                return false;
            }
            if (DeviceType != registration.DeviceType) {
                return false;
            }
            if (SiteId != registration.SiteId) {
                return false;
            }
            if ((IsDisabled ?? false) != (registration.IsDisabled ?? false)) {
                return false;
            }
            if (NotSeenSince != registration.NotSeenSince) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(EntityRegistration r1, EntityRegistration r2) =>
            EqualityComparer<EntityRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(EntityRegistration r1, EntityRegistration r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 479558466;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DeviceId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DeviceType);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(SiteId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<bool>.Default.GetHashCode(IsDisabled ?? false);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<DateTime?>.Default.GetHashCode(NotSeenSince);
            return hashCode;
        }
    }
}
