// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Model conversion extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoApiModel ToApiModel(
            this ApplicationInfoModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationInfoApiModel {
                ApplicationId = model.ApplicationId,
                ApplicationType = (Core.Models.ApplicationType)model.ApplicationType,
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
                Created = model.Created.ToApiModel(),
                Updated = model.Updated.ToApiModel(),
            };
        }

        /// <summary>
        /// Create service model
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
                ApplicationType = (OpcUa.Core.Models.ApplicationType)model.ApplicationType,
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
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoListApiModel ToApiModel(
            this ApplicationInfoListModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationInfoListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoListModel ToServiceModel(
            this ApplicationInfoListApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationInfoListModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationApiModel ToApiModel(
            this ApplicationRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationApiModel {
                Application = model.Application.ToApiModel(),
                Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(
            this ApplicationRegistrationApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationModel {
                Application = model.Application.ToServiceModel(),
                Endpoints = model.Endpoints?
                    .Select(e => e.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationQueryApiModel ToApiModel(
            this ApplicationRegistrationQueryModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationQueryApiModel {
                ApplicationType = (Core.Models.ApplicationType?)model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                Capability = model.Capability,
                SiteOrGatewayId = model.SiteOrGatewayId,
                IncludeNotSeenSince = model.IncludeNotSeenSince,
                GatewayServerUri = model.GatewayServerUri,
                DiscovererId = model.DiscovererId,
                DiscoveryProfileUri = model.DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationQueryModel ToServiceModel(
            this ApplicationRegistrationQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationQueryModel {
                ApplicationType = (OpcUa.Core.Models.ApplicationType?)model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                Capability = model.Capability,
                SiteOrGatewayId = model.SiteOrGatewayId,
                IncludeNotSeenSince = model.IncludeNotSeenSince,
                GatewayServerUri = model.GatewayServerUri,
                DiscovererId = model.DiscovererId,
                DiscoveryProfileUri = model.DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationRequestApiModel ToApiModel(
            this ApplicationRegistrationRequestModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationRequestApiModel {
                ApplicationType = (Core.Models.ApplicationType?)model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                ProductUri = model.ProductUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                DiscoveryUrls = model.DiscoveryUrls,
                SiteId = model.SiteId,
                GatewayServerUri = model.GatewayServerUri,
                Capabilities = model.Capabilities
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationRequestModel ToServiceModel(
            this ApplicationRegistrationRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationRequestModel {
                ApplicationType = (OpcUa.Core.Models.ApplicationType?)model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                ProductUri = model.ProductUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                DiscoveryUrls = model.DiscoveryUrls,
                SiteId = model.SiteId,
                GatewayServerUri = model.GatewayServerUri,
                Capabilities = model.Capabilities
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationResponseApiModel ToApiModel(
            this ApplicationRegistrationResultModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationResponseApiModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationResultModel ToServiceModel(
            this ApplicationRegistrationResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationResultModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationUpdateApiModel ToApiModel(
            this ApplicationRegistrationUpdateModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationUpdateApiModel {
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                ProductUri = model.ProductUri,
                Capabilities = model.Capabilities,
                DiscoveryUrls = model.DiscoveryUrls,
                GatewayServerUri = model.GatewayServerUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationUpdateModel ToServiceModel(
            this ApplicationRegistrationUpdateApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationUpdateModel {
                ApplicationName = model.ApplicationName,
                Locale = model.Locale,
                LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                ProductUri = model.ProductUri,
                Capabilities = model.Capabilities,
                DiscoveryUrls = model.DiscoveryUrls,
                GatewayServerUri = model.GatewayServerUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationSiteListApiModel ToApiModel(
            this ApplicationSiteListModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationSiteListApiModel {
                ContinuationToken = model.ContinuationToken,
                Sites = model.Sites?.ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationSiteListModel ToServiceModel(
            this ApplicationSiteListApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationSiteListModel {
                ContinuationToken = model.ContinuationToken,
                Sites = model.Sites?.ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationMethodApiModel ToApiModel(
            this AuthenticationMethodModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodApiModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (Core.Models.CredentialType?)model.CredentialType ??
                    Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create service model
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
                CredentialType = (OpcUa.Core.Models.CredentialType?)model.CredentialType ??
                   OpcUa.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererApiModel ToApiModel(
            this DiscovererModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                RequestedMode = (DiscoveryMode?)model.RequestedMode,
                RequestedConfig = model.RequestedConfig.ToApiModel(),
                Discovery = (DiscoveryMode?)model.Discovery,
                DiscoveryConfig = model.DiscoveryConfig.ToApiModel(),
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererModel ToServiceModel(
            this DiscovererApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                RequestedMode = (OpcUa.Registry.Models.DiscoveryMode?)model.RequestedMode,
                RequestedConfig = model.RequestedConfig.ToServiceModel(),
                Discovery = (OpcUa.Registry.Models.DiscoveryMode?)model.Discovery,
                DiscoveryConfig = model.DiscoveryConfig.ToServiceModel(),
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererListApiModel ToApiModel(
            this DiscovererListModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscovererQueryModel ToServiceModel(
            this DiscovererQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererQueryModel {
                SiteId = model.SiteId,
                Connected = model.Connected,
                Discovery = (OpcUa.Registry.Models.DiscoveryMode?)model.Discovery
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscovererUpdateModel ToServiceModel(
            this DiscovererUpdateApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererUpdateModel {
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                Discovery = (OpcUa.Registry.Models.DiscoveryMode?)model.Discovery,
                DiscoveryConfig = model.DiscoveryConfig.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryConfigApiModel ToApiModel(
            this DiscoveryConfigModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryConfigApiModel {
                AddressRangesToScan = model.AddressRangesToScan,
                NetworkProbeTimeout = model.NetworkProbeTimeout,
                MaxNetworkProbes = model.MaxNetworkProbes,
                PortRangesToScan = model.PortRangesToScan,
                PortProbeTimeout = model.PortProbeTimeout,
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                ActivationFilter = model.ActivationFilter.ToApiModel(),
                Locales = model.Locales,
                DiscoveryUrls = model.DiscoveryUrls
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
                NetworkProbeTimeout = model.NetworkProbeTimeout,
                MaxNetworkProbes = model.MaxNetworkProbes,
                PortRangesToScan = model.PortRangesToScan,
                PortProbeTimeout = model.PortProbeTimeout,
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                ActivationFilter = model.ActivationFilter.ToServiceModel(),
                Locales = model.Locales,
                DiscoveryUrls = model.DiscoveryUrls
            };
        }

        /// <summary>
        /// Convert to Api model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryCancelApiModel ToApiModel(
            this DiscoveryCancelModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryCancelApiModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryCancelModel ToServiceModel(
            this DiscoveryCancelApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryCancelModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Convert to Api model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryRequestApiModel ToApiModel(
            this DiscoveryRequestModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryRequestApiModel {
                Id = model.Id,
                Configuration = model.Configuration.ToApiModel(),
                Discovery = (DiscoveryMode?)model.Discovery
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryRequestModel ToServiceModel(
            this DiscoveryRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryRequestModel {
                Id = model.Id,
                Configuration = model.Configuration.ToServiceModel(),
                Discovery = (OpcUa.Registry.Models.DiscoveryMode?)model.Discovery
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationFilterApiModel ToApiModel(
            this EndpointActivationFilterModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationFilterApiModel {
                TrustLists = model.TrustLists,
                SecurityPolicies = model.SecurityPolicies,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode
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
                SecurityMode = (OpcUa.Core.Models.SecurityMode?)model.SecurityMode
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationStatusApiModel ToApiModel(
            this EndpointActivationStatusModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusApiModel {
                Id = model.Id,
                ActivationState = (EndpointActivationState?)model.ActivationState
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationStatusModel ToServiceModel(
            this EndpointActivationStatusApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusModel {
                Id = model.Id,
                ActivationState = (OpcUa.Registry.Models.EndpointActivationState?)model.ActivationState
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointApiModel ToApiModel(
            this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointApiModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                Certificate = model.Certificate,
            };
        }

        /// <summary>
        /// Create service model
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
                SecurityMode = (OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                Certificate = model.Certificate,
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoApiModel ToApiModel(
            this EndpointInfoModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoApiModel {
                ApplicationId = model.ApplicationId,
                NotSeenSince = model.NotSeenSince,
                Registration = model.Registration.ToApiModel(),
                ActivationState = (EndpointActivationState?)model.ActivationState,
                EndpointState = (EndpointConnectivityState?)model.EndpointState,
                OutOfSync = model.OutOfSync
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoModel ToServiceModel(
            this EndpointInfoApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoModel {
                ApplicationId = model.ApplicationId,
                NotSeenSince = model.NotSeenSince,
                Registration = model.Registration.ToServiceModel(),
                ActivationState = (OpcUa.Registry.Models.EndpointActivationState?)model.ActivationState,
                EndpointState = (OpcUa.Registry.Models.EndpointConnectivityState?)model.EndpointState,
                OutOfSync = model.OutOfSync
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoListApiModel ToApiModel(
            this EndpointInfoListModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoListModel ToServiceModel(
            this EndpointInfoListApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoListModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationApiModel ToApiModel(
            this EndpointRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationApiModel {
                Id = model.Id,
                Endpoint = model.Endpoint.ToApiModel(),
                EndpointUrl = model.EndpointUrl,
                AuthenticationMethods = model.AuthenticationMethods?
                    .Select(p => p.ToApiModel())
                    .ToList(),
                SecurityLevel = model.SecurityLevel,
                SiteId = model.SiteId,
                DiscovererId = model.DiscovererId,
                SupervisorId = model.SupervisorId
            };
        }

        /// <summary>
        /// Create api model
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
        /// Convert to Api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationQueryApiModel ToApiModel(
            this EndpointRegistrationQueryModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationQueryApiModel {
                Url = model.Url,
                Connected = model.Connected,
                Activated = model.Activated,
                EndpointState = (EndpointConnectivityState?)model.EndpointState,
                Certificate = model.Certificate,
                SecurityPolicy = model.SecurityPolicy,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                ApplicationId = model.ApplicationId,
                DiscovererId = model.DiscovererId,
                SiteOrGatewayId = model.SiteOrGatewayId,
                SupervisorId = model.SupervisorId,
                IncludeNotSeenSince = model.IncludeNotSeenSince
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationQueryModel ToServiceModel(
            this EndpointRegistrationQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationQueryModel {
                Url = model.Url,
                Connected = model.Connected,
                Activated = model.Activated,
                EndpointState = (OpcUa.Registry.Models.EndpointConnectivityState?)model.EndpointState,
                Certificate = model.Certificate,
                SecurityPolicy = model.SecurityPolicy,
                SecurityMode = (OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
                ApplicationId = model.ApplicationId,
                DiscovererId = model.DiscovererId,
                SiteOrGatewayId = model.SiteOrGatewayId,
                SupervisorId = model.SupervisorId,
                IncludeNotSeenSince = model.IncludeNotSeenSince
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayApiModel ToApiModel(
            this GatewayModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayInfoApiModel ToApiModel(
            this GatewayInfoModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayInfoApiModel {
                Gateway = model.Gateway.ToApiModel(),
                Modules = model.Modules.ToApiModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayModulesApiModel ToApiModel(
            this GatewayModulesModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayModulesApiModel {
                Publisher = model.Publisher.ToApiModel(),
                Supervisor = model.Supervisor.ToApiModel(),
                Discoverer = model.Discoverer.ToApiModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayListApiModel ToApiModel(
            this GatewayListModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayQueryModel ToServiceModel(
            this GatewayQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayQueryModel {
                SiteId = model.SiteId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayUpdateModel ToServiceModel(
            this GatewayUpdateApiModel model) {
            if (model == null) {
                return null;
            }
            return new GatewayUpdateModel {
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherApiModel ToApiModel(
            this PublisherModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToApiModel(),
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherModel ToServiceModel(
            this PublisherApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToServiceModel(),
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherConfigApiModel ToApiModel(
            this PublisherConfigModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherConfigApiModel {
                Capabilities = model.Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherConfigModel ToServiceModel(
            this PublisherConfigApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherConfigModel {
                Capabilities = model.Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherListApiModel ToApiModel(
            this PublisherListModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create services model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherListModel ToServiceModel(
            this PublisherListApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherListModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherQueryApiModel ToApiModel(
            this PublisherQueryModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherQueryApiModel {
                SiteId = model.SiteId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherQueryModel ToServiceModel(
            this PublisherQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherQueryModel {
                SiteId = model.SiteId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherUpdateApiModel ToApiModel(
            this PublisherUpdateModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherUpdateApiModel {
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherUpdateModel ToServiceModel(
            this PublisherUpdateApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherUpdateModel {
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RegistryOperationApiModel ToApiModel(
            this RegistryOperationContextModel model) {
            if (model == null) {
                return null;
            }
            return new RegistryOperationApiModel {
                Time = model.Time,
                AuthorityId = model.AuthorityId
            };
        }

        /// <summary>
        /// Create service model
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
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ServerRegistrationRequestModel ToServiceModel(
            this ServerRegistrationRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ServerRegistrationRequestModel {
                DiscoveryUrl = model.DiscoveryUrl,
                Id = model.Id,
                ActivationFilter = model.ActivationFilter.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorApiModel ToApiModel(
            this SupervisorModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorModel ToServiceModel(
            this SupervisorApiModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                OutOfSync = model.OutOfSync,
                Version = model.Version,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorListApiModel ToApiModel(
            this SupervisorListModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorListApiModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorListModel ToServiceModel(
            this SupervisorListApiModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorListModel {
                ContinuationToken = model.ContinuationToken,
                Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorQueryApiModel ToApiModel(
            this SupervisorQueryModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorQueryApiModel {
                SiteId = model.SiteId,
                EndpointId = model.EndpointId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorQueryModel ToServiceModel(
            this SupervisorQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorQueryModel {
                SiteId = model.SiteId,
                EndpointId = model.EndpointId,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorStatusApiModel ToApiModel(
            this SupervisorStatusModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorStatusApiModel {
                DeviceId = model.DeviceId,
                ModuleId = model.ModuleId,
                SiteId = model.SiteId,
                Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorStatusModel ToServiceModel(
            this SupervisorStatusApiModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorStatusModel {
                DeviceId = model.DeviceId,
                ModuleId = model.ModuleId,
                SiteId = model.SiteId,
                Endpoints = model.Endpoints?
                    .Select(e => e.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorUpdateApiModel ToApiModel(
            this SupervisorUpdateModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorUpdateApiModel {
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorUpdateModel ToServiceModel(
            this SupervisorUpdateApiModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorUpdateModel {
                SiteId = model.SiteId,
                LogLevel = (OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateApiModel ToApiModel(
            this X509CertificateModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateApiModel {
                Certificate = model.Certificate,
                NotAfterUtc = model.NotAfterUtc,
                NotBeforeUtc = model.NotBeforeUtc,
                SerialNumber = model.SerialNumber,
                Subject = model.Subject,
                SelfSigned = model.SelfSigned,
                Thumbprint = model.Thumbprint
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateModel ToServiceModel(
            this X509CertificateApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateModel {
                Certificate = model.Certificate,
                NotAfterUtc = model.NotAfterUtc,
                NotBeforeUtc = model.NotBeforeUtc,
                SerialNumber = model.SerialNumber,
                Subject = model.Subject,
                SelfSigned = model.SelfSigned,
                Thumbprint = model.Thumbprint
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainApiModel ToApiModel(
            this X509CertificateChainModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateChainApiModel {
                Status = model.Status?
                    .Select(s => (Core.Models.X509ChainStatus)s)
                    .ToList(),
                Chain = model.Chain?
                    .Select(c => c.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainModel ToServiceModel(
            this X509CertificateChainApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateChainModel {
                Status = model.Status?
                    .Select(s => (OpcUa.Core.Models.X509ChainStatus)s)
                    .ToList(),
                Chain = model.Chain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }
    }
}
