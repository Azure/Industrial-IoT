// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Publisher registration extensions
    /// </summary>
    public static class PublisherRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(
            this PublisherRegistration registration, IJsonSerializer serializer) {
            return Patch(null, registration, serializer);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="serializer"></param>
        public static DeviceTwinModel Patch(this PublisherRegistration existing,
            PublisherRegistration update, IJsonSerializer serializer) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, VariantValue>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, VariantValue>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(PublisherRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(PublisherRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrGatewayId != existing?.SiteOrGatewayId) {
                twin.Tags.Add(nameof(PublisherRegistration.SiteOrGatewayId),
                    update?.SiteOrGatewayId);
            }


            // Settings

            var capsUpdate = update?.Capabilities.SetEqualsSafe(
                existing?.Capabilities, (x, y) => x.Key == y.Key && y.Value == x.Value);
            if (!(capsUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.Capabilities),
                    update?.Capabilities == null ?
                    null : serializer.FromObject(update.Capabilities));
            }

            if (update?.JobOrchestratorUrl != existing?.JobOrchestratorUrl) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.JobOrchestratorUrl),
                    update?.JobOrchestratorUrl);
            }
            if (update?.LogLevel != existing?.LogLevel) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.LogLevel),
                    update?.LogLevel == null ?
                    null : serializer.FromObject(update.LogLevel.ToString()));
            }


            if ((update?.JobCheckInterval ?? Timeout.InfiniteTimeSpan) !=
                (existing?.JobCheckInterval ?? Timeout.InfiniteTimeSpan)) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.JobCheckInterval),
                    update?.JobCheckInterval);
            }

            if ((update?.MaxWorkers ?? 1) != (existing?.MaxWorkers ?? 1)) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.MaxWorkers),
                    update?.MaxWorkers);
            }

            if ((update?.HeartbeatInterval ?? Timeout.InfiniteTimeSpan) !=
                (existing?.HeartbeatInterval ?? Timeout.InfiniteTimeSpan)) {
                twin.Properties.Desired.Add(nameof(PublisherRegistration.HeartbeatInterval),
                    update?.HeartbeatInterval);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.SiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(PublisherRegistration.DeviceType), update?.DeviceType);
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
        public static PublisherRegistration ToPublisherRegistration(this DeviceTwinModel twin,
            Dictionary<string, VariantValue> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();
            var connected = twin.IsConnected();

            var registration = new PublisherRegistration {
                // Device

                DeviceId = twin.Id,
                ModuleId = twin.ModuleId,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault<bool>(nameof(PublisherRegistration.IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(PublisherRegistration.NotSeenSince), null),

                // Properties

                LogLevel =
                    properties.GetValueOrDefault<TraceLogLevel>(nameof(PublisherRegistration.LogLevel), null),
                JobOrchestratorUrl =
                    properties.GetValueOrDefault<string>(nameof(PublisherRegistration.JobOrchestratorUrl), null),
                JobCheckInterval =
                    properties.GetValueOrDefault<TimeSpan>(nameof(PublisherRegistration.JobCheckInterval), null),
                MaxWorkers =
                    properties.GetValueOrDefault<int>(nameof(PublisherRegistration.MaxWorkers), null),
                HeartbeatInterval =
                    properties.GetValueOrDefault<TimeSpan>(nameof(PublisherRegistration.HeartbeatInterval), null),
                Capabilities =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(PublisherRegistration.Capabilities), null),

                SiteId =
                    properties.GetValueOrDefault<string>(TwinProperty.SiteId, null),
                Connected = connected ??
                    properties.GetValueOrDefault(TwinProperty.Connected, false),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.Type, null)
            };
            return registration;
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static PublisherRegistration ToPublisherRegistration(this DeviceTwinModel twin,
            bool onlyServerState) {
            return ToPublisherRegistration(twin, onlyServerState, out var tmp);
        }

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
        /// <param name="connected"></param>
        /// <returns></returns>
        public static PublisherRegistration ToPublisherRegistration(this DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, VariantValue>();
            }

            var consolidated =
                ToPublisherRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                ToPublisherRegistration(twin, twin.Properties.Desired);

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
        public static PublisherRegistration ToPublisherRegistration(
            this PublisherModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(model.Id,
                out var moduleId);
            return new PublisherRegistration {
                IsDisabled = disabled,
                DeviceId = deviceId,
                ModuleId = moduleId,
                LogLevel = model.LogLevel,
                JobOrchestratorUrl = model.Configuration?.JobOrchestratorUrl,
                JobCheckInterval = model.Configuration?.JobCheckInterval,
                MaxWorkers = model.Configuration?.MaxWorkers,
                HeartbeatInterval = model.Configuration?.HeartbeatInterval,
                Capabilities = model.Configuration?.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                Connected = model.Connected ?? false,
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static PublisherModel ToServiceModel(this PublisherRegistration registration) {
            if (registration == null) {
                return null;
            }
            return new PublisherModel {
                Id = PublisherModelEx.CreatePublisherId(registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                LogLevel = registration.LogLevel,
                Configuration = registration.ToConfigModel(),
                Connected = registration.IsConnected() ? true : (bool?)null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : (bool?)null
            };
        }


        /// <summary>
        /// Returns if no discovery config specified
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static bool IsNullConfig(this PublisherRegistration registration) {
            if (string.IsNullOrEmpty(registration.JobOrchestratorUrl) &&
                (registration.MaxWorkers == null || registration.MaxWorkers == 1) &&
                (registration.JobCheckInterval == null ||
                    registration.JobCheckInterval == Timeout.InfiniteTimeSpan) &&
                (registration.Capabilities == null ||
                    registration.Capabilities.Count == 0) &&
                (registration.HeartbeatInterval == null ||
                    registration.HeartbeatInterval == Timeout.InfiniteTimeSpan)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns config model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private static PublisherConfigModel ToConfigModel(this PublisherRegistration registration) {
            return registration.IsNullConfig() ? null : new PublisherConfigModel {
                JobOrchestratorUrl = registration.JobOrchestratorUrl,
                MaxWorkers = registration.MaxWorkers == 1 ? null : registration.MaxWorkers,
                JobCheckInterval = registration.JobCheckInterval == Timeout.InfiniteTimeSpan ?
                    null : registration.JobCheckInterval,
                HeartbeatInterval = registration.HeartbeatInterval == Timeout.InfiniteTimeSpan ?
                    null : registration.HeartbeatInterval,
                Capabilities = registration.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value)
            };
        }

        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this PublisherRegistration registration,
            PublisherRegistration other) {
            if (registration == null) {
                return other == null;
            }
            return
                other != null &&
                registration.SiteId == other.SiteId &&
                registration.LogLevel == other.LogLevel &&
                registration.JobOrchestratorUrl == other.JobOrchestratorUrl &&
                (registration.MaxWorkers ?? 1) == (other.MaxWorkers ?? 1)&&
                (registration.JobCheckInterval ?? Timeout.InfiniteTimeSpan) ==
                    (other.JobCheckInterval ?? Timeout.InfiniteTimeSpan) &&
                (registration.HeartbeatInterval ?? Timeout.InfiniteTimeSpan) ==
                    (other.HeartbeatInterval ?? Timeout.InfiniteTimeSpan) &&
                registration.Capabilities.SetEqualsSafe(
                    other.Capabilities, (x, y) => x.Key == y.Key && y.Value == x.Value);
        }
    }
}
