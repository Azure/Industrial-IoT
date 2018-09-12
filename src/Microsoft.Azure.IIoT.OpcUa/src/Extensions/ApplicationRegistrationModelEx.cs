// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationRegistrationModelEx {

        /// <summary>
        /// Create server model
        /// </summary>
        /// <param name="result"></param>
        /// <param name="hostAddress"></param>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(this Protocol.DiscoveredEndpointsModel result,
            string hostAddress, string siteId, string supervisorId) {
            var type = result.Description.Server.ApplicationType.ToServiceType() ??
                ApplicationType.Server;
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SiteId = siteId,
                    SupervisorId = supervisorId,
                    ApplicationType = type,
                    ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                        siteId, result.Description.Server.ApplicationUri, type),
                    ProductUri = result.Description.Server.ProductUri,
                    ApplicationUri = result.Description.Server.ApplicationUri,
                    DiscoveryUrls = new HashSet<string>(result.Description.Server.DiscoveryUrls),
                    DiscoveryProfileUri = result.Description.Server.DiscoveryProfileUri,
                    HostAddresses = new HashSet<string> { hostAddress },
                    ApplicationName = result.Description.Server.ApplicationName.Text,
                    Certificate = result.Description.ServerCertificate,
                    Capabilities = new HashSet<string>(result.Capabilities)
                },
                Endpoints = new List<TwinRegistrationModel> {
                    new TwinRegistrationModel {
                        SiteId = siteId,
                        SupervisorId = supervisorId,
                        Certificate = result.Description.ServerCertificate,
                        SecurityLevel = result.Description.SecurityLevel,
                        Endpoint = new EndpointModel {
                            Url = result.Description.EndpointUrl,
                            SecurityMode = result.Description.SecurityMode.ToServiceType() ??
                                SecurityMode.None,
                            SecurityPolicy = result.Description.SecurityPolicyUri
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<ApplicationRegistrationModel> model,
            IEnumerable<ApplicationRegistrationModel> that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (model.Count() != that.Count()) {
                return false;
            }
            return model.All(a => that.Any(b => b.IsSameAs(a)));
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ApplicationRegistrationModel model,
            ApplicationRegistrationModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                that.Endpoints.IsSameAs(model.Endpoints) &&
                that.Application.IsSameAs(model.Application);
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel Clone(this ApplicationRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationRegistrationModel {
                Application = model.Application.Clone(),
                Endpoints = model.Endpoints?.Select(e => e.Clone()).ToList(),
                SecurityAssessment = model.SecurityAssessment
            };
        }

        /// <summary>
        /// Add or update a server list
        /// </summary>
        /// <param name="server"></param>
        /// <param name="discovered"></param>
        public static void AddOrUpdate(this List<ApplicationRegistrationModel> discovered,
            ApplicationRegistrationModel server) {
            var actual = discovered
                .FirstOrDefault(s => s.Application.IsSameAs(server.Application));
            if (actual != null) {
                // Merge server info
                actual.UnionWith(server);
            }
            else {
                discovered.Add(server);
            }
        }

        /// <summary>
        /// Create Union with server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="model"></param>
        public static void UnionWith(this ApplicationRegistrationModel model,
            ApplicationRegistrationModel server) {

            if (model.Application == null) {
                model.Application = server.Application;
            }
            else {
                model.Application.Capabilities = model.Application.Capabilities.UnionWithSafe(
                    server?.Application?.Capabilities);
                model.Application.DiscoveryUrls = model.Application.DiscoveryUrls.UnionWithSafe(
                    server?.Application?.DiscoveryUrls);
                model.Application.HostAddresses = model.Application.HostAddresses.UnionWithSafe(
                    server?.Application?.HostAddresses);
            }

            if (server?.Endpoints?.Any() ?? false) {
                if (model.Endpoints == null) {
                    model.Endpoints = server.Endpoints;
                }
                else {
                    foreach (var ep in server.Endpoints) {
                        if (!model.Endpoints.Any(ep.IsSameAs)) {
                            model.Endpoints.Add(ep);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update security assessment
        /// </summary>
        /// <param name="model"></param>
        public static ApplicationRegistrationModel SetSecurityAssessment(
            this ApplicationRegistrationModel model) {
            if (!model.Endpoints.Any()) {
                return model;
            }
            model.SecurityAssessment = (SecurityAssessment)
                model.Endpoints.Average(e => (int)e.GetSecurityAssessment());
            return model;
        }
    }
}
