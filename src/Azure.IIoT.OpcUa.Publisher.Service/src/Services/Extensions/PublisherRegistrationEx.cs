// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Publisher registration extensions
    /// </summary>
    public static class PublisherRegistrationEx
    {
        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this PublisherRegistration registration,
            TimeProvider timeProvider)
        {
            return Patch(null, registration, timeProvider);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="timeProvider"></param>
        ///
        public static DeviceTwinModel Patch(this PublisherRegistration? existing,
            PublisherRegistration update, TimeProvider timeProvider)
        {
            var tags = new Dictionary<string, VariantValue>();
            var desired = new Dictionary<string, VariantValue>();

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled)
            {
                tags.Add(nameof(PublisherRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                tags.Add(nameof(PublisherRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    timeProvider.GetUtcNow().UtcDateTime : (DateTime?)null);
            }

            if (update?.SiteOrGatewayId != existing?.SiteOrGatewayId)
            {
                tags.Add(nameof(PublisherRegistration.SiteOrGatewayId),
                    update?.SiteOrGatewayId);
            }

            // Settings
            if (update?.ApiKey != existing?.ApiKey)
            {
                desired.Add(Constants.TwinPropertyApiKeyKey, update?.ApiKey);
            }

            if (update?.SiteId != existing?.SiteId)
            {
                desired.Add(Constants.TwinPropertySiteKey, update?.SiteId);
            }

            tags.Add(nameof(PublisherRegistration.DeviceType), update?.DeviceType);

            return new DeviceTwinModel
            {
                Id = update?.DeviceId ?? existing?.DeviceId ?? string.Empty,
                ModuleId = update?.ModuleId ?? existing?.ModuleId,
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
        public static PublisherRegistration? ToPublisherRegistration(this DeviceTwinModel twin,
            IReadOnlyDictionary<string, VariantValue> properties)
        {
            if (twin == null)
            {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();

            return new PublisherRegistration
            {
                // Device

                DeviceId = twin.Id,
                ModuleId = twin.ModuleId,
                Etag = twin.Etag,
                Connected = twin.IsConnected() ?? false,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault(nameof(PublisherRegistration.IsDisabled), (bool?)null),
                NotSeenSince =
                    tags.GetValueOrDefault(nameof(PublisherRegistration.NotSeenSince), (DateTime?)null),

                // Properties

                ApiKey =
                    properties.GetValueOrDefault(Constants.TwinPropertyApiKeyKey, (string?)null),
                SiteId =
                    properties.GetValueOrDefault(Constants.TwinPropertySiteKey, (string?)null),
                Version =
                    properties.GetValueOrDefault(Constants.TwinPropertyVersionKey, (string?)null),
                Type =
                    properties.GetValueOrDefault(Constants.TwinPropertyTypeKey, (string?)null)
            };
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static PublisherRegistration? ToPublisherRegistration(this DeviceTwinModel twin,
            bool onlyServerState = false)
        {
            return ToPublisherRegistration(twin, onlyServerState, out _);
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
        public static PublisherRegistration? ToPublisherRegistration(this DeviceTwinModel twin,
            bool onlyServerState, out bool connected)
        {
            if (twin == null)
            {
                connected = false;
                return null;
            }
            twin.Tags ??= new Dictionary<string, VariantValue>();

            var consolidated =
                ToPublisherRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Desired == null) ? null :
                ToPublisherRegistration(twin, twin.Desired);

            connected = consolidated?.Connected ?? false;
            if (desired is not null && consolidated is not null)
            {
                desired.Connected = consolidated.Connected;
                if (desired.SiteId == null && consolidated.SiteId != null)
                {
                    // Not set by user, but by config, so fake user desiring it.
                    desired.SiteId = consolidated.SiteId;
                }
                if (desired.ApiKey == null && consolidated.ApiKey != null)
                {
                    // Not set by user, but reported, so set as desired
                    desired.ApiKey = consolidated.ApiKey;
                }
                desired.Version = consolidated.Version;
            }

            if (!onlyServerState && consolidated is not null)
            {
                consolidated._isInSync = consolidated.IsInSyncWith(desired);
                return consolidated;
            }
            if (desired is not null)
            {
                desired._isInSync = desired.IsInSyncWith(consolidated);
            }
            return desired;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <exception cref="ArgumentNullException"><paramref name="model"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        public static PublisherRegistration ToPublisherRegistration(
            this PublisherModel model, bool? disabled = null)
        {
            if (model.Id == null)
            {
                throw new ArgumentException("Id missing", nameof(model));
            }
            if (!HubResource.Parse(model.Id, out _, out var deviceId,
                out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(model));
            }
            return new PublisherRegistration
            {
                IsDisabled = disabled,
                DeviceId = deviceId,
                ModuleId = moduleId,
                ApiKey = model.ApiKey,
                Connected = model.Connected ?? false,
                Version = null,
                SiteId = model.SiteId
            };
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <exception cref="ArgumentNullException"><paramref name="model"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        public static PublisherRegistration ToPublisherRegistration(
            this SupervisorModel model, bool? disabled = null)
        {
            if (model.Id == null)
            {
                throw new ArgumentException("Id missing", nameof(model));
            }
            if (!HubResource.Parse(model.Id, out _, out var deviceId,
                out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(model));
            }
            return new PublisherRegistration
            {
                IsDisabled = disabled,
                DeviceId = deviceId,
                ModuleId = moduleId,
                ApiKey = model.ApiKey,
                Version = null,
                Connected = model.Connected ?? false,
                SiteId = model.SiteId
            };
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <exception cref="ArgumentNullException"><paramref name="model"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        public static PublisherRegistration ToPublisherRegistration(
            this DiscovererModel model, bool? disabled = null)
        {
            if (model.Id == null)
            {
                throw new ArgumentException("Id missing", nameof(model));
            }
            if (!HubResource.Parse(model.Id, out _, out var deviceId,
                out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(model));
            }
            return new PublisherRegistration
            {
                IsDisabled = disabled,
                DeviceId = deviceId,
                ModuleId = moduleId,
                ApiKey = model.ApiKey,
                Connected = model.Connected ?? false,
                Version = null,
                SiteId = model.SiteId
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="removeApiKey"></param>
        /// <returns></returns>
        public static PublisherModel? ToPublisherModel(this PublisherRegistration? registration,
            bool removeApiKey = false)
        {
            if (registration is null || registration.DeviceId == null)
            {
                return null;
            }
            return new PublisherModel
            {
                Id = HubResource.Format(null, registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                ApiKey = removeApiKey ? null : registration.ApiKey,
                Version = registration.Version,
                Connected = registration.IsConnected() ? true : null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : null
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static SupervisorModel? ToSupervisorModel(this PublisherRegistration? registration)
        {
            if (registration is null || registration.DeviceId == null)
            {
                return null;
            }
            return new SupervisorModel
            {
                Id = HubResource.Format(null, registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                ApiKey = registration.ApiKey,
                Version = registration.Version,
                Connected = registration.IsConnected() ? true : null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : null
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DiscovererModel? ToDiscovererModel(this PublisherRegistration? registration)
        {
            if (registration is null || registration.DeviceId == null)
            {
                return null;
            }
            return new DiscovererModel
            {
                Discovery = DiscoveryMode.Off,
                Id = HubResource.Format(null, registration.DeviceId, registration.ModuleId),
                SiteId = registration.SiteId,
                Version = registration.Version,
                ApiKey = registration.ApiKey,
                DiscoveryConfig = null,
                RequestedMode = null,
                RequestedConfig = null,
                Connected = registration.IsConnected() ? true : null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : null
            };
        }

        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this PublisherRegistration? registration,
            PublisherRegistration? other)
        {
            if (registration is null)
            {
                return other is null;
            }
            return
                other is not null &&
                registration.SiteId == other.SiteId &&
                registration.ApiKey == other.ApiKey;
        }
    }
}
