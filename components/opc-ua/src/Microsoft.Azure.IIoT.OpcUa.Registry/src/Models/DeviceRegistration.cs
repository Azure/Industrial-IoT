// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Device twin registration
    /// </summary>
    public abstract class DeviceRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public virtual string DeviceId { get; set; }

        /// <summary>
        /// Etag id
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Registration type
        /// </summary>
        public abstract string DeviceType { get; }

        /// <summary>
        /// Whether registration is enabled or not
        /// </summary>
        public virtual bool? IsDisabled { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        public virtual DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Certificate hash
        /// </summary>
        public virtual string Thumbprint { get; set; }

        /// <summary>
        /// The certificate of the endpoint
        /// </summary>
        public virtual Dictionary<string, string> Certificate { get; set; }

        /// <summary>
        /// Site of the registration
        /// </summary>
        public virtual string SiteId { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public virtual string Type { get; set; }

        /// <summary>
        /// Connected
        /// </summary>
        public virtual bool Connected { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as DeviceRegistration;
            return registration != null &&
                DeviceId == registration.DeviceId &&
                DeviceType == registration.DeviceType &&
                SiteId == registration.SiteId &&
                (IsDisabled ?? false) == (registration.IsDisabled ?? false) &&
                NotSeenSince == registration.NotSeenSince &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.Certificate.DecodeAsByteArray());
        }

        /// <inheritdoc/>
        public static bool operator ==(DeviceRegistration r1, DeviceRegistration r2) =>
            EqualityComparer<DeviceRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(DeviceRegistration r1, DeviceRegistration r2) =>
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
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint);
            return hashCode;
        }
    }
}
