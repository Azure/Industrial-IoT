// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Discovered Endpoint Model extensions
    /// </summary>
    public static class DiscoveredEndpointModelEx {

        /// <summary>
        /// Create server model
        /// </summary>
        /// <param name="result"></param>
        /// <param name="hostAddress"></param>
        /// <param name="siteId"></param>
        /// <param name="gatewayId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(this DiscoveredEndpointModel result,
            string hostAddress, string siteId, string gatewayId, string moduleId,
            IJsonSerializer serializer) {
            var type = result.Description.Server.ApplicationType.ToServiceType() ??
                ApplicationType.Server;
            var discovererId = DiscovererModelEx.CreateDiscovererId(gatewayId, moduleId);
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SiteId = siteId,
                    DiscovererId = discovererId,
                    ApplicationType = type,
                    ApplicationId = ApplicationInfoModelEx.CreateApplicationId(siteId ?? gatewayId,
                        result.Description.Server.ApplicationUri, type), // TODO: Assign at onboarder and leave null
                    ProductUri = result.Description.Server.ProductUri,
                    ApplicationUri = result.Description.Server.ApplicationUri,
                    DiscoveryUrls = new HashSet<string>(result.Description.Server.DiscoveryUrls),
                    DiscoveryProfileUri = result.Description.Server.DiscoveryProfileUri,
                    HostAddresses = new HashSet<string> { hostAddress },
                    ApplicationName = result.Description.Server.ApplicationName.Text,
                    LocalizedNames = string.IsNullOrEmpty(result.Description.Server.ApplicationName.Locale) ?
                        null : new Dictionary<string, string> {
                            [result.Description.Server.ApplicationName.Locale] =
                                result.Description.Server.ApplicationName.Text
                        },
                    NotSeenSince = null,
                    Capabilities = new HashSet<string>(result.Capabilities)
                },
                Endpoints = new List<EndpointRegistrationModel> {
                    new EndpointRegistrationModel {
                        SiteId = siteId,
                        DiscovererId = discovererId,
                        SupervisorId = null,
                        Id = null,
                        SecurityLevel = result.Description.SecurityLevel,
                        AuthenticationMethods = result.Description.UserIdentityTokens
                            .ToServiceModel(serializer),
                        EndpointUrl = result.Description.EndpointUrl, // Reported
                        Endpoint = new EndpointModel {
                            Url = result.AccessibleEndpointUrl, // Accessible
                            AlternativeUrls = new HashSet<string> {
                                result.AccessibleEndpointUrl,
                                result.Description.EndpointUrl,
                            },
                            Certificate = result.Description.ServerCertificate?.ToThumbprint(),
                            SecurityMode = result.Description.SecurityMode.ToServiceType() ??
                                SecurityMode.None,
                            SecurityPolicy = result.Description.SecurityPolicyUri
                        }
                    }
                }
            };
        }
    }
}
