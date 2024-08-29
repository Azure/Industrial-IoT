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
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Twin (endpoint) registration extensions
    /// </summary>
    public static class EndpointRegistrationEx
    {
        /// <summary>
        /// Logical comparison of endpoint registrations
        /// </summary>
        public static IEqualityComparer<EndpointRegistration> Logical =>
            new LogicalEquality();

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="serializer"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this EndpointRegistration registration,
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
        public static DeviceTwinModel Patch(this EndpointRegistration? existing,
            EndpointRegistration? update, IJsonSerializer serializer, TimeProvider timeProvider)
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
                tags.Add(nameof(EndpointRegistration.DiscovererId), update?.DiscovererId);
            }

            if (update?.SiteId != existing?.SiteId)
            {
                tags.Add(nameof(EntityRegistration.SiteId), update?.SiteId);
            }

            tags.Add(nameof(EntityRegistration.DeviceType), update?.DeviceType);

            if (update?.EndpointRegistrationUrl != null &&
                update.EndpointRegistrationUrl != existing?.EndpointRegistrationUrl)
            {
                tags.Add(nameof(EndpointRegistration.EndpointUrlLC),
                    update.EndpointUrlLC);
                tags.Add(nameof(EndpointRegistration.EndpointRegistrationUrl),
                    update.EndpointRegistrationUrl);
            }

            if (update?.SecurityLevel != existing?.SecurityLevel)
            {
                tags.Add(nameof(EndpointRegistration.SecurityLevel), update?.SecurityLevel == null ?
                    VariantValue.Null : serializer.FromObject(update.SecurityLevel?.ToString(CultureInfo.InvariantCulture)));
            }

            var methodEqual = update?.AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                existing?.AuthenticationMethods?.DecodeAsList(), (a, b) => a.IsSameAs(b));
            if (!(methodEqual ?? true))
            {
                tags.Add(nameof(EndpointRegistration.AuthenticationMethods),
                    update?.AuthenticationMethods == null ?
                    VariantValue.Null : serializer.FromObject(update.AuthenticationMethods));
            }

            // Endpoint Property

            if (update?.EndpointUrl != null &&
                update.EndpointUrl != existing?.EndpointUrl)
            {
                desired.Add(nameof(EndpointRegistration.EndpointUrl),
                    update.EndpointUrl);
            }

            var urlsEqual = update?.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                existing?.AlternativeUrls?.DecodeAsList());
            if (!(urlsEqual ?? true))
            {
                desired.Add(nameof(EndpointRegistration.AlternativeUrls),
                    update?.AlternativeUrls == null ?
                        VariantValue.Null : serializer.FromObject(update.AlternativeUrls));
            }

            if (update?.SecurityMode != null &&
                update.SecurityMode != existing?.SecurityMode)
            {
                desired.Add(nameof(EndpointRegistration.SecurityMode),
                    update?.SecurityMode == null ?
                        VariantValue.Null : serializer.FromObject(update.SecurityMode.ToString()));
            }

            if (update?.SecurityPolicy != null &&
                update.SecurityPolicy != existing?.SecurityPolicy)
            {
                desired.Add(nameof(EndpointRegistration.SecurityPolicy),
                    update.SecurityPolicy);
            }

            if (update?.Thumbprint != existing?.Thumbprint)
            {
                desired.Add(nameof(EndpointRegistration.Thumbprint), update?.Thumbprint);
            }

            // Recalculate identity

            var reportedEndpointUrl = existing?.EndpointRegistrationUrl;
            if (update?.EndpointRegistrationUrl != null)
            {
                reportedEndpointUrl = update.EndpointRegistrationUrl;
            }
            if (reportedEndpointUrl == null)
            {
                throw new ArgumentException(nameof(EndpointRegistration.EndpointUrl));
            }
            var applicationId = existing?.ApplicationId;
            if (update?.ApplicationId != null)
            {
                applicationId = update.ApplicationId;
            }
            if (applicationId == null)
            {
                throw new ArgumentException(nameof(EndpointRegistration.ApplicationId));
            }
            var securityMode = existing?.SecurityMode;
            if (update?.SecurityMode != null)
            {
                securityMode = update.SecurityMode;
            }
            var securityPolicy = existing?.SecurityPolicy;
            if (update?.SecurityPolicy != null)
            {
                securityPolicy = update.SecurityPolicy;
            }

            var id = EndpointInfoModelEx.CreateEndpointId(
                applicationId, reportedEndpointUrl, securityMode, securityPolicy);
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
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static EndpointRegistration? ToEndpointRegistration(this DeviceTwinModel twin,
            IReadOnlyDictionary<string, VariantValue> properties)
        {
            if (twin == null)
            {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, VariantValue>();

            return new EndpointRegistration
            {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,
                Connected = false,

                // Tags
                IsDisabled =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.IsDisabled), twin.IsDisabled()),
                NotSeenSince =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.NotSeenSince), (DateTime?)null),

                DiscovererId =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.DiscovererId), (string?)null),
                ApplicationId =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.ApplicationId), (string?)null),
                SecurityLevel =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.SecurityLevel), (int?)null),
                AuthenticationMethods =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.AuthenticationMethods), (Dictionary<string, AuthenticationMethodModel>?)null),
                EndpointRegistrationUrl =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.EndpointRegistrationUrl), (string?)null),

                // Properties

                Type =
                    properties.GetValueOrDefault(Constants.TwinPropertyTypeKey, (string?)null),
                State =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.State), (EndpointConnectivityState?)null),
                SiteId =
                    properties.GetValueOrDefault(Constants.TwinPropertySiteKey,
                        tags.GetValueOrDefault(nameof(EndpointRegistration.SiteId), (string?)null)),
                EndpointUrl =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.EndpointUrl), (string?)null),
                AlternativeUrls =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.AlternativeUrls), (Dictionary<string, string>?)null),
                SecurityMode =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.SecurityMode), (SecurityMode?)null),
                SecurityPolicy =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.SecurityPolicy), (string?)null),
                Thumbprint =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.Thumbprint), (string?)null)
            };
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
        /// <returns></returns>
        public static EndpointRegistration? ToEndpointRegistration(this DeviceTwinModel? twin,
            bool onlyServerState)
        {
            if (twin == null)
            {
                return null;
            }
            twin.Tags ??= new Dictionary<string, VariantValue>();

            var consolidated =
                ToEndpointRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Desired == null) ? null :
                ToEndpointRegistration(twin, twin.Desired);

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
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static EndpointInfoModel? ToServiceModel(this EndpointRegistration? registration)
        {
            if (registration is null || registration.ApplicationId == null)
            {
                return null;
            }
            return new EndpointInfoModel
            {
                ApplicationId = registration.ApplicationId,
                Registration = new EndpointRegistrationModel
                {
                    Id = registration.DeviceId ?? string.Empty,
                    SiteId = string.IsNullOrEmpty(registration.SiteId) ?
                        null : registration.SiteId,
                    DiscovererId = string.IsNullOrEmpty(registration.DiscovererId) ?
                        null : registration.DiscovererId,
                    AuthenticationMethods = registration.AuthenticationMethods?.DecodeAsList(),
                    SecurityLevel = registration.SecurityLevel,
                    EndpointUrl = string.IsNullOrEmpty(registration.EndpointRegistrationUrl) ?
                        (string.IsNullOrEmpty(registration.EndpointUrl) ?
                            registration.EndpointUrlLC : registration.EndpointUrl) : registration.EndpointRegistrationUrl,
                    Endpoint = new EndpointModel
                    {
                        Url = string.IsNullOrEmpty(registration.EndpointUrl) ?
                            (registration.EndpointUrlLC ?? string.Empty) : registration.EndpointUrl,
                        AlternativeUrls = registration.AlternativeUrls?.DecodeAsList().ToHashSetSafe(),
                        SecurityMode = registration.SecurityMode == SecurityMode.NotNone ?
                            null : registration.SecurityMode,
                        SecurityPolicy = string.IsNullOrEmpty(registration.SecurityPolicy) ?
                            null : registration.SecurityPolicy,
                        Certificate = registration.Thumbprint
                    }
                },
                NotSeenSince = registration.NotSeenSince.ToDateTimeOffsetUtc(),
                EndpointState = registration.State
            };
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <param name="discoverId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        public static EndpointRegistration ToEndpointRegistration(this EndpointInfoModel model,
            bool? disabled = null, string? discoverId = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            return new EndpointRegistration
            {
                IsDisabled = disabled,
                NotSeenSince = model.NotSeenSince?.UtcDateTime,
                ApplicationId = model.ApplicationId,
                SiteId = model.Registration?.SiteId,
                DiscovererId = discoverId ?? model.Registration?.DiscovererId,
                SecurityLevel = model.Registration?.SecurityLevel,
                EndpointRegistrationUrl = model.Registration?.EndpointUrl ??
                    model.Registration?.Endpoint?.Url,
                EndpointUrl = model.Registration?.Endpoint?.Url,
                AlternativeUrls = model.Registration?.Endpoint?.AlternativeUrls?.ToList()?
                    .EncodeAsDictionary(),
                AuthenticationMethods = model.Registration?.AuthenticationMethods?
                    .EncodeAsDictionary(),
                SecurityMode = model.Registration?.Endpoint?.SecurityMode ??
                    SecurityMode.NotNone,
                SecurityPolicy = model.Registration?.Endpoint?.SecurityPolicy,
                Thumbprint = model.Registration?.Endpoint?.Certificate
            };
        }

        /// <summary>
        /// Get site or gateway id from registration
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetSiteOrGatewayId(this EndpointRegistration registration)
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
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this EndpointRegistration? registration,
            EndpointRegistration? other)
        {
            if (registration is null)
            {
                return other is null;
            }
            return
                other is not null &&
                registration.EndpointUrl == other.EndpointUrl &&
                registration.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                    other.AlternativeUrls.DecodeAsList()) &&
                registration.SecurityPolicy == other.SecurityPolicy &&
                registration.SecurityMode == other.SecurityMode &&
                registration.Thumbprint == other.Thumbprint;
        }

        /// <summary>
        /// Compares for logical equality
        /// </summary>
        private class LogicalEquality : IEqualityComparer<EndpointRegistration>
        {
            /// <inheritdoc />
            public bool Equals(EndpointRegistration? x, EndpointRegistration? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                if (y is null)
                {
                    return false;
                }
                if (x.EndpointUrlLC != y.EndpointUrlLC)
                {
                    return false;
                }
                if (x.ApplicationId != y.ApplicationId)
                {
                    return false;
                }
                if (x.SecurityPolicy != y.SecurityPolicy)
                {
                    return false;
                }
                if (x.SecurityMode != y.SecurityMode)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc />
            public int GetHashCode(EndpointRegistration obj)
            {
                var hashCode = 1200389859;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.EndpointUrlLC ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.ApplicationId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<SecurityMode>.Default.GetHashCode(
                        obj.SecurityMode ?? SecurityMode.NotNone);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.SecurityPolicy ?? string.Empty);
                return hashCode;
            }
        }
    }
}
