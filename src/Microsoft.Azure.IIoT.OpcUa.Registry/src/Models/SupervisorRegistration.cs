// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using Serilog.Events;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    public sealed class SupervisorRegistration : BaseRegistration {

        /// <inheritdoc/>
        public override string DeviceType => "Supervisor";

        /// <inheritdoc/>
        public override string ApplicationId => null;

        #region Twin Tags

        /// <summary>
        /// Supervisor that owns the twin.
        /// </summary>
        public override string SupervisorId =>
            SupervisorModelEx.CreateSupervisorId(DeviceId, ModuleId);

        /// <summary>
        /// Discovery state callback uris
        /// </summary>
        public Dictionary<string, CallbackModel> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Activation filter security mode
        /// </summary>
        public SecurityMode? SecurityModeFilter { get; set; }

        /// <summary>
        /// Activation filter security policies
        /// </summary>
        public Dictionary<string, string> SecurityPoliciesFilter { get; set; }

        /// <summary>
        /// Activation filter trust lists
        /// </summary>
        public Dictionary<string, string> TrustListsFilter { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Discovery state
        /// </summary>
        public DiscoveryMode Discovery { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public SupervisorLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Address ranges to scan (null == all wired)
        /// </summary>
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        public TimeSpan? NetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public TimeSpan? PortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public TimeSpan? IdleTimeBetweenScans { get; set; }

        /// <summary>
        /// predefined discovery urls for supervisor
        /// </summary>
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Locales to filter discoveries against
        /// </summary>
        public Dictionary<string, string> Locales { get; set; }

        #endregion Twin Properties

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

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(SupervisorRegistration existing,
            SupervisorRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken> ()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrSupervisorId != existing?.SiteOrSupervisorId) {
                twin.Tags.Add(nameof(SiteOrSupervisorId), update?.SiteOrSupervisorId);
            }

            var cbUpdate = update?.DiscoveryCallbacks?.DecodeAsList()?.SetEqualsSafe(
                existing?.DiscoveryCallbacks?.DecodeAsList(),
                    (callback1, callback2) => callback1.IsSameAs(callback2));
            if (!(cbUpdate ?? true)) {
                twin.Tags.Add(nameof(DiscoveryCallbacks), update?.DiscoveryCallbacks == null ?
                    null : JToken.FromObject(update.DiscoveryCallbacks));
            }

            var policiesUpdate = update?.SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.SecurityPoliciesFilter?.DecodeAsList());
            if (!(policiesUpdate ?? true)) {
                twin.Tags.Add(nameof(SecurityPoliciesFilter), update?.SecurityPoliciesFilter == null ?
                    null : JToken.FromObject(update.SecurityPoliciesFilter));
            }

            var trustListUpdate = update?.TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                existing?.TrustListsFilter?.DecodeAsList());
            if (!(trustListUpdate ?? true)) {
                twin.Tags.Add(nameof(TrustListsFilter), update?.TrustListsFilter == null ?
                    null : JToken.FromObject(update.TrustListsFilter));
            }

            if (update?.SecurityModeFilter != existing?.SecurityModeFilter) {
                twin.Tags.Add(nameof(SecurityModeFilter),
                    JToken.FromObject(update?.SecurityModeFilter));
            }

            // Settings

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                existing?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(DiscoveryUrls), update?.DiscoveryUrls == null ?
                    null : JToken.FromObject(update.DiscoveryUrls));
            }

            var certUpdate = update?.Certificate?.DecodeAsByteArray()?.SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(Certificate), update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
                twin.Tags.Add(nameof(Thumbprint),
                    update?.Certificate?.DecodeAsByteArray()?.ToSha1Hash());
            }

            var localesUpdate = update?.Locales?.DecodeAsList()?.SequenceEqualsSafe(
                existing?.Locales?.DecodeAsList());
            if (!(localesUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(Locales), update?.Locales == null ?
                    null : JToken.FromObject(update.Locales));
            }

            if (update?.Discovery != existing?.Discovery) {
                twin.Properties.Desired.Add(nameof(Discovery),
                    JToken.FromObject(update?.Discovery));
            }

            if (update?.AddressRangesToScan != existing?.AddressRangesToScan) {
                twin.Properties.Desired.Add(nameof(AddressRangesToScan),
                    update?.AddressRangesToScan);
            }

            if (update?.NetworkProbeTimeout != existing?.NetworkProbeTimeout) {
                twin.Properties.Desired.Add(nameof(NetworkProbeTimeout),
                    update?.NetworkProbeTimeout);
            }

            if (update?.LogLevel != existing?.LogLevel) {
                twin.Properties.Desired.Add(nameof(LogLevel), update?.LogLevel == null ?
                    null : JToken.FromObject(update.LogLevel));
            }

            if (update?.MaxNetworkProbes != existing?.MaxNetworkProbes) {
                twin.Properties.Desired.Add(nameof(MaxNetworkProbes),
                    update?.MaxNetworkProbes);
            }

            if (update?.PortRangesToScan != existing?.PortRangesToScan) {
                twin.Properties.Desired.Add(nameof(PortRangesToScan),
                    update?.PortRangesToScan);
            }

            if (update?.PortProbeTimeout != existing?.PortProbeTimeout) {
                twin.Properties.Desired.Add(nameof(PortProbeTimeout),
                    update?.PortProbeTimeout);
            }

            if (update?.MaxPortProbes != existing?.MaxPortProbes) {
                twin.Properties.Desired.Add(nameof(MaxPortProbes),
                    update?.MaxPortProbes);
            }

            if (update?.IdleTimeBetweenScans != existing?.IdleTimeBetweenScans) {
                twin.Properties.Desired.Add(nameof(IdleTimeBetweenScans),
                    update?.IdleTimeBetweenScans);
            }

            if (update?.MinPortProbesPercent != existing?.MinPortProbesPercent) {
                twin.Properties.Desired.Add(nameof(MinPortProbesPercent),
                    update?.MinPortProbesPercent);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.kSiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(DeviceType), update?.DeviceType);
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
        public static SupervisorRegistration FromTwin(DeviceTwinModel twin,
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
                    tags.GetValueOrDefault<bool>(nameof(IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(NotSeenSince), null),
                Thumbprint =
                    tags.GetValueOrDefault<string>(nameof(Thumbprint), null),
                DiscoveryCallbacks =
                    tags.GetValueOrDefault<Dictionary<string, CallbackModel>>(nameof(DiscoveryCallbacks), null),
                SecurityModeFilter =
                    tags.GetValueOrDefault<SecurityMode>(nameof(SecurityModeFilter), null),
                SecurityPoliciesFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(SecurityPoliciesFilter), null),
                TrustListsFilter =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(TrustListsFilter), null),

                // Properties

                Certificate =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(Certificate), null),
                LogLevel =
                    properties.GetValueOrDefault<SupervisorLogLevel>(nameof(LogLevel), null),
                Discovery =
                    properties.GetValueOrDefault(nameof(Discovery), DiscoveryMode.Off),
                AddressRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(AddressRangesToScan), null),
                NetworkProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(NetworkProbeTimeout), null),
                MaxNetworkProbes =
                    properties.GetValueOrDefault<int>(nameof(MaxNetworkProbes), null),
                PortRangesToScan =
                    properties.GetValueOrDefault<string>(nameof(PortRangesToScan), null),
                PortProbeTimeout =
                    properties.GetValueOrDefault<TimeSpan>(nameof(PortProbeTimeout), null),
                MaxPortProbes =
                    properties.GetValueOrDefault<int>(nameof(MaxPortProbes), null),
                MinPortProbesPercent =
                    properties.GetValueOrDefault<int>(nameof(MinPortProbesPercent), null),
                IdleTimeBetweenScans =
                    properties.GetValueOrDefault<TimeSpan>(nameof(IdleTimeBetweenScans), null),
                DiscoveryUrls =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(DiscoveryUrls), null),
                Locales =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(Locales), null),

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
        public static SupervisorRegistration FromTwin(DeviceTwinModel twin,
            bool onlyServerState) {
            return FromTwin(twin, onlyServerState, out var tmp);
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
        public static SupervisorRegistration FromTwin(DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated =
                FromTwin(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                FromTwin(twin, twin.Properties.Desired);

            connected = consolidated.Connected;
            if (desired != null) {
                desired.Connected = connected;
                if (desired.SiteId == null && consolidated.SiteId != null) {
                    // Not set by user, but by config, so fake user desiring it.
                    desired.SiteId = consolidated.SiteId;
                }
            }

            if (!onlyServerState) {
                consolidated.MarkAsInSyncWith(desired);
                return consolidated;
            }
            desired?.MarkAsInSyncWith(consolidated);
            return desired;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        public static SupervisorRegistration FromServiceModel(
            SupervisorModel model, bool? disabled = null) {
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
        /// <returns></returns>
        public SupervisorModel ToServiceModel() {
            return new SupervisorModel {
                Discovery = Discovery != DiscoveryMode.Off ?
                    Discovery : (DiscoveryMode?)null,
                Id = SupervisorId,
                SiteId = SiteId,
                Certificate = Certificate?.DecodeAsByteArray(),
                LogLevel = LogLevel,
                DiscoveryConfig = ToConfigModel(),
                Connected = IsConnected() ? true : (bool?)null,
                OutOfSync = IsConnected() && !_isInSync ? true : (bool?)null
            };
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as SupervisorRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                Discovery == registration.Discovery &&
                LogLevel == registration.LogLevel &&
                AddressRangesToScan == registration.AddressRangesToScan &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    NetworkProbeTimeout, registration.NetworkProbeTimeout) &&
                EqualityComparer<int?>.Default.Equals(
                    MaxNetworkProbes, registration.MaxNetworkProbes) &&
                PortRangesToScan == registration.PortRangesToScan &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    PortProbeTimeout, registration.PortProbeTimeout) &&
                EqualityComparer<int?>.Default.Equals(
                    MaxPortProbes, registration.MaxPortProbes) &&
                EqualityComparer<int?>.Default.Equals(
                    MinPortProbesPercent, registration.MinPortProbesPercent) &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    IdleTimeBetweenScans, registration.IdleTimeBetweenScans) &&
                EqualityComparer<SecurityMode?>.Default.Equals(
                    SecurityModeFilter, registration.SecurityModeFilter) &&
                TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.TrustListsFilter.DecodeAsList()) &&
                SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.SecurityPoliciesFilter.DecodeAsList()) &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryUrls.DecodeAsList()) &&
                Locales.DecodeAsList().SequenceEqualsSafe(
                    registration.Locales.DecodeAsList()) &&
                DiscoveryCallbacks.DecodeAsList().SetEqualsSafe(
                    registration.DiscoveryCallbacks.DecodeAsList(),
                        (callback1, callback2) => callback1.IsSameAs(callback2));
        }

        /// <inheritdoc/>
        public static bool operator ==(SupervisorRegistration r1,
            SupervisorRegistration r2) {
            return EqualityComparer<SupervisorRegistration>.Default.Equals(r1, r2);
        }

        /// <inheritdoc/>
        public static bool operator !=(SupervisorRegistration r1,
            SupervisorRegistration r2) {
            return !(r1 == r2);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = hashCode * -1521134295 +
                Discovery.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<SupervisorLogLevel?>.Default.GetHashCode(LogLevel);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(AddressRangesToScan);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(NetworkProbeTimeout);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<int?>.Default.GetHashCode(MaxNetworkProbes);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(PortRangesToScan);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(PortProbeTimeout);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<int?>.Default.GetHashCode(MaxPortProbes);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<int?>.Default.GetHashCode(MinPortProbesPercent);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityModeFilter);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(IdleTimeBetweenScans);
            return hashCode;
        }

        /// <summary>
        /// Returns if no discovery config specified
        /// </summary>
        /// <returns></returns>
        private bool IsNullConfig() {
            if (string.IsNullOrEmpty(AddressRangesToScan) &&
                string.IsNullOrEmpty(PortRangesToScan) &&
                MaxNetworkProbes == null &&
                NetworkProbeTimeout == null &&
                MaxPortProbes == null &&
                MinPortProbesPercent == null &&
                PortProbeTimeout == null &&
                (DiscoveryCallbacks == null || DiscoveryCallbacks.Count == 0) &&
                (DiscoveryUrls == null || DiscoveryUrls.Count == 0) &&
                (Locales == null || Locales.Count == 0) &&
                IdleTimeBetweenScans == null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns config model
        /// </summary>
        /// <returns></returns>
        private DiscoveryConfigModel ToConfigModel() {
            return IsNullConfig() ? null : new DiscoveryConfigModel {
                AddressRangesToScan = AddressRangesToScan,
                PortRangesToScan = PortRangesToScan,
                MaxNetworkProbes = MaxNetworkProbes,
                NetworkProbeTimeout = NetworkProbeTimeout,
                MaxPortProbes = MaxPortProbes,
                MinPortProbesPercent = MinPortProbesPercent,
                PortProbeTimeout = PortProbeTimeout,
                IdleTimeBetweenScans = IdleTimeBetweenScans,
                Callbacks = DiscoveryCallbacks?.DecodeAsList(),
                DiscoveryUrls = DiscoveryUrls?.DecodeAsList(),
                Locales = Locales?.DecodeAsList(),
                ActivationFilter = ToFilterModel(),
            };
        }

        /// <summary>
        /// Returns if no activation filter specified
        /// </summary>
        /// <returns></returns>
        private bool IsNullFilter() {
            if (SecurityModeFilter == null &&
                (TrustListsFilter == null || TrustListsFilter.Count == 0) &&
                (SecurityPoliciesFilter == null || SecurityPoliciesFilter.Count == 0)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns activation filter model
        /// </summary>
        /// <returns></returns>
        private EndpointActivationFilterModel ToFilterModel() {
            return IsNullFilter() ? null : new EndpointActivationFilterModel {
                SecurityMode = SecurityModeFilter,
                SecurityPolicies = SecurityPoliciesFilter.DecodeAsList(),
                TrustLists = TrustListsFilter.DecodeAsList()
            };
        }


        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(SupervisorRegistration other) {
            _isInSync =
                other != null &&
                SiteId == other.SiteId &&
                LogLevel == other.LogLevel &&
                Discovery == other.Discovery &&
                AddressRangesToScan == other.AddressRangesToScan &&
                PortRangesToScan == other.PortRangesToScan &&
                MaxNetworkProbes == other.MaxNetworkProbes &&
                NetworkProbeTimeout == other.NetworkProbeTimeout &&
                MaxPortProbes == other.MaxPortProbes &&
                MinPortProbesPercent == other.MinPortProbesPercent &&
                PortProbeTimeout == other.PortProbeTimeout &&
                IdleTimeBetweenScans == other.IdleTimeBetweenScans &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    other.DiscoveryUrls.DecodeAsList()) &&
                Locales.DecodeAsList().SequenceEqualsSafe(
                    other.Locales.DecodeAsList());
        }

        internal bool IsInSync() {
            return _isInSync;
        }

        internal bool IsConnected() {
            return Connected;
        }

        private bool _isInSync;
    }
}
