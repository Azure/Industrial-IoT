// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discoverer registration extensions
    /// </summary>
    public static class DiscovererRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(
            this DiscovererRegistration registration, IJsonSerializer serializer) {
            return Patch(null, registration, serializer);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="serializer"></param>
        public static DeviceTwinModel Patch(this DiscovererRegistration existing,
            DiscovererRegistration update, IJsonSerializer serializer) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, VariantValue>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, VariantValue>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(DiscovererRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(DiscovererRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrGatewayId != existing?.SiteOrGatewayId) {
                twin.Tags.Add(nameof(DiscovererRegistration.SiteOrGatewayId),
                    update?.SiteOrGatewayId);
            }

            var policiesUpdate = update?.SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.SecurityPoliciesFilter?.DecodeAsList());
            if (!(policiesUpdate ?? true)) {
                twin.Tags.Add(nameof(DiscovererRegistration.SecurityPoliciesFilter),
                    update?.SecurityPoliciesFilter == null ?
                    null : serializer.FromObject(update.SecurityPoliciesFilter));
            }

            var trustListUpdate = update?.TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.TrustListsFilter?.DecodeAsList());
            if (!(trustListUpdate ?? true)) {
                twin.Tags.Add(nameof(DiscovererRegistration.TrustListsFilter),
                    update?.TrustListsFilter == null ?
                    null : serializer.FromObject(update.TrustListsFilter));
            }

            if (update?.SecurityModeFilter != existing?.SecurityModeFilter) {
                twin.Tags.Add(nameof(DiscovererRegistration.SecurityModeFilter),
                    update?.SecurityModeFilter == null ?
                    null : serializer.FromObject(update.SecurityModeFilter.ToString()));
            }

            // Settings

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                existing?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.DiscoveryUrls),
                    update?.DiscoveryUrls == null ?
                    null : serializer.FromObject(update.DiscoveryUrls));
            }

            var localesUpdate = update?.Locales?.DecodeAsList()?.SequenceEqualsSafe(
                existing?.Locales?.DecodeAsList());
            if (!(localesUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.Locales),
                    update?.Locales == null ?
                    null : serializer.FromObject(update.Locales));
            }

            if (update?.Discovery != existing?.Discovery) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.Discovery),
                    serializer.FromObject(update?.Discovery.ToString()));
            }

            if (update?.AddressRangesToScan != existing?.AddressRangesToScan) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.AddressRangesToScan),
                    update?.AddressRangesToScan);
            }

            if (update?.NetworkProbeTimeout != existing?.NetworkProbeTimeout) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.NetworkProbeTimeout),
                    update?.NetworkProbeTimeout);
            }

            if (update?.LogLevel != existing?.LogLevel) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.LogLevel),
                    update?.LogLevel == null ?
                    null : serializer.FromObject(update.LogLevel.ToString()));
            }

            if (update?.MaxNetworkProbes != existing?.MaxNetworkProbes) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.MaxNetworkProbes),
                    update?.MaxNetworkProbes);
            }

            if (update?.PortRangesToScan != existing?.PortRangesToScan) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.PortRangesToScan),
                    update?.PortRangesToScan);
            }

            if (update?.PortProbeTimeout != existing?.PortProbeTimeout) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.PortProbeTimeout),
                    update?.PortProbeTimeout);
            }

            if (update?.MaxPortProbes != existing?.MaxPortProbes) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.MaxPortProbes),
                    update?.MaxPortProbes);
            }

            if (update?.IdleTimeBetweenScans != existing?.IdleTimeBetweenScans) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.IdleTimeBetweenScans),
                    update?.IdleTimeBetweenScans);
            }

            if (update?.MinPortProbesPercent != existing?.MinPortProbesPercent) {
                twin.Properties.Desired.Add(nameof(DiscovererRegistration.MinPortProbesPercent),
                    update?.MinPortProbesPercent);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.SiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(DiscovererRegistration.DeviceType), update?.DeviceType);
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
        public static DiscovererRegistration ToDiscovererRegistration(this DeviceTwinModel twin,
            Dictionary<string, VariantValue> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();

            var registration = new DiscovererRegistration {
                // Device

                DeviceId = twin.Id,
                ModuleId = twin.ModuleId,
                Etag = twin.Etag,
                Connected = twin.IsConnected() ?? false,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault<bool>(nameof(DiscovererRegistration.IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(DiscovererRegistration.NotSeenSince), null),
                SecurityModeFilter =
                    tags.GetValueOrDefault<SecurityMode>(nameof(DiscovererRegistration.SecurityModeFilter), null),
                SecurityPoliciesFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(DiscovererRegistration.SecurityPoliciesFilter), null),
                TrustListsFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(DiscovererRegistration.TrustListsFilter), null),

                // Properties

                LogLevel =
                    properties.GetValueOrDefault<TraceLogLevel>(nameof(DiscovererRegistration.LogLevel), null),
                Discovery =
                    properties.GetValueOrDefault(nameof(DiscovererRegistration.Discovery), DiscoveryMode.Off),
                AddressRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(DiscovererRegistration.AddressRangesToScan), null),
                NetworkProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(DiscovererRegistration.NetworkProbeTimeout), null),
                MaxNetworkProbes =
                    properties.GetValueOrDefault<int>(nameof(DiscovererRegistration.MaxNetworkProbes), null),
                PortRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(DiscovererRegistration.PortRangesToScan), null),
                PortProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(DiscovererRegistration.PortProbeTimeout), null),
                MaxPortProbes =
                    properties.GetValueOrDefault<int>(nameof(DiscovererRegistration.MaxPortProbes), null),
                MinPortProbesPercent =
                    properties.GetValueOrDefault<int>(nameof(DiscovererRegistration.MinPortProbesPercent), null),
                IdleTimeBetweenScans =
                    properties.GetValueOrDefault<TimeSpan>(nameof(DiscovererRegistration.IdleTimeBetweenScans), null),
                DiscoveryUrls =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(DiscovererRegistration.DiscoveryUrls), null),
                Locales =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(DiscovererRegistration.Locales), null),

                SiteId =
                    properties.GetValueOrDefault<string>(TwinProperty.SiteId, null),
                Version =
                    properties.GetValueOrDefault<string>(TwinProperty.Version, null),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.Type, null)
            };
            return registration;
        }

        /// <summary>
        /// Get discoverer registration from twin
        /// </summary>
        /// <param name="onlyServerState"></param>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static DiscovererRegistration ToDiscovererRegistration(this DeviceTwinModel twin, bool onlyServerState = false) {
            return ToDiscovererRegistration(twin, onlyServerState, out var tmp);
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="connected"></param>
        /// <returns></returns>
        public static DiscovererRegistration ToDiscovererRegistration(this DeviceTwinModel twin,
             bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, VariantValue>();
            }

            var consolidated =
                ToDiscovererRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                ToDiscovererRegistration(twin, twin.Properties.Desired);

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
                desired.Version = consolidated.Version;
            }

            if (onlyServerState) {
                consolidated = desired;
            }

            consolidated._isInSync = consolidated.IsInSyncWith(desired);
            consolidated._desired = desired;
            return consolidated;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        public static DiscovererRegistration ToDiscovererRegistration(
            this DiscovererModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = DiscovererModelEx.ParseDeviceId(model.Id,
                out var moduleId);
            return new DiscovererRegistration {
                IsDisabled = disabled,
                DeviceId = deviceId,
                ModuleId = moduleId,
                LogLevel = model.LogLevel,
                Discovery = model.RequestedMode ?? DiscoveryMode.Off,
                AddressRangesToScan = model.RequestedConfig?.AddressRangesToScan,
                NetworkProbeTimeout = model.RequestedConfig?.NetworkProbeTimeout,
                MaxNetworkProbes = model.RequestedConfig?.MaxNetworkProbes,
                PortRangesToScan = model.RequestedConfig?.PortRangesToScan,
                PortProbeTimeout = model.RequestedConfig?.PortProbeTimeout,
                MaxPortProbes = model.RequestedConfig?.MaxPortProbes,
                IdleTimeBetweenScans = model.RequestedConfig?.IdleTimeBetweenScans,
                MinPortProbesPercent = model.RequestedConfig?.MinPortProbesPercent,
                SecurityModeFilter = model.RequestedConfig?.ActivationFilter?.
                    SecurityMode,
                TrustListsFilter = model.RequestedConfig?.ActivationFilter?.
                    TrustLists.EncodeAsDictionary(),
                SecurityPoliciesFilter = model.RequestedConfig?.ActivationFilter?.
                    SecurityPolicies.EncodeAsDictionary(),
                DiscoveryUrls = model.RequestedConfig?.DiscoveryUrls?.
                    EncodeAsDictionary(),
                Locales = model.RequestedConfig?.Locales?.
                    EncodeAsDictionary(),
                Connected = model.Connected ?? false,
                Version = null,
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DiscovererModel ToServiceModel(this DiscovererRegistration registration) {
            if (registration == null) {
                return null;
            }
            return new DiscovererModel {
                Discovery = registration.Discovery != DiscoveryMode.Off ?
                    registration.Discovery : (DiscoveryMode?)null,
                Id = DiscovererModelEx.CreateDiscovererId(registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                Version = registration.Version,
                LogLevel = registration.LogLevel,
                DiscoveryConfig = registration.ToConfigModel(),
                RequestedMode = registration._desired?.Discovery != DiscoveryMode.Off ?
                    registration._desired?.Discovery : null,
                RequestedConfig = registration._desired.ToConfigModel(),
                Connected = registration.IsConnected() ? true : (bool?)null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Returns if no discovery config specified
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static bool IsNullConfig(this DiscovererRegistration registration) {
            if (registration == null) {
                return true;
            }
            if (string.IsNullOrEmpty(registration.AddressRangesToScan) &&
                string.IsNullOrEmpty(registration.PortRangesToScan) &&
                registration.MaxNetworkProbes == null &&
                registration.NetworkProbeTimeout == null &&
                registration.MaxPortProbes == null &&
                registration.MinPortProbesPercent == null &&
                registration.PortProbeTimeout == null &&
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
        private static DiscoveryConfigModel ToConfigModel(this DiscovererRegistration registration) {
            return registration.IsNullConfig() ? null : new DiscoveryConfigModel {
                AddressRangesToScan = string.IsNullOrEmpty(registration.AddressRangesToScan) ?
                    null : registration.AddressRangesToScan,
                PortRangesToScan = string.IsNullOrEmpty(registration.PortRangesToScan) ?
                    null : registration.PortRangesToScan,
                MaxNetworkProbes = registration.MaxNetworkProbes,
                NetworkProbeTimeout = registration.NetworkProbeTimeout,
                MaxPortProbes = registration.MaxPortProbes,
                MinPortProbesPercent = registration.MinPortProbesPercent,
                PortProbeTimeout = registration.PortProbeTimeout,
                IdleTimeBetweenScans = registration.IdleTimeBetweenScans,
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
        private static bool IsNullFilter(this DiscovererRegistration registration) {
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
        private static EndpointActivationFilterModel ToFilterModel(this DiscovererRegistration registration) {
            return registration.IsNullFilter() ? null : new EndpointActivationFilterModel {
                SecurityMode = registration.SecurityModeFilter,
                SecurityPolicies = registration.SecurityPoliciesFilter.DecodeAsList(),
                TrustLists = registration.TrustListsFilter.DecodeAsList()
            };
        }


        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="reported"></param>
        /// <param name="desired"></param>
        internal static bool IsInSyncWith(this DiscovererRegistration reported,
            DiscovererRegistration desired) {
            if (reported == null) {
                return desired == null;
            }
            return
                desired != null &&
                reported.SiteId == desired.SiteId &&
                reported.LogLevel == desired.LogLevel &&
                reported.Discovery == desired.Discovery &&
                (string.IsNullOrEmpty(desired.AddressRangesToScan) ||
                    reported.AddressRangesToScan == desired.AddressRangesToScan) &&
                (string.IsNullOrEmpty(desired.PortRangesToScan) ||
                    reported.PortRangesToScan == desired.PortRangesToScan) &&
                (desired.MaxNetworkProbes == null ||
                    reported.MaxNetworkProbes == desired.MaxNetworkProbes) &&
                (desired.MaxNetworkProbes == null ||
                    reported.NetworkProbeTimeout == desired.NetworkProbeTimeout) &&
                (desired.MaxPortProbes == null ||
                    reported.MaxPortProbes == desired.MaxPortProbes) &&
                (desired.MinPortProbesPercent == null ||
                    reported.MinPortProbesPercent == desired.MinPortProbesPercent) &&
                (desired.PortProbeTimeout == null ||
                    reported.PortProbeTimeout == desired.PortProbeTimeout) &&
                (desired.IdleTimeBetweenScans == null ||
                    reported.IdleTimeBetweenScans == desired.IdleTimeBetweenScans) &&
                ((desired.DiscoveryUrls.DecodeAsList()?.Count ?? 0) == 0 ||
                    reported.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                        desired.DiscoveryUrls.DecodeAsList())) &&
                ((desired.Locales.DecodeAsList()?.Count ?? 0) == 0 ||
                    reported.Locales.DecodeAsList().SequenceEqualsSafe(
                        desired.Locales.DecodeAsList()));
        }
    }
}
