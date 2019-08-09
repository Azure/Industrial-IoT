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

    /// <summary>
    /// Endpoint registration extensions
    /// </summary>
    public static class SupervisorRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this SupervisorRegistration registration) {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this SupervisorRegistration existing,
            SupervisorRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(SupervisorRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(SupervisorRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrSupervisorId != existing?.SiteOrSupervisorId) {
                twin.Tags.Add(nameof(SupervisorRegistration.SiteOrSupervisorId),
                    update?.SiteOrSupervisorId);
            }

            var cbUpdate = update?.DiscoveryCallbacks?.DecodeAsList()?.SetEqualsSafe(
                existing?.DiscoveryCallbacks?.DecodeAsList(),
                    (callback1, callback2) => callback1.IsSameAs(callback2));
            if (!(cbUpdate ?? true)) {
                twin.Tags.Add(nameof(SupervisorRegistration.DiscoveryCallbacks),
                    update?.DiscoveryCallbacks == null ?
                    null : JToken.FromObject(update.DiscoveryCallbacks));
            }

            var policiesUpdate = update?.SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.SecurityPoliciesFilter?.DecodeAsList());
            if (!(policiesUpdate ?? true)) {
                twin.Tags.Add(nameof(SupervisorRegistration.SecurityPoliciesFilter),
                    update?.SecurityPoliciesFilter == null ?
                    null : JToken.FromObject(update.SecurityPoliciesFilter));
            }

            var trustListUpdate = update?.TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.TrustListsFilter?.DecodeAsList());
            if (!(trustListUpdate ?? true)) {
                twin.Tags.Add(nameof(SupervisorRegistration.TrustListsFilter),
                    update?.TrustListsFilter == null ?
                    null : JToken.FromObject(update.TrustListsFilter));
            }

            if (update?.SecurityModeFilter != existing?.SecurityModeFilter) {
                twin.Tags.Add(nameof(SupervisorRegistration.SecurityModeFilter),
                    JToken.FromObject(update?.SecurityModeFilter));
            }

            // Settings

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                existing?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.DiscoveryUrls),
                    update?.DiscoveryUrls == null ?
                    null : JToken.FromObject(update.DiscoveryUrls));
            }

            var certUpdate = update?.Certificate?.DecodeAsByteArray()?.SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.Certificate),
                    update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
                twin.Tags.Add(nameof(SupervisorRegistration.Thumbprint),
                    update?.Certificate?.DecodeAsByteArray()?.ToSha1Hash());
            }

            var localesUpdate = update?.Locales?.DecodeAsList()?.SequenceEqualsSafe(
                existing?.Locales?.DecodeAsList());
            if (!(localesUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.Locales),
                    update?.Locales == null ?
                    null : JToken.FromObject(update.Locales));
            }

            if (update?.Discovery != existing?.Discovery) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.Discovery),
                    JToken.FromObject(update?.Discovery));
            }

            if (update?.AddressRangesToScan != existing?.AddressRangesToScan) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.AddressRangesToScan),
                    update?.AddressRangesToScan);
            }

            if (update?.NetworkProbeTimeout != existing?.NetworkProbeTimeout) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.NetworkProbeTimeout),
                    update?.NetworkProbeTimeout);
            }

            if (update?.LogLevel != existing?.LogLevel) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.LogLevel),
                    update?.LogLevel == null ?
                    null : JToken.FromObject(update.LogLevel));
            }

            if (update?.MaxNetworkProbes != existing?.MaxNetworkProbes) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.MaxNetworkProbes),
                    update?.MaxNetworkProbes);
            }

            if (update?.PortRangesToScan != existing?.PortRangesToScan) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.PortRangesToScan),
                    update?.PortRangesToScan);
            }

            if (update?.PortProbeTimeout != existing?.PortProbeTimeout) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.PortProbeTimeout),
                    update?.PortProbeTimeout);
            }

            if (update?.MaxPortProbes != existing?.MaxPortProbes) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.MaxPortProbes),
                    update?.MaxPortProbes);
            }

            if (update?.IdleTimeBetweenScans != existing?.IdleTimeBetweenScans) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.IdleTimeBetweenScans),
                    update?.IdleTimeBetweenScans);
            }

            if (update?.MinPortProbesPercent != existing?.MinPortProbesPercent) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.MinPortProbesPercent),
                    update?.MinPortProbesPercent);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.kSiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(SupervisorRegistration.DeviceType), update?.DeviceType);
            twin.Id = update?.DeviceId ?? existing?.DeviceId;
            twin.ModuleId = update?.ModuleId ?? existing?.ModuleId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static SupervisorRegistration ToSupervisorRegistration(this DeviceTwinModel twin,
            Dictionary<string, JToken> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, JToken>();
            var connected = twin.IsConnected();

            var registration = new SupervisorRegistration {
                // Device

                DeviceId = twin.Id,
                ModuleId = twin.ModuleId,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault<bool>(nameof(SupervisorRegistration.IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(SupervisorRegistration.NotSeenSince), null),
                Thumbprint =
                    tags.GetValueOrDefault<string>(nameof(SupervisorRegistration.Thumbprint), null),
                DiscoveryCallbacks =
                    tags.GetValueOrDefault<Dictionary<string, CallbackModel>>(nameof(SupervisorRegistration.DiscoveryCallbacks), null),
                SecurityModeFilter =
                    tags.GetValueOrDefault<SecurityMode>(nameof(SupervisorRegistration.SecurityModeFilter), null),
                SecurityPoliciesFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(SupervisorRegistration.SecurityPoliciesFilter), null),
                TrustListsFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(SupervisorRegistration.TrustListsFilter), null),

                // Properties

                Certificate =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(SupervisorRegistration.Certificate), null),
                LogLevel =
                    properties.GetValueOrDefault<SupervisorLogLevel>(nameof(SupervisorRegistration.LogLevel), null),
                Discovery =
                    properties.GetValueOrDefault(nameof(SupervisorRegistration.Discovery), DiscoveryMode.Off),
                AddressRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(SupervisorRegistration.AddressRangesToScan), null),
                NetworkProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(SupervisorRegistration.NetworkProbeTimeout), null),
                MaxNetworkProbes =
                    properties.GetValueOrDefault<int>(nameof(SupervisorRegistration.MaxNetworkProbes), null),
                PortRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(SupervisorRegistration.PortRangesToScan), null),
                PortProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(SupervisorRegistration.PortProbeTimeout), null),
                MaxPortProbes =
                    properties.GetValueOrDefault<int>(nameof(SupervisorRegistration.MaxPortProbes), null),
                MinPortProbesPercent =
                    properties.GetValueOrDefault<int>(nameof(SupervisorRegistration.MinPortProbesPercent), null),
                IdleTimeBetweenScans =
                    properties.GetValueOrDefault<TimeSpan>(nameof(SupervisorRegistration.IdleTimeBetweenScans), null),
                DiscoveryUrls =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(SupervisorRegistration.DiscoveryUrls), null),
                Locales =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(SupervisorRegistration.Locales), null),

                SiteId =
                    properties.GetValueOrDefault<string>(TwinProperty.kSiteId, null),
                Connected = connected ??
                    properties.GetValueOrDefault(TwinProperty.kConnected, false),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.kType, null)
            };
            return registration;
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static SupervisorRegistration ToSupervisorRegistration(this DeviceTwinModel twin,
            bool onlyServerState) {
            return ToSupervisorRegistration(twin, onlyServerState, out var tmp);
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="connected"></param>
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        public static SupervisorRegistration ToSupervisorRegistration(this DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated =
                ToSupervisorRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                ToSupervisorRegistration(twin, twin.Properties.Desired);

            connected = consolidated.Connected;
            if (desired != null) {
                desired.Connected = connected;
                if (desired.SiteId == null && consolidated.SiteId != null) {
                    // Not set by user, but by config, so fake user desiring it.
                    desired.SiteId = consolidated.SiteId;
                }
                if (desired.LogLevel == null && consolidated.LogLevel != null) {
                    // Not set by user, but reported, so set as desired
                    desired.LogLevel = consolidated.LogLevel;
                }
            }

            if (!onlyServerState) {
                consolidated._isInSync = consolidated.IsInSyncWith(desired);
                return consolidated;
            }
            if (desired != null) {
                desired._isInSync = desired.IsInSyncWith(consolidated);
            }
            return desired;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        public static SupervisorRegistration ToSupervisorRegistration(
            this SupervisorModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(model.Id,
                out var moduleId);
            return new SupervisorRegistration {
                IsDisabled = disabled,
                SupervisorId = model.Id,
                DeviceId = deviceId,
                ModuleId = moduleId,
                LogLevel = model.LogLevel,
                Discovery = model.Discovery ?? DiscoveryMode.Off,
                AddressRangesToScan = model.DiscoveryConfig?.AddressRangesToScan,
                NetworkProbeTimeout = model.DiscoveryConfig?.NetworkProbeTimeout,
                MaxNetworkProbes = model.DiscoveryConfig?.MaxNetworkProbes,
                PortRangesToScan = model.DiscoveryConfig?.PortRangesToScan,
                PortProbeTimeout = model.DiscoveryConfig?.PortProbeTimeout,
                MaxPortProbes = model.DiscoveryConfig?.MaxPortProbes,
                IdleTimeBetweenScans = model.DiscoveryConfig?.IdleTimeBetweenScans,
                MinPortProbesPercent = model.DiscoveryConfig?.MinPortProbesPercent,
                Certificate = model.Certificate?.EncodeAsDictionary(),
                DiscoveryCallbacks = model.DiscoveryConfig?.Callbacks.
                    EncodeAsDictionary(),
                SecurityModeFilter = model.DiscoveryConfig?.ActivationFilter?.
                    SecurityMode,
                TrustListsFilter = model.DiscoveryConfig?.ActivationFilter?.
                    TrustLists.EncodeAsDictionary(),
                SecurityPoliciesFilter = model.DiscoveryConfig?.ActivationFilter?.
                    SecurityPolicies.EncodeAsDictionary(),
                DiscoveryUrls = model.DiscoveryConfig?.DiscoveryUrls?.
                    EncodeAsDictionary(),
                Locales = model.DiscoveryConfig?.Locales?.
                    EncodeAsDictionary(),
                Connected = model.Connected ?? false,
                Thumbprint = model.Certificate?.ToSha1Hash(),
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static SupervisorModel ToServiceModel(this SupervisorRegistration registration) {
            return new SupervisorModel {
                Discovery = registration.Discovery != DiscoveryMode.Off ?
                    registration.Discovery : (DiscoveryMode?)null,
                Id = registration.SupervisorId,
                SiteId = registration.SiteId,
                Certificate = registration.Certificate?.DecodeAsByteArray(),
                LogLevel = registration.LogLevel,
                DiscoveryConfig = registration.ToConfigModel(),
                Connected = registration.IsConnected() ? true : (bool?)null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : (bool?)null
            };
        }


        /// <summary>
        /// Returns if no discovery config specified
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static bool IsNullConfig(this SupervisorRegistration registration) {
            if (string.IsNullOrEmpty(registration.AddressRangesToScan) &&
                string.IsNullOrEmpty(registration.PortRangesToScan) &&
                registration.MaxNetworkProbes == null &&
                registration.NetworkProbeTimeout == null &&
                registration.MaxPortProbes == null &&
                registration.MinPortProbesPercent == null &&
                registration.PortProbeTimeout == null &&
                (registration.DiscoveryCallbacks == null || registration.DiscoveryCallbacks.Count == 0) &&
                (registration.DiscoveryUrls == null || registration.DiscoveryUrls.Count == 0) &&
                (registration.Locales == null || registration.Locales.Count == 0) &&
                registration.IdleTimeBetweenScans == null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns config model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static DiscoveryConfigModel ToConfigModel(this SupervisorRegistration registration) {
            return registration.IsNullConfig() ? null : new DiscoveryConfigModel {
                AddressRangesToScan = registration.AddressRangesToScan,
                PortRangesToScan = registration.PortRangesToScan,
                MaxNetworkProbes = registration.MaxNetworkProbes,
                NetworkProbeTimeout = registration.NetworkProbeTimeout,
                MaxPortProbes = registration.MaxPortProbes,
                MinPortProbesPercent = registration.MinPortProbesPercent,
                PortProbeTimeout = registration.PortProbeTimeout,
                IdleTimeBetweenScans = registration.IdleTimeBetweenScans,
                Callbacks = registration.DiscoveryCallbacks?.DecodeAsList(),
                DiscoveryUrls = registration.DiscoveryUrls?.DecodeAsList(),
                Locales = registration.Locales?.DecodeAsList(),
                ActivationFilter = registration.ToFilterModel()
            };
        }

        /// <summary>
        /// Returns if no activation filter specified
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static bool IsNullFilter(this SupervisorRegistration registration) {
            if (registration.SecurityModeFilter == null &&
                (registration.TrustListsFilter == null || registration.TrustListsFilter.Count == 0) &&
                (registration.SecurityPoliciesFilter == null || registration.SecurityPoliciesFilter.Count == 0)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns activation filter model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static EndpointActivationFilterModel ToFilterModel(this SupervisorRegistration registration) {
            return registration.IsNullFilter() ? null : new EndpointActivationFilterModel {
                SecurityMode = registration.SecurityModeFilter,
                SecurityPolicies = registration.SecurityPoliciesFilter.DecodeAsList(),
                TrustLists = registration.TrustListsFilter.DecodeAsList()
            };
        }


        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this SupervisorRegistration registration,
            SupervisorRegistration other) {
            return
                other != null &&
                registration.SiteId == other.SiteId &&
                registration.LogLevel == other.LogLevel &&
                registration.Discovery == other.Discovery &&
                registration.AddressRangesToScan == other.AddressRangesToScan &&
                registration.PortRangesToScan == other.PortRangesToScan &&
                registration.MaxNetworkProbes == other.MaxNetworkProbes &&
                registration.NetworkProbeTimeout == other.NetworkProbeTimeout &&
                registration.MaxPortProbes == other.MaxPortProbes &&
                registration.MinPortProbesPercent == other.MinPortProbesPercent &&
                registration.PortProbeTimeout == other.PortProbeTimeout &&
                registration.IdleTimeBetweenScans == other.IdleTimeBetweenScans &&
                registration.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    other.DiscoveryUrls.DecodeAsList()) &&
                registration.Locales.DecodeAsList().SequenceEqualsSafe(
                    other.Locales.DecodeAsList());
        }
    }
}
