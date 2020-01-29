// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
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
                ApplicationType = (IIoT.OpcUa.Api.Registry.Models.ApplicationType)model.ApplicationType,
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
                SecurityAssessment = (IIoT.OpcUa.Api.Registry.Models.SecurityAssessment?)model.SecurityAssessment,
                Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
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
                ApplicationType = (IIoT.OpcUa.Core.Models.ApplicationType?)model.ApplicationType,
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
        public static ApplicationRegistrationRequestModel ToServiceModel(
            this ApplicationRegistrationRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationRequestModel {
                ApplicationType = (IIoT.OpcUa.Core.Models.ApplicationType?)model.ApplicationType,
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
                CredentialType = (IIoT.OpcUa.Api.Registry.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Api.Registry.Models.CredentialType.None
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
                LogLevel = (IIoT.OpcUa.Api.Registry.Models.TraceLogLevel?)model.LogLevel,
                Discovery = (IIoT.OpcUa.Api.Registry.Models.DiscoveryMode?)model.Discovery,
                DiscoveryConfig = model.DiscoveryConfig.ToApiModel(),
                OutOfSync = model.OutOfSync,
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
                Discovery = (IIoT.OpcUa.Registry.Models.DiscoveryMode?)model.Discovery
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
                LogLevel = (IIoT.OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
                Discovery = (IIoT.OpcUa.Registry.Models.DiscoveryMode?)model.Discovery,
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
                NetworkProbeTimeoutMs = (int?)model.NetworkProbeTimeout?.TotalMilliseconds,
                MaxNetworkProbes = model.MaxNetworkProbes,
                PortRangesToScan = model.PortRangesToScan,
                PortProbeTimeoutMs = (int?)model.PortProbeTimeout?.TotalMilliseconds,
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                IdleTimeBetweenScansSec = (int?)model.IdleTimeBetweenScans?.TotalSeconds,
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
        public static DiscoveryRequestModel ToServiceModel(
            this DiscoveryRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryRequestModel {
                Id = model.Id,
                Configuration = model.Configuration.ToServiceModel(),
                Discovery = (IIoT.OpcUa.Registry.Models.DiscoveryMode?)model.Discovery
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
                SecurityMode = (IIoT.OpcUa.Api.Registry.Models.SecurityMode?)model.SecurityMode
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
                ActivationState = (IIoT.OpcUa.Api.Registry.Models.EndpointActivationState?)model.ActivationState
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
                SecurityMode = (IIoT.OpcUa.Api.Registry.Models.SecurityMode?)model.SecurityMode,
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
                ActivationState = (IIoT.OpcUa.Api.Registry.Models.EndpointActivationState?)model.ActivationState,
                EndpointState = (IIoT.OpcUa.Api.Registry.Models.EndpointConnectivityState?)model.EndpointState,
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
                EndpointState = (IIoT.OpcUa.Registry.Models.EndpointConnectivityState?)model.EndpointState,
                Certificate = model.Certificate,
                SecurityPolicy = model.SecurityPolicy,
                SecurityMode = (IIoT.OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
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
                LogLevel = (IIoT.OpcUa.Api.Registry.Models.TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToApiModel(),
                OutOfSync = model.OutOfSync,
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
                LogLevel = (IIoT.OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel,
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
                LogLevel = (IIoT.OpcUa.Api.Registry.Models.TraceLogLevel?)model.LogLevel,
                OutOfSync = model.OutOfSync,
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
                LogLevel = (IIoT.OpcUa.Registry.Models.TraceLogLevel?)model.LogLevel
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
        /// Create collection
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainApiModel ToApiModel(
            this X509CertificateChainModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateChainApiModel {
                Status = model.Status?
                    .Select(s => (IIoT.OpcUa.Api.Registry.Models.X509ChainStatus)s)
                    .ToList(),
                Chain = model.Chain?
                    .Select(c => c.ToApiModel())
                    .ToList()
            };
        }

    }
}
