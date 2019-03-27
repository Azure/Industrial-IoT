// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;

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
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(this DiscoveredEndpointModel result,
            string hostAddress, string siteId, string supervisorId) {
            var type = result.Description.Server.ApplicationType.ToServiceType() ??
                ApplicationType.Server;
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SiteId = siteId,
                    SupervisorId = supervisorId,
                    ApplicationType = type,
                    ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                        siteId ?? supervisorId, result.Description.Server.ApplicationUri, type),
                    ProductUri = result.Description.Server.ProductUri,
                    ApplicationUri = result.Description.Server.ApplicationUri,
                    DiscoveryUrls = new HashSet<string>(result.Description.Server.DiscoveryUrls),
                    DiscoveryProfileUri = result.Description.Server.DiscoveryProfileUri,
                    HostAddresses = new HashSet<string> { hostAddress },
                    ApplicationName = result.Description.Server.ApplicationName.Text,
                    Locale = result.Description.Server.ApplicationName.Locale,
                    NotSeenSince = null,
                    Certificate = result.Description.ServerCertificate,
                    Capabilities = new HashSet<string>(result.Capabilities)
                },
                Endpoints = new List<EndpointRegistrationModel> {
                    new EndpointRegistrationModel {
                        SiteId = siteId,
                        SupervisorId = supervisorId,
                        Certificate = result.Description.ServerCertificate,
                        SecurityLevel = result.Description.SecurityLevel,
                        AuthenticationMethods = result.Description.UserIdentityTokens.ToServiceModel(),
                        EndpointUrl = result.Description.EndpointUrl, // Reported
                        Endpoint = new EndpointModel {
                            Url = result.AccessibleEndpointUrl, // Accessible
                            AlternativeUrls = new HashSet<string> {
                                result.AccessibleEndpointUrl,
                                result.Description.EndpointUrl,
                            },
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
