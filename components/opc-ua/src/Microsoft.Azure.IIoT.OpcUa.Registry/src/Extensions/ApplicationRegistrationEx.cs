// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Aapplication registration persisted and comparable
    /// </summary>
    public static class ApplicationRegistrationEx {

        /// <summary>
        /// Logical comparison of application registrations
        /// </summary>
        public static IEqualityComparer<ApplicationRegistration> Logical =>
            new LogicalEquality();

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this ApplicationRegistration registration) {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this ApplicationRegistration registration,
            ApplicationRegistration update) {

            var twin = BaseRegistrationEx.PatchBase(registration, update);

            // Tags

            if (update?.ApplicationType != null &&
                update?.ApplicationType != registration?.ApplicationType) {
                twin.Tags.Add(nameof(ApplicationRegistration.ApplicationType),
                    JToken.FromObject(update.ApplicationType));
                twin.Tags.Add(nameof(ApplicationType.Server),
                    update.ApplicationType != ApplicationType.Client);
                twin.Tags.Add(nameof(ApplicationType.Client),
                    update.ApplicationType != ApplicationType.Server &&
                    update.ApplicationType != ApplicationType.DiscoveryServer);
                twin.Tags.Add(nameof(ApplicationType.DiscoveryServer),
                    update.ApplicationType == ApplicationType.DiscoveryServer);
            }

            if (update?.ApplicationUri != registration?.ApplicationUri) {
                twin.Tags.Add(nameof(ApplicationRegistration.ApplicationUri),
                    update?.ApplicationUri);
                twin.Tags.Add(nameof(ApplicationRegistration.ApplicationUriLC),
                    update?.ApplicationUriLC);
            }

            if (update?.RecordId != registration?.RecordId) {
                twin.Tags.Add(nameof(ApplicationRegistration.RecordId),
                    update?.RecordId);
            }

            if (update?.ApplicationName != registration?.ApplicationName) {
                twin.Tags.Add(nameof(ApplicationRegistration.ApplicationName),
                    update?.ApplicationName);
            }

            if (update?.Locale != registration?.Locale) {
                twin.Tags.Add(nameof(ApplicationRegistration.Locale),
                    update?.Locale);
            }

            if (update?.DiscoveryProfileUri != registration?.DiscoveryProfileUri) {
                twin.Tags.Add(nameof(ApplicationRegistration.DiscoveryProfileUri),
                    update?.DiscoveryProfileUri);
            }

            if (update?.GatewayServerUri != registration?.GatewayServerUri) {
                twin.Tags.Add(nameof(ApplicationRegistration.GatewayServerUri),
                    update?.GatewayServerUri);
            }

            if (update?.ProductUri != registration?.ProductUri) {
                twin.Tags.Add(nameof(ApplicationRegistration.ProductUri), update?.ProductUri);
            }

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                registration?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true)) {
                twin.Tags.Add(nameof(ApplicationRegistration.DiscoveryUrls),
                    update?.DiscoveryUrls == null ?
                    null : JToken.FromObject(update.DiscoveryUrls));
            }

            var capsUpdate = update?.Capabilities.DecodeAsSet().SetEqualsSafe(
                registration?.Capabilities?.DecodeAsSet());
            if (!(capsUpdate ?? true)) {
                twin.Tags.Add(nameof(ApplicationRegistration.Capabilities),
                    update?.Capabilities == null ?
                    null : JToken.FromObject(update.Capabilities));
            }

            var namesUpdate = update?.LocalizedNames.DictionaryEqualsSafe(
                registration?.LocalizedNames);
            if (!(namesUpdate ?? true)) {
                twin.Tags.Add(nameof(ApplicationRegistration.LocalizedNames),
                    update?.LocalizedNames == null ?
                    null : JToken.FromObject(update.LocalizedNames));
            }

            var hostsUpdate = update?.HostAddresses.DecodeAsList().SequenceEqualsSafe(
                registration?.HostAddresses?.DecodeAsList());
            if (!(hostsUpdate ?? true)) {
                twin.Tags.Add(nameof(ApplicationRegistration.HostAddresses),
                    update?.HostAddresses == null ?
                    null : JToken.FromObject(update.HostAddresses));
            }

            if (update?.CreateAuthorityId != registration?.CreateAuthorityId) {
                twin.Tags.Add(nameof(ApplicationRegistration.CreateAuthorityId),
                    update?.CreateAuthorityId);
            }
            if (update?.CreateTime != registration?.CreateTime) {
                twin.Tags.Add(nameof(ApplicationRegistration.CreateTime),
                    update?.CreateTime);
            }

            if (update?.UpdateAuthorityId != registration?.UpdateAuthorityId) {
                twin.Tags.Add(nameof(ApplicationRegistration.UpdateAuthorityId),
                    update?.UpdateAuthorityId);
            }
            if (update?.UpdateTime != registration?.UpdateTime) {
                twin.Tags.Add(nameof(ApplicationRegistration.UpdateTime),
                    update?.UpdateTime);
            }

            // Recalculate identity

            var applicationUri = registration?.ApplicationUri;
            if (update?.ApplicationUri != null) {
                applicationUri = update?.ApplicationUri;
            }
            if (applicationUri == null) {
                throw new ArgumentException(nameof(ApplicationRegistration.ApplicationUri));
            }
            var siteOrSupervisorId = registration?.SiteId ?? registration?.SupervisorId;
            if (update?.SupervisorId != null || update?.SiteId != null) {
                siteOrSupervisorId = update?.SiteId ?? update?.SupervisorId;
            }
            var applicationType = registration?.ApplicationType;
            if (update?.ApplicationType != null) {
                applicationType = update?.ApplicationType;
            }

            var applicationId = ApplicationInfoModelEx.CreateApplicationId(
                siteOrSupervisorId, applicationUri, applicationType);
            twin.Tags.Remove(nameof(ApplicationId));
            twin.Tags.Add(nameof(ApplicationId), applicationId);
            twin.Id = applicationId;

            if (registration?.DeviceId != twin.Id) {
                twin.Etag = null; // Force creation of new identity
            }
            return twin;
        }

        /// <summary>
        /// Patch registration
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        public static void Patch(this ApplicationRegistration registration,
            ApplicationRegistrationUpdateModel request) {
            // Patch
            if (!string.IsNullOrEmpty(request.ApplicationName)) {
                registration.ApplicationName = request.ApplicationName;
            }
            if (request.Capabilities != null) {
                registration.Capabilities =
                    request.Capabilities.EncodeAsDictionary();
            }
            if (!string.IsNullOrEmpty(request.DiscoveryProfileUri)) {
                registration.DiscoveryProfileUri = request.DiscoveryProfileUri;
            }
            if (!string.IsNullOrEmpty(request.GatewayServerUri)) {
                registration.GatewayServerUri = request.GatewayServerUri;
            }
            if (!string.IsNullOrEmpty(request.ProductUri)) {
                registration.ProductUri = request.ProductUri;
            }
            if (request.DiscoveryUrls != null) {
                registration.DiscoveryUrls =
                    request.DiscoveryUrls?.ToList().EncodeAsDictionary();
            }
            if (request.LocalizedNames != null) {
                var table = registration.LocalizedNames;
                if (table == null) {
                    table = new Dictionary<string, string>();
                }
                foreach (var item in request.LocalizedNames) {
                    if (item.Value == null) {
                        table.Remove(item.Key);
                    }
                    else {
                        table.AddOrUpdate(item.Key, item.Value);
                    }
                }
                registration.LocalizedNames = table;
            }
            registration.Validate();
        }

        /// <summary>
        /// Validates all fields in an application record to be consistent with
        /// the OPC UA specification.
        /// </summary>
        /// <param name="registration">The application registration</param>
        public static void Validate(this ApplicationRegistration registration) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }

            if (registration.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(registration.ApplicationUri));
            }

            if (!Uri.IsWellFormedUriString(registration.ApplicationUri, UriKind.Absolute)) {
                throw new ArgumentException(registration.ApplicationUri +
                    " is not a valid URI.", nameof(registration.ApplicationUri));
            }

            if ((registration.ApplicationType < ApplicationType.Server) ||
                (registration.ApplicationType > ApplicationType.DiscoveryServer)) {
                throw new ArgumentException(registration.ApplicationType.ToString() +
                    " is not a valid ApplicationType.", nameof(registration.ApplicationType));
            }

            if (string.IsNullOrEmpty(registration.GetApplicationName())) {
                throw new ArgumentException(
                    "At least one ApplicationName must be provided.",
                    nameof(registration.LocalizedNames));
            }

            if (string.IsNullOrEmpty(registration.ProductUri)) {
                throw new ArgumentException(
                    "A ProductUri must be provided.", nameof(registration.ProductUri));
            }

            if (!Uri.IsWellFormedUriString(registration.ProductUri, UriKind.Absolute)) {
                throw new ArgumentException(registration.ProductUri +
                    " is not a valid URI.", nameof(registration.ProductUri));
            }

            if (registration.DiscoveryUrls != null) {
                foreach (var discoveryUrl in registration.DiscoveryUrls.DecodeAsList()) {
                    if (string.IsNullOrEmpty(discoveryUrl)) {
                        continue;
                    }
                    if (!Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute)) {
                        throw new ArgumentException(discoveryUrl + " is not a valid URL.",
                            nameof(registration.DiscoveryUrls));
                    }
                    // TODO: check for https:/hostname:62541, typo is not detected here
                }
            }

            if (registration.ApplicationType != ApplicationType.Client) {
                if (!(registration.DiscoveryUrls.DecodeAsList()?.Any() ?? false)) {
                    throw new ArgumentException(
                        "At least one DiscoveryUrl must be provided.",
                        nameof(registration.DiscoveryUrls));
                }

                if (!registration.Capabilities.Any()) {
                    throw new ArgumentException(
                        "At least one Server Capability must be provided.",
                        nameof(registration.Capabilities));
                }

                // TODO: check for valid servercapabilities
            }
            else {
                if (registration.DiscoveryUrls.DecodeAsList()?.Any() ?? false) {
                    throw new ArgumentException(
                        "DiscoveryUrls must not be specified for clients.",
                        nameof(registration.DiscoveryUrls));
                }
            }
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static ApplicationRegistration ToApplicationRegistration(this DeviceTwinModel twin) {
            if (twin == null) {
                return null;
            }
            var tags = twin.Tags ?? new Dictionary<string, JToken>();
            var registration = new ApplicationRegistration {

                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault(nameof(ApplicationRegistration.IsDisabled), twin.IsDisabled()),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.NotSeenSince), null),
                SiteId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.SiteId), null),

                ApplicationName =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ApplicationName), null),
                Locale =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.Locale), null),
                LocalizedNames =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.LocalizedNames), null),
                ApplicationUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ApplicationUri), null),
                RecordId =
                    tags.GetValueOrDefault<uint>(nameof(ApplicationRegistration.RecordId), null),
                ProductUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ProductUri), null),
                SupervisorId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.SupervisorId), null),
                DiscoveryProfileUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.DiscoveryProfileUri), null),
                GatewayServerUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.GatewayServerUri), null),
                ApplicationId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationId), null),
                ApplicationType =
                    tags.GetValueOrDefault<ApplicationType>(nameof(ApplicationType), null),
                Capabilities =
                    tags.GetValueOrDefault<Dictionary<string, bool>>(nameof(ApplicationRegistration.Capabilities), null),
                HostAddresses =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.HostAddresses), null),
                DiscoveryUrls =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.DiscoveryUrls), null),
                Certificate =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.Certificate), null),
                Thumbprint =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.Thumbprint), null),

                CreateTime =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.CreateTime), null),
                CreateAuthorityId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.CreateAuthorityId), null),
                UpdateTime =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.UpdateTime), null),
                UpdateAuthorityId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.UpdateAuthorityId), null),
            };
            return registration;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static ApplicationRegistration Clone(this ApplicationRegistration registration) {
            if (registration == null) {
                return null;
            }
            return new ApplicationRegistration {
                DeviceId = registration.Id,
                ModuleId = registration.ModuleId,
                Type = registration.Type,
                Etag = registration.Etag,
                IsDisabled = registration.IsDisabled,
                NotSeenSince = registration.NotSeenSince,
                SiteId = registration.SiteId,
                ApplicationName = registration.ApplicationName,
                Locale = registration.Locale,
                LocalizedNames = registration.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                ApplicationUri = registration.ApplicationUri,
                RecordId = registration.RecordId,
                ProductUri = registration.ProductUri,
                SupervisorId = registration.SupervisorId,
                DiscoveryProfileUri = registration.DiscoveryProfileUri,
                GatewayServerUri = registration.GatewayServerUri,
                ApplicationId = registration.ApplicationId,
                ApplicationType = registration.ApplicationType,
                Capabilities = registration.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                HostAddresses = registration.HostAddresses?
                    .ToDictionary(k => k.Key, v => v.Value),
                DiscoveryUrls = registration.DiscoveryUrls?
                    .ToDictionary(k => k.Key, v => v.Value),
                Certificate = registration.Certificate?
                    .ToDictionary(k => k.Key, v => v.Value),
                Thumbprint = registration.Thumbprint,
                CreateTime = registration.CreateTime,
                CreateAuthorityId = registration.CreateAuthorityId,
                UpdateTime = registration.UpdateTime,
                UpdateAuthorityId = registration.UpdateAuthorityId,
                Connected = registration.Connected,
            };
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <param name="etag"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static ApplicationRegistration ToApplicationRegistration(
            this ApplicationInfoModel model, bool? disabled = null, string etag = null,
            uint? recordId = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new ApplicationRegistration {
                IsDisabled = disabled,
                SupervisorId = model.SupervisorId,
                Etag = etag,
                RecordId = recordId,
                SiteId = model.SiteId,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames,
                HostAddresses = model.HostAddresses?.ToList().EncodeAsDictionary(),
                ApplicationType = model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                NotSeenSince = model.NotSeenSince,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                GatewayServerUri = model.GatewayServerUri,
                Capabilities = model.Capabilities.EncodeAsDictionary(true),
                Certificate = model.Certificate.EncodeAsDictionary(),
                Thumbprint = model.Certificate.ToSha1Hash(),
                DiscoveryUrls = model.DiscoveryUrls?.ToList().EncodeAsDictionary(),
                CreateAuthorityId = model.Created?.AuthorityId,
                CreateTime = model.Created?.Time,
                UpdateAuthorityId = model.Updated?.AuthorityId,
                UpdateTime = model.Updated?.Time,
                Connected = false
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static ApplicationInfoModel ToServiceModel(this ApplicationRegistration registration) {
            return new ApplicationInfoModel {
                ApplicationId = registration.ApplicationId,
                ApplicationName = registration.ApplicationName,
                Locale = registration.Locale,
                LocalizedNames = registration.LocalizedNames,
                HostAddresses = registration.HostAddresses.DecodeAsList().ToHashSetSafe(),
                NotSeenSince = registration.NotSeenSince,
                ApplicationType = registration.ApplicationType ?? ApplicationType.Server,
                ApplicationUri = string.IsNullOrEmpty(registration.ApplicationUri) ?
                    registration.ApplicationUriLC : registration.ApplicationUri,
                ProductUri = registration.ProductUri,
                Certificate = registration.Certificate?.DecodeAsByteArray(),
                SiteId = string.IsNullOrEmpty(registration.SiteId) ?
                    null : registration.SiteId,
                SupervisorId = string.IsNullOrEmpty(registration.SupervisorId) ?
                    null : registration.SupervisorId,
                DiscoveryUrls = registration.DiscoveryUrls.DecodeAsList().ToHashSetSafe(),
                DiscoveryProfileUri = registration.DiscoveryProfileUri,
                GatewayServerUri = registration.GatewayServerUri,
                Capabilities = registration.Capabilities?.DecodeAsSet(),
                Created = ToOperationModel(registration.CreateAuthorityId, registration.CreateTime),
                Updated = ToOperationModel(registration.UpdateAuthorityId, registration.UpdateTime),
            };
        }

        /// <summary>
        /// Returns true if this registration matches the application
        /// model provided.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool Matches(this ApplicationRegistration registration,
            ApplicationInfoModel model) {
            return model != null &&
                registration.ApplicationId == model.ApplicationId &&
                registration.ApplicationType == model.ApplicationType &&
                registration.ApplicationUri == model.ApplicationUri &&
                registration.HostAddresses.DecodeAsList().SequenceEqualsSafe(
                    model.HostAddresses) &&
                registration.CreateAuthorityId == model.Created?.AuthorityId &&
                registration.UpdateAuthorityId == model.Updated?.AuthorityId &&
                registration.CreateTime == model.Created?.Time &&
                registration.UpdateTime == model.Updated?.Time &&
                registration.DiscoveryProfileUri == model.DiscoveryProfileUri &&
                registration.GatewayServerUri == model.GatewayServerUri &&
                registration.NotSeenSince == model.NotSeenSince &&
                registration.SupervisorId == model.SupervisorId &&
                registration.SiteId == model.SiteId &&
                registration.Capabilities.DecodeAsSet().SetEqualsSafe(
                    model.Capabilities?.Select(x =>
                        JTokenEx.SanitizePropertyName(x).ToUpperInvariant())) &&
                registration.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    model.DiscoveryUrls) &&
                registration.Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    model.Certificate);
        }

        /// <summary>
        /// Returns application name
        /// </summary>
        /// <param name="registration">The application record.</param>
        public static string GetApplicationName(this ApplicationRegistration registration) {
            if (!string.IsNullOrEmpty(registration.ApplicationName)) {
                return registration.ApplicationName;
            }
            if (registration.LocalizedNames != null &&
                registration.LocalizedNames.Count != 0 &&
                !string.IsNullOrEmpty(registration.LocalizedNames.First().Value)) {
                return registration.LocalizedNames.First().Value;
            }
            return null;
        }

        /// <summary>
        /// Create operation model
        /// </summary>
        /// <param name="authorityId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static RegistryOperationContextModel ToOperationModel(
            string authorityId, DateTime? time) {
            if (string.IsNullOrEmpty(authorityId) && time == null) {
                return null;
            }
            return new RegistryOperationContextModel {
                AuthorityId = authorityId,
                Time = time ?? DateTime.MinValue
            };
        }

        /// <summary>
        /// Compares for logical equality - applications are logically equivalent if they
        /// have the same uri, type, and site location or supervisor that registered.
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
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<ApplicationType?>.Default.GetHashCode(obj.ApplicationType);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.ApplicationUriLC);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.SiteOrSupervisorId);
                return hashCode;
            }
        }
    }
}
