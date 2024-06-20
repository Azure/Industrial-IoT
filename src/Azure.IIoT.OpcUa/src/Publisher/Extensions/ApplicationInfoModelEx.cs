// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationInfoModelEx
    {
        /// <summary>
        /// Get logical equality comparer
        /// </summary>
        public static IEqualityComparer<ApplicationInfoModel> LogicalEquality { get; } =
            new LogicalComparer();

        /// <summary>
        /// Create unique application id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        public static string CreateApplicationId(ApplicationInfoModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            var siteOrGatewayId = model.SiteId;
            return CreateApplicationId(siteOrGatewayId, model.ApplicationUri,
                model.ApplicationType);
        }

        /// <summary>
        /// Create unique application id
        /// </summary>
        /// <param name="siteOrGatewayId"></param>
        /// <param name="applicationUri"></param>
        /// <param name="applicationType"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(applicationUri))]
        public static string? CreateApplicationId(string? siteOrGatewayId,
            string? applicationUri, ApplicationType? applicationType)
        {
            if (string.IsNullOrEmpty(applicationUri))
            {
                return null;
            }
#pragma warning disable CA1308 // Normalize strings to uppercase
            applicationUri = applicationUri.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            var type = applicationType ?? ApplicationType.Server;
            var id = $"{siteOrGatewayId ?? ""}-{type}-{applicationUri}";
            var prefix = applicationType == ApplicationType.Client ? "uac" : "uas";
            return prefix + id.ToSha1Hash();
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IReadOnlyList<ApplicationInfoModel>? model,
            IReadOnlyList<ApplicationInfoModel>? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.Count != that.Count)
            {
                return false;
            }
            foreach (var a in model)
            {
                if (!that.Any(b => b.IsSameAs(a)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ApplicationInfoModel? model,
            ApplicationInfoModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            return
                StringComparer.OrdinalIgnoreCase.Equals(that.ApplicationUri, model.ApplicationUri) &&
                that.ApplicationType == model.ApplicationType;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ApplicationInfoModel? Clone(this ApplicationInfoModel? model,
            TimeProvider timeProvider)
        {
            return model == null ? null : (model with
            {
                LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                Capabilities = model.Capabilities
                    .ToHashSetSafe(),
                HostAddresses = model.HostAddresses
                    .ToHashSetSafe(),
                DiscoveryUrls = model.DiscoveryUrls
                    .ToHashSetSafe(),
                Created = model.Created.Clone(timeProvider),
                Updated = model.Updated.Clone(timeProvider)
            });
        }

        /// <summary>
        /// Convert to registration request
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ApplicationRegistrationRequestModel ToRegistrationRequest(
            this ApplicationInfoModel model, OperationContextModel? context = null)
        {
            return new ApplicationRegistrationRequestModel
            {
                ApplicationName = model.ApplicationName,
                ApplicationType = model.ApplicationType,
                Capabilities = model.Capabilities,
                ApplicationUri = model.ApplicationUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                DiscoveryUrls = model.DiscoveryUrls,
                GatewayServerUri = model.GatewayServerUri,
                LocalizedNames = model.LocalizedNames,
                Locale = model.Locale,
                ProductUri = model.ProductUri,
                SiteId = model.SiteId,
                Context = context
            };
        }

        /// <summary>
        /// Convert registration request to application info model
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ApplicationInfoModel ToApplicationInfo(
            this ApplicationRegistrationRequestModel request,
            OperationContextModel context)
        {
            return new ApplicationInfoModel
            {
                ApplicationName = request.ApplicationName,
                LocalizedNames = request.LocalizedNames,
                ProductUri = request.ProductUri,
                DiscoveryUrls = request.DiscoveryUrls,
                DiscoveryProfileUri = request.DiscoveryProfileUri,
                ApplicationType = request.ApplicationType ?? ApplicationType.Server,
                ApplicationUri = request.ApplicationUri!,
                Locale = request.Locale,
                Capabilities = request.Capabilities,
                GatewayServerUri = request.GatewayServerUri,
                SiteId = request.SiteId,
                NotSeenSince = null,
                Created = context,
                Updated = null,
                ApplicationId = null!,
                DiscovererId = null,
                HostAddresses = null
            };
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="model"></param>
        public static ApplicationInfoModel Update(this ApplicationInfoModel application,
            ApplicationInfoModel model)
        {
            application.ApplicationId = model.ApplicationId;
            application.ApplicationName = model.ApplicationName;
            application.LocalizedNames = model.LocalizedNames;
            application.ApplicationType = model.ApplicationType;
            application.ApplicationUri = model.ApplicationUri;
            application.Capabilities = model.Capabilities;
            application.DiscoveryProfileUri = model.DiscoveryProfileUri;
            application.HostAddresses = model.HostAddresses;
            application.DiscoveryUrls = model.DiscoveryUrls;
            application.NotSeenSince = model.NotSeenSince;
            application.ProductUri = model.ProductUri;
            application.SiteId = model.SiteId;
            application.DiscovererId = model.DiscovererId;
            application.GatewayServerUri = model.GatewayServerUri;
            application.Created = model.Created;
            application.Updated = model.Updated;
            application.Locale = model.Locale;
            return application;
        }

        /// <summary>
        /// Patch application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="request"></param>
        public static ApplicationInfoModel Patch(this ApplicationInfoModel application,
            ApplicationRegistrationUpdateModel request)
        {
            // Update from update request
            if (request.ApplicationName != null)
            {
                application.ApplicationName = string.IsNullOrEmpty(request.ApplicationName) ?
                    null : request.ApplicationName;
            }
            if (request.LocalizedNames != null)
            {
                application.LocalizedNames = request.LocalizedNames;
            }
            if (request.ProductUri != null)
            {
                application.ProductUri = string.IsNullOrEmpty(request.ProductUri) ?
                    null : request.ProductUri;
            }
            if (request.GatewayServerUri != null)
            {
                application.GatewayServerUri = string.IsNullOrEmpty(request.GatewayServerUri) ?
                    null : request.GatewayServerUri;
            }
            if (request.Capabilities != null)
            {
                application.Capabilities = request.Capabilities.Count == 0 ?
                    null : request.Capabilities;
            }
            if (request.DiscoveryUrls != null)
            {
                application.DiscoveryUrls = request.DiscoveryUrls.Count == 0 ?
                    null : request.DiscoveryUrls;
            }
            if (request.Locale != null)
            {
                application.Locale = string.IsNullOrEmpty(request.Locale) ?
                    null : request.Locale;
            }
            if (request.DiscoveryProfileUri != null)
            {
                application.DiscoveryProfileUri = string.IsNullOrEmpty(request.DiscoveryProfileUri) ?
                    null : request.DiscoveryProfileUri;
            }
            return application;
        }

        /// <summary>
        /// Returns the site or supervisor id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string? GetSiteOrGatewayId(this ApplicationInfoModel model)
        {
            if (string.IsNullOrEmpty(model.SiteId))
            {
                return model.DiscovererId;
            }
            return model.SiteId;
        }

        /// <summary>
        /// Compares for logical equality - applications are logically equivalent if they
        /// have the same uri, type, and site location or supervisor that registered.
        /// </summary>
        private class LogicalComparer : IEqualityComparer<ApplicationInfoModel>
        {
            /// <inheritdoc />
            public bool Equals(ApplicationInfoModel? x, ApplicationInfoModel? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                if (y is null)
                {
                    return false;
                }
                if (x.GetSiteOrGatewayId() != y.GetSiteOrGatewayId())
                {
                    return false;
                }
                if (x.ApplicationType != y.ApplicationType)
                {
                    return false;
                }
                if (!StringComparer.OrdinalIgnoreCase.Equals(x.ApplicationUri, y.ApplicationUri))
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc />
            public int GetHashCode(ApplicationInfoModel obj)
            {
                var hashCode = 1200389859;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<ApplicationType?>.Default.GetHashCode(
                        obj.ApplicationType);
#pragma warning disable CA1308 // Normalize strings to uppercase
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        obj.ApplicationUri?.ToLowerInvariant() ?? string.Empty);
#pragma warning restore CA1308 // Normalize strings to uppercase
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        obj.GetSiteOrGatewayId() ?? string.Empty);
                return hashCode;
            }
        }
    }
}
