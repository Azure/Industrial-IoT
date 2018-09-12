// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Twin (application) registration persisted and comparable
    /// </summary>
    public sealed class ApplicationRegistration : BaseRegistration {

        /// <summary>
        /// Logical comparison of application registrations
        /// </summary>
        public static IEqualityComparer<ApplicationRegistration> Logical =>
            new LogicalEquality();

        /// <inheritdoc/>
        public override string DeviceType => "Application";

        /// <summary>
        /// Connected
        /// </summary>
        public override bool Connected => false;

        /// <summary>
        /// Device id is application id
        /// </summary>
        public override string DeviceId {
            get => base.DeviceId ?? Id;
        }

        /// <summary>
        /// Type
        /// </summary>
        public override string Type {
            get => DeviceType;
        }

        #region Twin Tags

        /// <summary>
        /// Device id is application id
        /// </summary>
        public override string ApplicationId {
            get => base.ApplicationId ?? DeviceId;
        }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Lower case application url
        /// </summary>
        public string ApplicationUriLC => ApplicationUri?.ToLowerInvariant();

        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Returns discovery urls of the application
        /// </summary>
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Host address of server application
        /// </summary>
        public Dictionary<string, string> HostAddresses { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        public Dictionary<string, bool> Capabilities { get; set; }

        #endregion Twin Tags

        /// <summary>
        /// Application registration id
        /// </summary>
        public string Id => ApplicationInfoModelEx.CreateApplicationId(
             SiteOrSupervisorId, ApplicationUri, ApplicationType);

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(
            ApplicationRegistration existing,
            ApplicationRegistration update) {

            var twin = BaseRegistration.Patch(existing, update);

            // Tags

            if (update?.ApplicationType != null &&
                update?.ApplicationType != existing?.ApplicationType) {
                twin.Tags.Add(nameof(ApplicationType),
                    JToken.FromObject(update.ApplicationType));
                twin.Tags.Add(nameof(OpcUa.Models.ApplicationType.Server),
                    update.ApplicationType != OpcUa.Models.ApplicationType.Client);
                twin.Tags.Add(nameof(OpcUa.Models.ApplicationType.Client),
                    update.ApplicationType != OpcUa.Models.ApplicationType.Server);
            }

            if (update?.ApplicationUri != existing?.ApplicationUri) {
                twin.Tags.Add(nameof(ApplicationUri), update?.ApplicationUri);
                twin.Tags.Add(nameof(ApplicationUriLC), update?.ApplicationUriLC);
            }

            if (update?.ApplicationName != existing?.ApplicationName) {
                twin.Tags.Add(nameof(ApplicationName), update?.ApplicationName);
            }

            if (update?.DiscoveryProfileUri != existing?.DiscoveryProfileUri) {
                twin.Tags.Add(nameof(DiscoveryProfileUri), update?.DiscoveryProfileUri);
            }

            if (update?.ProductUri != existing?.ProductUri) {
                twin.Tags.Add(nameof(ProductUri), update?.ProductUri);
            }

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                existing?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true)) {
                twin.Tags.Add(nameof(DiscoveryUrls), update?.DiscoveryUrls == null ?
                    null : JToken.FromObject(update.DiscoveryUrls));
            }

            var capsUpdate = update?.Capabilities.DecodeAsSet().SetEqualsSafe(
                existing?.Capabilities?.DecodeAsSet());
            if (!(capsUpdate ?? true)) {
                twin.Tags.Add(nameof(Capabilities), update?.Capabilities == null ?
                    null : JToken.FromObject(update.Capabilities));
            }

            var hostsUpdate = update?.HostAddresses.DecodeAsList().SequenceEqualsSafe(
                existing?.HostAddresses?.DecodeAsList());
            if (!(hostsUpdate ?? true)) {
                twin.Tags.Add(nameof(HostAddresses), update?.HostAddresses == null ?
                    null : JToken.FromObject(update.HostAddresses));
            }

            // Recalculate identity

            var applicationUri = existing?.ApplicationUri;
            if (update?.ApplicationUri != null) {
                applicationUri = update?.ApplicationUri;
            }
            if (applicationUri == null) {
                throw new ArgumentException(nameof(ApplicationUri));
            }
            var siteOrSupervisorId = existing?.SiteId ?? existing?.SupervisorId;
            if (update?.SupervisorId != null || update?.SiteId != null) {
                siteOrSupervisorId = update?.SiteId ?? update?.SupervisorId;
            }
            var applicationType = existing?.ApplicationType;
            if (update?.ApplicationType != null) {
                applicationType = update?.ApplicationType;
            }

            var applicationId = ApplicationInfoModelEx.CreateApplicationId(
                siteOrSupervisorId, applicationUri, applicationType);
            twin.Tags.Remove(nameof(ApplicationId));
            twin.Tags.Add(nameof(ApplicationId), applicationId);
            twin.Id = applicationId;

            if (existing?.DeviceId != twin.Id) {
                twin.Etag = null; // Force creation of new identity
            }
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="etag"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static ApplicationRegistration FromTwin(string deviceId, string etag,
            Dictionary<string, JToken> tags) {
            var registration = new ApplicationRegistration {

                // Device

                Etag = etag,
                DeviceId = deviceId,

                // Tags

                IsDisabled =
                    tags.Get<bool>(nameof(IsDisabled), null),
                NotSeenSince =
                    tags.Get<DateTime>(nameof(NotSeenSince), null),
                SiteId =
                    tags.Get<string>(nameof(SiteId), null),

                ApplicationName =
                    tags.Get<string>(nameof(ApplicationName), null),
                ApplicationUri =
                    tags.Get<string>(nameof(ApplicationUri), null),
                ProductUri =
                    tags.Get<string>(nameof(ProductUri), null),
                SupervisorId =
                    tags.Get<string>(nameof(SupervisorId), null),
                DiscoveryProfileUri =
                    tags.Get<string>(nameof(DiscoveryProfileUri), null),
                ApplicationId =
                    tags.Get<string>(nameof(ApplicationId), null),
                ApplicationType =
                    tags.Get<ApplicationType>(nameof(ApplicationType), null),
                Capabilities =
                    tags.Get<Dictionary<string, bool>>(nameof(Capabilities), null),
                HostAddresses =
                    tags.Get<Dictionary<string, string>>(nameof(HostAddresses), null),
                DiscoveryUrls =
                    tags.Get<Dictionary<string, string>>(nameof(DiscoveryUrls), null),
                Certificate =
                    tags.Get<Dictionary<string, string>>(nameof(Certificate), null),
                Thumbprint =
                    tags.Get<string>(nameof(Thumbprint), null),
            };
            return registration;
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static ApplicationRegistration FromTwin(DeviceTwinModel twin) {
            if (twin == null) {
                return null;
            }
            return FromTwin(twin.Id, twin.Etag,
                twin.Tags ?? new Dictionary<string, JToken>());
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <returns></returns>
        public static ApplicationRegistration FromServiceModel(
            ApplicationInfoModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new ApplicationRegistration {
                IsDisabled = disabled,
                SupervisorId = model.SupervisorId,
                SiteId = model.SiteId,
                ApplicationName = model.ApplicationName,
                HostAddresses = model.HostAddresses?.ToList().EncodeAsDictionary(),
                ApplicationType = model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                NotSeenSince = model.NotSeenSince,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                Capabilities = model.Capabilities.EncodeAsDictionary(true),
                Certificate = model.Certificate.EncodeAsDictionary(),
                Thumbprint = model.Certificate.ToSha1Hash(),
                DiscoveryUrls = model.DiscoveryUrls?.ToList().EncodeAsDictionary(),
                Connected = false
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationInfoModel ToServiceModel() {
            return new ApplicationInfoModel {
                ApplicationId = ApplicationId,
                ApplicationName = ApplicationName,
                HostAddresses = HostAddresses.DecodeAsList().ToHashSetSafe(),
                NotSeenSince = NotSeenSince,
                ApplicationType = ApplicationType ?? OpcUa.Models.ApplicationType.Server,
                ApplicationUri = string.IsNullOrEmpty(ApplicationUri) ?
                    ApplicationUriLC : ApplicationUri,
                ProductUri = ProductUri,
                Certificate = Certificate?.DecodeAsByteArray(),
                SiteId = string.IsNullOrEmpty(SiteId) ?
                    null : SiteId,
                SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                    null : SupervisorId,
                DiscoveryUrls = DiscoveryUrls.DecodeAsList().ToHashSetSafe(),
                DiscoveryProfileUri = DiscoveryProfileUri,
                Capabilities = Capabilities?.DecodeAsSet()
            };
        }

        /// <summary>
        /// Returns true if this registration matches the application
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(ApplicationInfoModel model) {
            return model != null &&
                ApplicationId == model.ApplicationId &&
                ApplicationType == model.ApplicationType &&
                ApplicationUri == model.ApplicationUri &&
                HostAddresses.DecodeAsList().SequenceEqualsSafe(
                    model.HostAddresses) &&
                DiscoveryProfileUri == model.DiscoveryProfileUri &&
                NotSeenSince == model.NotSeenSince &&
                SupervisorId == model.SupervisorId &&
                SiteId == model.SiteId &&
                Capabilities.DecodeAsSet().SetEqualsSafe(
                    model.Capabilities?.Select(x => x.SanitizePropertyName().ToUpperInvariant())) &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    model.DiscoveryUrls) &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    model.Certificate);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as ApplicationRegistration;
            return base.Equals(registration) &&
                ApplicationType == registration.ApplicationType &&
                ApplicationUriLC == registration.ApplicationUriLC &&
                DiscoveryProfileUri == registration.DiscoveryProfileUri &&
                ProductUri == registration.ProductUri &&
                HostAddresses.DecodeAsList().SequenceEqualsSafe(
                    registration.HostAddresses.DecodeAsList()) &&
                ApplicationName == registration.ApplicationName &&
                Capabilities.DecodeAsSet().SetEqualsSafe(
                    registration.Capabilities.DecodeAsSet()) &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryUrls.DecodeAsList());
        }

        /// <inheritdoc/>
        public static bool operator ==(ApplicationRegistration r1,
            ApplicationRegistration r2) =>
            EqualityComparer<ApplicationRegistration>.Default.Equals(r1, r2);
        /// <inheritdoc/>
        public static bool operator !=(ApplicationRegistration r1,
            ApplicationRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<ApplicationType?>.Default.GetHashCode(ApplicationType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ProductUri);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(DiscoveryProfileUri);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationName);
            return hashCode;
        }

        /// <summary>
        /// Compares for logical equality - applications are logically equivalent if they
        /// have the same site location or were registered by the same supervisor.
        /// </summary>
        private class LogicalEquality : IEqualityComparer<ApplicationRegistration> {
            /// <inheritdoc />
            public bool Equals(ApplicationRegistration x, ApplicationRegistration y) {
                return
                    x.SiteOrSupervisorId == y.SiteOrSupervisorId &&
                    x.ApplicationType == y.ApplicationType &&
                    x.ApplicationUriLC == y.ApplicationUriLC;
            }

            /// <inheritdoc />
            public int GetHashCode(ApplicationRegistration obj) {
                var hashCode = 1200389859;
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<ApplicationType?>.Default.GetHashCode(obj.ApplicationType);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(obj.ApplicationUriLC);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(obj.SiteOrSupervisorId);
                return hashCode;
            }
        }
    }
}
