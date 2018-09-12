// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Stack models extensions
    /// </summary>
    public static class StackModelsEx {

        /// <summary>
        /// Convert endpoint description to application registration
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="hostAddress"></param>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(this EndpointDescription ep,
            string hostAddress, string siteId, string supervisorId) {
            var caps = new HashSet<string>();
            if (ep.Server.ApplicationType == Opc.Ua.ApplicationType.DiscoveryServer) {
                caps.Add("LDS");
            }
            var type = ep.Server.ApplicationType.ToServiceType() ?? Models.ApplicationType.Server;
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SiteId = siteId,
                    SupervisorId = supervisorId,
                    ApplicationType = type,
                    HostAddresses = new HashSet<string> { hostAddress },
                    ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                        siteId, ep.Server.ApplicationUri, type),
                    ApplicationUri = ep.Server.ApplicationUri,
                    DiscoveryUrls = new HashSet<string>(ep.Server.DiscoveryUrls),
                    DiscoveryProfileUri = ep.Server.DiscoveryProfileUri,
                    ProductUri = ep.Server.ProductUri,
                    Certificate = ep.ServerCertificate,
                    ApplicationName = ep.Server.ApplicationName.Text,
                    Capabilities = caps
                },
                Endpoints = new List<TwinRegistrationModel> {
                    new TwinRegistrationModel {
                        SiteId = siteId,
                        SupervisorId = supervisorId,
                        Certificate = ep.ServerCertificate,
                        SecurityLevel = ep.SecurityLevel,
                        Endpoint = new EndpointModel {
                            Url = ep.EndpointUrl,
                            SecurityMode = ep.SecurityMode.ToServiceType(),
                            SecurityPolicy = ep.SecurityPolicyUri
                        }
                    }
                }
            };
        }
    }
}
