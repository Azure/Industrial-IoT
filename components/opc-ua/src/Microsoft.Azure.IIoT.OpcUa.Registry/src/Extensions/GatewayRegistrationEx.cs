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
    /// Edge gateway registration extensions
    /// </summary>
    public static class GatewayRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this GatewayRegistration registration) {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this GatewayRegistration existing,
            GatewayRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, VariantValue>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, VariantValue>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(GatewayRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(GatewayRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Tags.Add(TwinProperty.SiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(GatewayRegistration.DeviceType), update?.DeviceType);
            twin.Id = update?.DeviceId ?? existing?.DeviceId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static GatewayRegistration ToGatewayRegistration(this DeviceTwinModel twin,
            Dictionary<string, VariantValue> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();
            var connected = twin.IsConnected();

            var registration = new GatewayRegistration {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault<bool>(nameof(GatewayRegistration.IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(GatewayRegistration.NotSeenSince), null),
                Type =
                    tags.GetValueOrDefault<string>(TwinProperty.Type, null),
                SiteId =
                    tags.GetValueOrDefault<string>(TwinProperty.SiteId, null),

                // Properties

            };
            return registration;
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static GatewayRegistration ToGatewayRegistration(this DeviceTwinModel twin) {
            return ToGatewayRegistration(twin, out var tmp);
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
        /// <returns></returns>
        public static GatewayRegistration ToGatewayRegistration(this DeviceTwinModel twin,
            out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, VariantValue>();
            }

            var consolidated =
                ToGatewayRegistration(twin, twin.GetConsolidatedProperties());
            connected = consolidated.Connected;
            return consolidated;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        public static GatewayRegistration ToGatewayRegistration(
            this GatewayModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = model.Id;
            return new GatewayRegistration {
                IsDisabled = disabled,
                DeviceId = deviceId,
                Connected = model.Connected ?? false,
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static GatewayModel ToServiceModel(this GatewayRegistration registration) {
            if (registration == null) {
                return null;
            }
            return new GatewayModel {
                Id = registration.DeviceId,
                SiteId = registration.SiteId,
                Connected = registration.IsConnected() ? true : (bool?)null
            };
        }
    }
}
