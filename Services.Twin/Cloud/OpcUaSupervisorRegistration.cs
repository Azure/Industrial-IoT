// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    public sealed class OpcUaSupervisorRegistration {

        /// <summary>
        /// Device id of supervisor
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Module id of supervisor
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Etag id
        /// </summary>
        public string Etag { get; set; }

        #region Twin Tags

        /// <summary>
        /// Domain
        /// </summary>
        public string Domain { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Discovery state
        /// </summary>
        public DiscoveryMode Discovery { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        public DiscoveryConfigModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Connected
        /// </summary>
        public bool Connected { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        public DeviceTwinModel Patch(SupervisorModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            var twin = new DeviceTwinModel {
                Etag = Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken> ()
                }
            };

            DeviceId = SupervisorModelEx.ParseDeviceId(model.Id,
                out var moduleId);
            ModuleId = moduleId;

            // Tags

            if (Domain != model.Domain?.ToLowerInvariant()) {
                Domain = model.Domain?.ToLowerInvariant();
                twin.Tags.Add(nameof(Domain), Domain);
            }

            // Settings

            if (Discovery != (model.Discovery ?? DiscoveryMode.Off)) {
                Discovery = (model.Discovery ?? DiscoveryMode.Off);
                twin.Properties.Desired.Add(nameof(Discovery),
                    JToken.FromObject(Discovery));
            }

            if (!IsConfigEqual(DiscoveryConfig, model.DiscoveryConfig)) {
                DiscoveryConfig = model.DiscoveryConfig;
                twin.Properties.Desired.Add(nameof(DiscoveryConfig),
                    JToken.FromObject(DiscoveryConfig));
            }

            twin.Id = DeviceId;
            twin.ModuleId = ModuleId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="etag"></param>
        /// <param name="tags"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(string deviceId, string moduleId,
            string etag, Dictionary<string, JToken> tags, Dictionary<string, JToken> properties) {
            return new OpcUaSupervisorRegistration {
                // Device

                DeviceId = deviceId,
                ModuleId = moduleId,
                Etag = etag,

                // Tags

                Domain =
                    tags.Get<string>(nameof(Domain), null),

                // Properties

                Discovery =
                    properties.Get(nameof(Discovery), DiscoveryMode.Off),
                DiscoveryConfig =
                    properties.Get(nameof(DiscoveryConfig), new DiscoveryConfigModel()),
                Connected =
                    properties.Get("connected", false),
                Type =
                    properties.Get<string>("type", null)
            };
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

            connected = false;
            if (twin == null) {
                return null;
            }

            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            OpcUaSupervisorRegistration reported = null, desired = null;
            if (twin.Properties?.Reported != null) {
                reported = FromTwin(twin.Id, twin.ModuleId, twin.Etag, twin.Tags,
                    twin.Properties.Reported);
                if (reported != null) {
                    connected = reported.Connected;
                }
            }

            if (twin.Properties?.Desired != null) {
                desired = FromTwin(twin.Id, twin.ModuleId, twin.Etag, twin.Tags,
                    twin.Properties.Desired);
            }

            if (!onlyServerState && reported != null) {
                reported.MarkAsInSyncWith(desired);
                return reported;
            }

            if (desired != null) {
                desired.MarkAsInSyncWith(reported);
                return desired;
            }
            return null;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorModel ToServiceModel() {
            return new SupervisorModel {
                Discovery = Discovery != DiscoveryMode.Off ? Discovery : (DiscoveryMode?)null,
                Domain = string.IsNullOrEmpty(Domain) ? null : Domain,
                DiscoveryConfig = IsNullOrEmpty(DiscoveryConfig) ? null : DiscoveryConfig,
                Connected = IsConnected() ? true : (bool?)null,
                OutOfSync = IsConnected() && !_isInSync ? true : (bool?)null,
                Id = SupervisorModelEx.CreateSupervisorId(DeviceId, ModuleId)
            };
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// These test only the items that are exchanged between edge
        /// and server, i.e. reported and desired.  Other properties are
        /// reported out of band, or only apply to the server side, etc.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(OpcUaSupervisorRegistration other) {
            _isInSync =
                other != null &&
                Discovery == other.Discovery;
        }

        /// <summary>
        /// Returns true if config is null or empty
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static bool IsNullOrEmpty(DiscoveryConfigModel config) =>
            config == null || (
                string.IsNullOrEmpty(config.AddressRangesToScan) &&
                string.IsNullOrEmpty(config.PortRangesToScan) &&
                config.MaxNetworkProbes == null &&
                config.NetworkProbeTimeout == null &&
                config.MaxPortProbes == null &&
                config.PortProbeTimeout == null &&
                config.IdleTimeBetweenScans == null);

        /// <summary>
        /// Returns whether 2 configurations are the same.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        private static bool IsConfigEqual(DiscoveryConfigModel config,
            DiscoveryConfigModel other) {
            if (config == null) {
                return other == null;
            }
            return
                config.AddressRangesToScan == other?.AddressRangesToScan &&
                config.NetworkProbeTimeout == other?.NetworkProbeTimeout &&
                config.MaxNetworkProbes == other?.MaxNetworkProbes &&
                config.PortRangesToScan == other?.PortRangesToScan &&
                config.PortProbeTimeout == other?.PortProbeTimeout &&
                config.MaxPortProbes == other?.MaxPortProbes &&
                config.IdleTimeBetweenScans == other?.IdleTimeBetweenScans;
        }

        internal bool IsConnected() => Connected;
        internal bool IsInSync() => _isInSync;
        private bool _isInSync;
    }
}
