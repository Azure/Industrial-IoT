// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Model conversion extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoModel ToServiceModel(
            this ApplicationInfoApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationInfoModel {
                ApplicationId = model.ApplicationId,
                ApplicationType = (IIoT.OpcUa.Core.Models.ApplicationType)model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames,
                ProductUri = model.ProductUri,
                SiteId = model.SiteId,
                HostAddresses = model.HostAddresses,
                DiscovererId = model.DiscovererId,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                DiscoveryUrls = model.DiscoveryUrls,
                Capabilities = model.Capabilities,
                NotSeenSince = model.NotSeenSince,
                GatewayServerUri = model.GatewayServerUri,
                Created = model.Created.ToServiceModel(),
                Updated = model.Updated.ToServiceModel(),
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationMethodModel ToServiceModel(
            this AuthenticationMethodApiModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (IIoT.OpcUa.Core.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryConfigModel ToServiceModel(
            this DiscoveryConfigApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryConfigModel {
                AddressRangesToScan = model.AddressRangesToScan,
                NetworkProbeTimeout = model.NetworkProbeTimeoutMs == null ?
                    (TimeSpan?)null : TimeSpan.FromMilliseconds((double)model.NetworkProbeTimeoutMs),
                MaxNetworkProbes = model.MaxNetworkProbes,
                PortRangesToScan = model.PortRangesToScan,
                PortProbeTimeout = model.PortProbeTimeoutMs == null ?
                    (TimeSpan?)null : TimeSpan.FromMilliseconds((double)model.PortProbeTimeoutMs),
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                IdleTimeBetweenScans = model.IdleTimeBetweenScansSec == null ?
                    (TimeSpan?)null : TimeSpan.FromSeconds((double)model.IdleTimeBetweenScansSec),
                ActivationFilter = model.ActivationFilter.ToServiceModel(),
                Locales = model.Locales,
                DiscoveryUrls = model.DiscoveryUrls
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static EndpointActivationFilterModel ToServiceModel(
            this EndpointActivationFilterApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationFilterModel {
                TrustLists = model.TrustLists,
                SecurityPolicies = model.SecurityPolicies,
                SecurityMode = (IIoT.OpcUa.Core.Models.SecurityMode?)model.SecurityMode
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointModel ToServiceModel(
            this EndpointApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                SecurityMode = (IIoT.OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                Certificate = model.Certificate,
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationModel ToServiceModel(
            this EndpointRegistrationApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationModel {
                Id = model.Id,
                Endpoint = model.Endpoint.ToServiceModel(),
                EndpointUrl = model.EndpointUrl,
                AuthenticationMethods = model.AuthenticationMethods?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                SecurityLevel = model.SecurityLevel,
                SiteId = model.SiteId,
                DiscovererId = model.DiscovererId,
                SupervisorId = model.SupervisorId
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RegistryOperationContextModel ToServiceModel(
            this RegistryOperationApiModel model) {
            if (model == null) {
                return null;
            }
            return new RegistryOperationContextModel {
                Time = model.Time,
                AuthorityId = model.AuthorityId
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryEventModel ToServiceModel(
            this DiscoveryEventApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryEventModel {
                TimeStamp = model.TimeStamp,
                Result = null,
                Registration = model.Registration.ToServiceModel(),
                Application = model.Application.ToServiceModel(),
                Index = model.Index
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryResultModel ToServiceModel(
            this DiscoveryResultApiModel model) {
            return new DiscoveryResultModel {
                Id = model.Id,
                DiscoveryConfig = model.DiscoveryConfig.ToServiceModel(),
                Context = model.Context.ToServiceModel(),
                Diagnostics = model.Diagnostics,
                RegisterOnly = model.RegisterOnly
            };
        }
    }
}
