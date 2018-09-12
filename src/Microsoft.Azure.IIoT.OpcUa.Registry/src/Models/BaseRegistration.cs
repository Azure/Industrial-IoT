// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base twin registration
    /// </summary>
    public abstract class BaseRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public virtual string DeviceId { get; set; }

        /// <summary>
        /// Device id for registration
        /// </summary>
        public virtual string ModuleId { get; set; }

        /// <summary>
        /// Etag id
        /// </summary>
        public string Etag { get; set; }

        #region Twin Tags

        /// <summary>
        /// Registration type
        /// </summary>
        public abstract string DeviceType { get; }

        /// <summary>
        /// Supervisor that owns the twin.
        /// </summary>
        public virtual string SupervisorId { get; set; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        public virtual string ApplicationId { get; set; }

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
        public string Thumbprint { get; set; }

        #endregion Twin Tags

        #region Twin Tags or properties

        /// <summary>
        /// The certificate of the twin
        /// </summary>
        public Dictionary<string, string> Certificate { get; set; }

        /// <summary>
        /// Site of the twin
        /// </summary>
        public virtual string SiteId { get; set; }

        /// <summary>
        /// Searchable grouping (either supervisor or site id)
        /// </summary>
        public string SiteOrSupervisorId =>
            !string.IsNullOrEmpty(SiteId) ? SiteId : SupervisorId;

        #endregion Twin Tags or reported properties

        #region Properties

        /// <summary>
        /// Type
        /// </summary>
        public virtual string Type { get; set; }

        /// <summary>
        /// Connected
        /// </summary>
        public virtual bool Connected { get; set; }

        #endregion Properties

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        protected static DeviceTwinModel Patch(
            BaseRegistration existing, BaseRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            // Tags

            if (update?.ApplicationId != null &&
                update.ApplicationId != existing?.ApplicationId) {
                twin.Tags.Add(nameof(ApplicationId), update.ApplicationId);
            }

            if (update?.IsDisabled != null &&
                update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrSupervisorId != existing?.SiteOrSupervisorId) {
                twin.Tags.Add(nameof(SiteOrSupervisorId), update?.SiteOrSupervisorId);
            }

            if (update?.SupervisorId != existing?.SupervisorId) {
                twin.Tags.Add(nameof(SupervisorId), update?.SupervisorId);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Tags.Add(nameof(SiteId), update?.SiteId);
            }

            var certUpdate = update?.Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Tags.Add(nameof(Certificate), update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
                twin.Tags.Add(nameof(Thumbprint),
                    update?.Certificate?.DecodeAsByteArray()?.ToSha1Hash());
            }

            twin.Tags.Add(nameof(DeviceType), update?.DeviceType);
            return twin;
        }

        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static BaseRegistration ToRegistration(DeviceTwinModel twin) {
            if (twin == null || twin.Tags == null) {
                return null;
            }
            var type = twin.Tags.Get<string>(nameof(DeviceType), null);
            if (string.IsNullOrEmpty(type) && twin.Properties.Reported != null) {
                type = twin.Properties.Reported.Get<string>("type", null);
            }
            switch (type?.ToLowerInvariant() ?? "") {
                case "endpoint":
                    return EndpointRegistration.FromTwin(twin, false);
                case "application":
                    return ApplicationRegistration.FromTwin(twin);
                case "supervisor":
                    return SupervisorRegistration.FromTwin(twin);
            }
            return null;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as BaseRegistration;
            return registration != null &&
                DeviceId == registration.DeviceId &&
                DeviceType == registration.DeviceType &&
                SupervisorId == registration.SupervisorId &&
                SiteId == registration.SiteId &&
                ApplicationId == registration.ApplicationId &&
                (IsDisabled ?? false) == (registration.IsDisabled ?? false) &&
                NotSeenSince == registration.NotSeenSince &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.Certificate.DecodeAsByteArray());
        }

        /// <inheritdoc/>
        public static bool operator ==(BaseRegistration r1, BaseRegistration r2) =>
            EqualityComparer<BaseRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(BaseRegistration r1, BaseRegistration r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 479558466;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(DeviceId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(DeviceType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SupervisorId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SiteId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<bool>.Default.GetHashCode(IsDisabled ?? false);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<DateTime?>.Default.GetHashCode(NotSeenSince);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint);
            return hashCode;
        }

        /// <summary>Connected property name constant</summary>
        public const string kConnectedProp = "__connected__";
        /// <summary>Type property name constant</summary>
        public const string kTypeProp = "__type__";
        /// <summary>Site id property name constant</summary>
        public const string kSiteIdProp = "__siteid__";
    }
}
