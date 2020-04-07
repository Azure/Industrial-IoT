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

    /// <summary>
    /// Twin registration extensions
    /// </summary>
    public static class SupervisorRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(
            this SupervisorRegistration registration, IJsonSerializer serializer) {
            return Patch(null, registration, serializer);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="serializer"></param>
        public static DeviceTwinModel Patch(this SupervisorRegistration existing,
            SupervisorRegistration update, IJsonSerializer serializer) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, VariantValue>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, VariantValue>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(SupervisorRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(SupervisorRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrGatewayId != existing?.SiteOrGatewayId) {
                twin.Tags.Add(nameof(SupervisorRegistration.SiteOrGatewayId),
                    update?.SiteOrGatewayId);
            }

            // Settings

            if (update?.LogLevel != existing?.LogLevel) {
                twin.Properties.Desired.Add(nameof(SupervisorRegistration.LogLevel),
                    update?.LogLevel == null ?
                    null : serializer.FromObject(update.LogLevel.ToString()));
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.SiteId, update?.SiteId);
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
            Dictionary<string, VariantValue> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();
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

                // Properties

                LogLevel =
                    properties.GetValueOrDefault<TraceLogLevel>(nameof(SupervisorRegistration.LogLevel), null),

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
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <param name="connected"></param>
        /// <returns></returns>
        public static SupervisorRegistration ToSupervisorRegistration(this DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, VariantValue>();
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
                DeviceId = deviceId,
                ModuleId = moduleId,
                LogLevel = model.LogLevel,
                Connected = model.Connected ?? false,
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static SupervisorModel ToServiceModel(this SupervisorRegistration registration) {
            if (registration == null) {
                return null;
            }
            return new SupervisorModel {
                Id = SupervisorModelEx.CreateSupervisorId(registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                LogLevel = registration.LogLevel,
                Connected = registration.IsConnected() ? true : (bool?)null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this SupervisorRegistration registration,
            SupervisorRegistration other) {
            if (registration == null) {
                return other == null;
            }
            return
                other != null &&
                registration.SiteId == other.SiteId &&
                registration.LogLevel == other.LogLevel;
        }
    }
}
