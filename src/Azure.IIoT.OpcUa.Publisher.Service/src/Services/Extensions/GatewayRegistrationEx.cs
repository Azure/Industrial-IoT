// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Edge gateway registration extensions
    /// </summary>
    public static class GatewayRegistrationEx
    {
        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this GatewayRegistration registration)
        {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this GatewayRegistration? existing,
            GatewayRegistration update)
        {
            var tags = new Dictionary<string, VariantValue>();
            var desired = new Dictionary<string, VariantValue>();

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled)
            {
                tags.Add(nameof(GatewayRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                tags.Add(nameof(GatewayRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteId != existing?.SiteId)
            {
                tags.Add(Constants.TwinPropertySiteKey, update?.SiteId);
            }

            tags.Add(nameof(GatewayRegistration.DeviceType), update?.DeviceType);

            return new DeviceTwinModel
            {
                Id = update?.DeviceId ?? existing?.DeviceId ?? string.Empty,
                Etag = existing?.Etag ?? string.Empty,
                Tags = tags,
                Desired = desired
            };
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static GatewayRegistration? ToGatewayRegistration(this DeviceTwinModel twin,
            IReadOnlyDictionary<string, VariantValue> properties)
        {
            if (twin == null)
            {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();

            return new GatewayRegistration
            {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Connected
                Connected = twin.IsConnected() ?? false,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault(nameof(GatewayRegistration.IsDisabled), (bool?)null),
                NotSeenSince =
                    tags.GetValueOrDefault(nameof(GatewayRegistration.NotSeenSince), (DateTime?)null),
                Type =
                    tags.GetValueOrDefault(Constants.TwinPropertyTypeKey, (string?)null),
                SiteId =
                    tags.GetValueOrDefault(Constants.TwinPropertySiteKey, (string?)null)

                // Properties
            };
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static GatewayRegistration? ToGatewayRegistration(this DeviceTwinModel twin)
        {
            return ToGatewayRegistration(twin, out _);
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
        public static GatewayRegistration? ToGatewayRegistration(this DeviceTwinModel twin,
            out bool connected)
        {
            if (twin == null)
            {
                connected = false;
                return null;
            }
            twin.Tags ??= new Dictionary<string, VariantValue>();

            var consolidated =
                ToGatewayRegistration(twin, twin.GetConsolidatedProperties());
            connected = consolidated?.Connected ?? false;
            return consolidated;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        public static GatewayRegistration ToGatewayRegistration(
            this GatewayModel model, bool? disabled = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            var deviceId = model.Id;
            return new GatewayRegistration
            {
                IsDisabled = disabled,
                DeviceId = deviceId,
                Connected = model.Connected ?? false,
                SiteId = model.SiteId
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static GatewayModel? ToServiceModel(this GatewayRegistration? registration)
        {
            if (registration is null)
            {
                return null;
            }
            return new GatewayModel
            {
                Id = registration.DeviceId,
                SiteId = registration.SiteId,
                Connected = registration.IsConnected() ? true : null
            };
        }
    }
}
