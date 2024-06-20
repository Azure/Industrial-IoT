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
    using System.Linq;

    /// <summary>
    /// Aapplication registration persisted and comparable
    /// </summary>
    public static class ApplicationRegistrationEx
    {
        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="serializer"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this ApplicationRegistration registration,
            IJsonSerializer serializer, TimeProvider timeProvider)
        {
            return Patch(null, registration, serializer, timeProvider);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="serializer"></param>
        /// <param name="timeProvider"></param>
        /// <exception cref="ArgumentException"></exception>
        public static DeviceTwinModel Patch(this ApplicationRegistration? existing,
            ApplicationRegistration update, IJsonSerializer serializer, TimeProvider timeProvider)
        {
            var tags = new Dictionary<string, VariantValue>();
            var desired = new Dictionary<string, VariantValue>();

            // Tags

            if (update?.ApplicationId != null &&
                update.ApplicationId != existing?.ApplicationId)
            {
                tags.Add(nameof(ApplicationId), update.ApplicationId);
            }

            if (update?.IsDisabled != null &&
                update.IsDisabled != existing?.IsDisabled)
            {
                tags.Add(nameof(EntityRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                tags.Add(nameof(EntityRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    timeProvider.GetUtcNow().UtcDateTime : (DateTime?)null);
            }

            if (update?.SiteOrGatewayId != existing?.SiteOrGatewayId)
            {
                tags.Add(nameof(EntityRegistration.SiteOrGatewayId), update?.SiteOrGatewayId);
            }

            if (update?.DiscovererId != existing?.DiscovererId)
            {
                tags.Add(nameof(ApplicationRegistration.DiscovererId), update?.DiscovererId);
            }

            if (update?.SiteId != existing?.SiteId)
            {
                tags.Add(nameof(EntityRegistration.SiteId), update?.SiteId);
            }

            tags.Add(nameof(EntityRegistration.DeviceType), update?.DeviceType);

            if (update?.ApplicationType != null &&
                update.ApplicationType != existing?.ApplicationType)
            {
                tags.Add(nameof(ApplicationRegistration.ApplicationType),
                    serializer.FromObject(update.ApplicationType.ToString()));
                tags.Add(nameof(ApplicationType.Server),
                    update.ApplicationType != ApplicationType.Client);
                tags.Add(nameof(ApplicationType.Client),
                    update.ApplicationType != ApplicationType.Server &&
                    update.ApplicationType != ApplicationType.DiscoveryServer);
                tags.Add(nameof(ApplicationType.DiscoveryServer),
                    update.ApplicationType == ApplicationType.DiscoveryServer);
            }

            if (update?.ApplicationUri != existing?.ApplicationUri)
            {
                tags.Add(nameof(ApplicationRegistration.ApplicationUri),
                    update?.ApplicationUri);
                tags.Add(nameof(ApplicationRegistration.ApplicationUriLC),
                    update?.ApplicationUriLC);
            }

            if (update?.RecordId != existing?.RecordId)
            {
                tags.Add(nameof(ApplicationRegistration.RecordId),
                    update?.RecordId);
            }

            if (update?.ApplicationName != existing?.ApplicationName)
            {
                tags.Add(nameof(ApplicationRegistration.ApplicationName),
                    update?.ApplicationName);
            }

            if (update?.Locale != existing?.Locale)
            {
                tags.Add(nameof(ApplicationRegistration.Locale),
                    update?.Locale);
            }

            if (update?.DiscoveryProfileUri != existing?.DiscoveryProfileUri)
            {
                tags.Add(nameof(ApplicationRegistration.DiscoveryProfileUri),
                    update?.DiscoveryProfileUri);
            }

            if (update?.GatewayServerUri != existing?.GatewayServerUri)
            {
                tags.Add(nameof(ApplicationRegistration.GatewayServerUri),
                    update?.GatewayServerUri);
            }

            if (update?.ProductUri != existing?.ProductUri)
            {
                tags.Add(nameof(ApplicationRegistration.ProductUri), update?.ProductUri);
            }

            var urlUpdate = update?.DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                existing?.DiscoveryUrls?.DecodeAsList());
            if (!(urlUpdate ?? true))
            {
                tags.Add(nameof(ApplicationRegistration.DiscoveryUrls),
                    update?.DiscoveryUrls == null ?
                    VariantValue.Null : serializer.FromObject(update.DiscoveryUrls));
            }

            var capsUpdate = update?.Capabilities.DecodeAsSet().SetEqualsSafe(
                existing?.Capabilities?.DecodeAsSet());
            if (!(capsUpdate ?? true))
            {
                tags.Add(nameof(ApplicationRegistration.Capabilities),
                    update?.Capabilities == null ?
                    VariantValue.Null : serializer.FromObject(update.Capabilities));
            }

            var namesUpdate = update?.LocalizedNames.DictionaryEqualsSafe(
                existing?.LocalizedNames);
            if (!(namesUpdate ?? true))
            {
                tags.Add(nameof(ApplicationRegistration.LocalizedNames),
                    update?.LocalizedNames == null ?
                    VariantValue.Null : serializer.FromObject(update.LocalizedNames));
            }

            var hostsUpdate = update?.HostAddresses.DecodeAsList().SequenceEqualsSafe(
                existing?.HostAddresses?.DecodeAsList());
            if (!(hostsUpdate ?? true))
            {
                tags.Add(nameof(ApplicationRegistration.HostAddresses),
                    update?.HostAddresses == null ?
                    VariantValue.Null : serializer.FromObject(update.HostAddresses));
            }

            if (update?.CreateAuthorityId != existing?.CreateAuthorityId)
            {
                tags.Add(nameof(ApplicationRegistration.CreateAuthorityId),
                    update?.CreateAuthorityId);
            }
            if (update?.CreateTime != existing?.CreateTime)
            {
                tags.Add(nameof(ApplicationRegistration.CreateTime),
                    update?.CreateTime);
            }

            if (update?.UpdateAuthorityId != existing?.UpdateAuthorityId)
            {
                tags.Add(nameof(ApplicationRegistration.UpdateAuthorityId),
                    update?.UpdateAuthorityId);
            }
            if (update?.UpdateTime != existing?.UpdateTime)
            {
                tags.Add(nameof(ApplicationRegistration.UpdateTime),
                    update?.UpdateTime);
            }

            // Recalculate identity

            var applicationUri = existing?.ApplicationUri;
            if (update?.ApplicationUri != null)
            {
                applicationUri = update?.ApplicationUri;
            }
            if (applicationUri == null)
            {
                throw new ArgumentException(nameof(ApplicationRegistration.ApplicationUri));
            }

            var siteOrGatewayId = (existing?.SiteOrGatewayId) ??
                (update?.SiteOrGatewayId) ?? EntityRegistration.UnknownGatewayOrSiteId;

            var applicationType = existing?.ApplicationType;
            if (update?.ApplicationType != null)
            {
                applicationType = update?.ApplicationType ?? ApplicationType.Server;
            }

            var id = ApplicationInfoModelEx.CreateApplicationId(
                siteOrGatewayId, applicationUri, applicationType);
            return new DeviceTwinModel
            {
                Id = id ?? string.Empty,
                // Force creation of new identity
                Etag = existing?.DeviceId != id ? string.Empty : existing?.Etag ?? string.Empty,
                Tags = tags,
                Desired = desired
            };
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static ApplicationRegistration? ToApplicationRegistration(this DeviceTwinModel twin)
        {
            if (twin == null)
            {
                return null;
            }
            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();
            return new ApplicationRegistration
            {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault(nameof(ApplicationRegistration.IsDisabled), twin.IsDisabled()),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.NotSeenSince), null),
                SiteId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.SiteId), null),

                ApplicationName =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ApplicationName), null),
                Locale =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.Locale), null),
                LocalizedNames =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.LocalizedNames), null),
                ApplicationUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ApplicationUri), null),
                RecordId =
                    tags.GetValueOrDefault<uint>(nameof(ApplicationRegistration.RecordId), null),
                ProductUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.ProductUri), null),
                DiscovererId =
                    tags.GetValueOrDefault(nameof(ApplicationRegistration.DiscovererId),
                        tags.GetValueOrDefault<string>("SupervisorId", null)),
                DiscoveryProfileUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.DiscoveryProfileUri), null),
                GatewayServerUri =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.GatewayServerUri), null),
                ApplicationType =
                    tags.GetValueOrDefault<ApplicationType>(nameof(ApplicationType), null),
                Capabilities =
                    tags.GetValueOrDefault<Dictionary<string, bool>>(nameof(ApplicationRegistration.Capabilities), null),
                HostAddresses =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.HostAddresses), null),
                DiscoveryUrls =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(ApplicationRegistration.DiscoveryUrls), null),

                CreateTime =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.CreateTime), null),
                CreateAuthorityId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.CreateAuthorityId), null),
                UpdateTime =
                    tags.GetValueOrDefault<DateTime>(nameof(ApplicationRegistration.UpdateTime), null),
                UpdateAuthorityId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationRegistration.UpdateAuthorityId), null)
            };
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <param name="etag"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        public static ApplicationRegistration ToApplicationRegistration(
            this ApplicationInfoModel model, bool? disabled = null, string? etag = null,
            uint? recordId = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            return new ApplicationRegistration
            {
                IsDisabled = disabled,
                DiscovererId = model.DiscovererId,
                Etag = etag,
                RecordId = recordId,
                SiteId = model.SiteId,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames,
                HostAddresses = model.HostAddresses?.ToList().EncodeAsDictionary(),
                ApplicationType = model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                NotSeenSince = model.NotSeenSince?.UtcDateTime,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                GatewayServerUri = model.GatewayServerUri,
                Capabilities = model.Capabilities.EncodeAsDictionary(true),
                DiscoveryUrls = model.DiscoveryUrls?.ToList().EncodeAsDictionary(),
                CreateAuthorityId = model.Created?.AuthorityId,
                CreateTime = model.Created?.Time.UtcDateTime,
                UpdateAuthorityId = model.Updated?.AuthorityId,
                UpdateTime = model.Updated?.Time.UtcDateTime,
                Version = null,
                Connected = false
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static ApplicationInfoModel? ToServiceModel(this ApplicationRegistration? registration)
        {
            if (registration is null || registration.ApplicationId == null)
            {
                return null;
            }
            return new ApplicationInfoModel
            {
                ApplicationId = registration.ApplicationId,
                ApplicationName = registration.ApplicationName,
                Locale = registration.Locale,
                LocalizedNames = registration.LocalizedNames,
                HostAddresses = registration.HostAddresses.DecodeAsList().ToHashSetSafe(),
                NotSeenSince = registration.NotSeenSince.ToDateTimeOffsetUtc(),
                ApplicationType = registration.ApplicationType ?? ApplicationType.Server,
                ApplicationUri = string.IsNullOrEmpty(registration.ApplicationUri) ?
                    registration.ApplicationUriLC! : registration.ApplicationUri,
                ProductUri = registration.ProductUri,
                SiteId = string.IsNullOrEmpty(registration.SiteId) ?
                    null : registration.SiteId,
                DiscovererId = string.IsNullOrEmpty(registration.DiscovererId) ?
                    null : registration.DiscovererId,
                DiscoveryUrls = registration.DiscoveryUrls.DecodeAsList().ToHashSetSafe(),
                DiscoveryProfileUri = registration.DiscoveryProfileUri,
                GatewayServerUri = registration.GatewayServerUri,
                Capabilities = registration.Capabilities?.DecodeAsSet(),
                Created = ToOperationModel(registration.CreateAuthorityId, registration.CreateTime),
                Updated = ToOperationModel(registration.UpdateAuthorityId, registration.UpdateTime)
            };
        }

        /// <summary>
        /// Get site or gateway id from registration
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetSiteOrGatewayId(this ApplicationRegistration registration)
        {
            var siteOrGatewayId = registration?.SiteId;
            if (siteOrGatewayId == null)
            {
                var id = registration?.DiscovererId;
                if (id != null &&
                    !HubResource.Parse(id, out _, out siteOrGatewayId, out _, out var error))
                {
                    throw new ArgumentException(error, nameof(registration));
                }
            }
            return siteOrGatewayId ?? EntityRegistration.UnknownGatewayOrSiteId;
        }

        /// <summary>
        /// Create operation model
        /// </summary>
        /// <param name="authorityId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static OperationContextModel? ToOperationModel(
            string? authorityId, DateTime? time)
        {
            if (string.IsNullOrEmpty(authorityId) && time == null)
            {
                return null;
            }
            return new OperationContextModel
            {
                AuthorityId = authorityId,
                Time = time.ToDateTimeOffsetUtc() ?? DateTimeOffset.MinValue
            };
        }

        /// <summary>
        /// Set the kind to utc and convert to date time offset
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTimeOffset? ToDateTimeOffsetUtc(this DateTime? time)
        {
            if (time == null)
            {
                return null;
            }
            return new DateTimeOffset(DateTime.SpecifyKind(time.Value, DateTimeKind.Utc));
        }
    }
}
