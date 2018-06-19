// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    public sealed class OpcUaSupervisorRegistration : OpcUaTwinRegistration {

        public override string DeviceType => "Supervisor";

        public override string ApplicationId => null;

        #region Twin Tags

        /// <summary>
        /// Edge supervisor that owns the twin.
        /// </summary>
        public override string SupervisorId =>
            SupervisorModelEx.CreateSupervisorId(DeviceId, ModuleId);

        /// <summary>
        /// Discovery state callback uris
        /// </summary>
        public Dictionary<string, Uri> DiscoveryCallbacks { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Discovery state
        /// </summary>
        public DiscoveryMode Discovery { get; set; }

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

        #endregion Twin Properties

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public OpcUaSupervisorRegistration(string deviceId = null,
            string moduleId = null) {
            DeviceId = deviceId;
            ModuleId = moduleId;
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(
            OpcUaSupervisorRegistration existing,
            OpcUaSupervisorRegistration update) {

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

            // Tags and Settings
            var cbUpdate = update?.DiscoveryCallbacks?.DecodeAsList()?.SequenceEqualsSafe(
                existing?.DiscoveryCallbacks?.DecodeAsList());
            if (!(cbUpdate ?? true)) {
                twin.Tags.Add(nameof(DiscoveryCallbacks), update?.DiscoveryCallbacks == null ?
                    null : JToken.FromObject(update.DiscoveryCallbacks));
            }

            var certUpdate = update?.Certificate?.DecodeAsByteArray()?.SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(Certificate), update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
                twin.Tags.Add(nameof(Thumbprint),
                    update?.Certificate?.DecodeAsByteArray()?.ToSha1Hash());
            }

            // Settings

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
                twin.Properties.Desired.Add(kSiteIdProp, update?.SiteId);
            }

            twin.Tags.Add(nameof(DeviceType), update?.DeviceType);
            twin.Id = update?.DeviceId ?? existing?.DeviceId;
            twin.ModuleId = update?.ModuleId ?? existing?.ModuleId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="etag"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(string deviceId, string moduleId,
            string etag, Dictionary<string, JToken> tags, Dictionary<string, JToken> properties) {
            var registration = new OpcUaSupervisorRegistration {
                // Device

                DeviceId = deviceId,
                ModuleId = moduleId,
                Etag = etag,

                // Tags

                IsDisabled =
                    tags.Get<bool>(nameof(IsDisabled), null),
                NotSeenSince =
                    tags.Get<DateTime>(nameof(NotSeenSince), null),
                Thumbprint =
                    tags.Get<string>(nameof(Thumbprint), null),
                DiscoveryCallbacks =
                    tags.Get<Dictionary<string, Uri>>(nameof(DiscoveryCallbacks), null),

                // Properties

                Certificate =
                    properties.Get<Dictionary<string, string>>(nameof(Certificate), null),
                Discovery =
                    properties.Get(nameof(Discovery), DiscoveryMode.Off),
                AddressRangesToScan =
                    properties.Get<string>(nameof(AddressRangesToScan), null),
                NetworkProbeTimeout =
                    properties.Get<TimeSpan>(nameof(NetworkProbeTimeout), null),
                MaxNetworkProbes =
                    properties.Get<int>(nameof(MaxNetworkProbes), null),
                PortRangesToScan =
                    properties.Get<string>(nameof(PortRangesToScan), null),
                PortProbeTimeout =
                    properties.Get<TimeSpan>(nameof(PortProbeTimeout), null),
                MaxPortProbes =
                    properties.Get<int>(nameof(MaxPortProbes), null),
                MinPortProbesPercent =
                    properties.Get<int>(nameof(MinPortProbesPercent), null),
                IdleTimeBetweenScans =
                    properties.Get<TimeSpan>(nameof(IdleTimeBetweenScans), null),

                SiteId =
                    properties.Get<string>(kSiteIdProp, null),
                Connected =
                    properties.Get(kConnectedProp, false),
                Type =
                    properties.Get<string>(kTypeProp, null)
            };
            return registration;
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(DeviceTwinModel twin) =>
            FromTwin(twin, false, out var tmp);

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated = FromTwin(twin.Id, twin.ModuleId, twin.Etag, twin.Tags,
                twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                FromTwin(twin.Id, twin.ModuleId, twin.Etag, twin.Tags, 
                    twin.Properties.Desired);

            connected = consolidated.Connected;
            if (desired != null) {
                desired.Connected = connected;
                if (desired.SiteId == null && consolidated.SiteId != null) {
                    // Not set by user, but by config on edge, so fake user
                    // desiring it.
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
        public static OpcUaSupervisorRegistration FromServiceModel(
            SupervisorModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(model.Id,
                out var moduleId);
            return new OpcUaSupervisorRegistration {
                IsDisabled = disabled,
                SupervisorId = model.Id,
                DeviceId = deviceId,
                ModuleId = moduleId,
                Discovery = (model.Discovery ?? DiscoveryMode.Off),
                AddressRangesToScan = model.DiscoveryConfig?.AddressRangesToScan,
                NetworkProbeTimeout = model.DiscoveryConfig?.NetworkProbeTimeout,
                MaxNetworkProbes = model.DiscoveryConfig?.MaxNetworkProbes,
                PortRangesToScan = model.DiscoveryConfig?.PortRangesToScan,
                PortProbeTimeout = model.DiscoveryConfig?.PortProbeTimeout,
                MaxPortProbes = model.DiscoveryConfig?.MaxPortProbes,
                IdleTimeBetweenScans = model.DiscoveryConfig?.IdleTimeBetweenScans,
                MinPortProbesPercent = model.DiscoveryConfig?.MinPortProbesPercent,
                Certificate = model.Certificate?.EncodeAsDictionary(),
                DiscoveryCallbacks = model.DiscoveryCallbacks?.EncodeAsDictionary(),
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
                Discovery = Discovery != DiscoveryMode.Off ? Discovery : (DiscoveryMode?)null,
                Id = SupervisorId,
                SiteId = SiteId,
                Certificate = Certificate?.DecodeAsByteArray(),
                DiscoveryConfig = ToConfigModel(),
                DiscoveryCallbacks = DiscoveryCallbacks?.DecodeAsList(),
                Connected = IsConnected() ? true : (bool?)null,
                OutOfSync = IsConnected() && !_isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var registration = obj as OpcUaSupervisorRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                Discovery == registration.Discovery &&
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
                DiscoveryCallbacks.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryCallbacks.DecodeAsList());
        }

        public static bool operator ==(OpcUaSupervisorRegistration r1,
            OpcUaSupervisorRegistration r2) =>
            EqualityComparer<OpcUaSupervisorRegistration>.Default.Equals(r1, r2);

        public static bool operator !=(OpcUaSupervisorRegistration r1,
            OpcUaSupervisorRegistration r2) => !(r1 == r2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = hashCode * -1521134295 +
                Discovery.GetHashCode();
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
                IdleTimeBetweenScans = IdleTimeBetweenScans
            };
        }

        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(OpcUaSupervisorRegistration other) {
            _isInSync =
                other != null &&
                SiteId == other.SiteId &&
                Discovery == other.Discovery &&
                AddressRangesToScan == other.AddressRangesToScan &&
                PortRangesToScan == other.PortRangesToScan &&
                MaxNetworkProbes == other.MaxNetworkProbes &&
                NetworkProbeTimeout == other.NetworkProbeTimeout &&
                MaxPortProbes == other.MaxPortProbes &&
                MinPortProbesPercent == other.MinPortProbesPercent &&
                PortProbeTimeout == other.PortProbeTimeout &&
                IdleTimeBetweenScans == other.IdleTimeBetweenScans;
        }
        internal bool IsInSync() => _isInSync;

        internal bool IsConnected() => Connected;

        private bool _isInSync;
    }
}
