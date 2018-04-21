// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Client;
    using Microsoft.Azure.IIoT.OpcTwin.Services.External;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationModelEx {

        /// <summary>
        /// Create unique server id
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="applicationUri"></param>
        /// <returns></returns>
        public static string CreateApplicationId(string supervisorId,
            string applicationUri) =>
            $"{supervisorId ?? ""}{applicationUri?.ToLowerInvariant() ?? ""}"
                .ToSha1Hash();

        /// <summary>
        /// Create server model
        /// </summary>
        /// <param name="result"></param>
        public static ApplicationRegistrationModel ToServiceModel(
            this OpcUaDiscoveryResult result, string supervisorId) {
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SupervisorId = supervisorId,
                    ApplicationId = CreateApplicationId(supervisorId,
                        result.Description.Server.ApplicationUri),
                    ApplicationType = result.Description.Server.ApplicationType
                        .ToServiceType() ?? ApplicationType.Server,
                    ProductUri = result.Description.Server.ProductUri,
                    ApplicationUri = result.Description.Server.ApplicationUri,
                    DiscoveryUrls = result.Description.Server.DiscoveryUrls,
                    DiscoveryProfileUri = result.Description.Server.DiscoveryProfileUri,
                    ApplicationName = result.Description.Server.ApplicationName.Text,
                    Certificate = result.Description.ServerCertificate,
                    Capabilities = new HashSet<string>(result.Capabilities)
                },
                Endpoints = new List<TwinRegistrationModel> {
                    new TwinRegistrationModel {
                        Certificate = result.Description.ServerCertificate,
                        SecurityLevel = result.Description.SecurityLevel,
                        Endpoint = new EndpointModel {
                            Url = result.Description.EndpointUrl,
                            SecurityMode = result.Description.SecurityMode.ToServiceType() ??
                                SecurityMode.None,
                            SecurityPolicy = result.Description.SecurityPolicyUri,
                            SupervisorId = supervisorId
                        },
                    }
                }
            };
        }

        /// <summary>
        /// Update supervisor id on model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="supervisorId"></param>
        public static ApplicationRegistrationModel SetSupervisorId(
            this ApplicationRegistrationModel model, string supervisorId) {
            if (model == null) {
                return null;
            }
            if (model.Application != null) {
                model.Application.SupervisorId = supervisorId;
                model.Application.ApplicationId = CreateApplicationId(
                    supervisorId, model.Application.ApplicationUri);
            }
            if (model.Endpoints != null) {
                model.Endpoints.ForEach(e => e.Endpoint.SupervisorId = supervisorId);
            }
            return model;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this ApplicationInfoModel model,
            ApplicationInfoModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return that.ApplicationUri == model.ApplicationUri;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this TwinRegistrationModel model,
            TwinRegistrationModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return model.Endpoint.IsEqual(that.Endpoint) &&
                model.SecurityLevel == that.SecurityLevel &&
                model.Certificate?.ToSha1Hash() == that.Certificate?.ToSha1Hash();
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this EndpointModel model, EndpointModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return that.Url == model.Url &&
                that.SecurityPolicy == model.SecurityPolicy &&
                that.SecurityMode == model.SecurityMode;
        }

        /// <summary>
        /// Add or update a server list
        /// </summary>
        /// <param name="server"></param>
        public static void AddOrUpdate(this List<ApplicationRegistrationModel> discovered,
            ApplicationRegistrationModel server) {
            var actual = discovered
                .FirstOrDefault(s => s.Application.IsEqual(server.Application));
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
        public static void UnionWith(this ApplicationRegistrationModel model,
            ApplicationRegistrationModel server) {

            if (server?.Application?.Capabilities?.Any() ?? false) {
                if (model.Application == null) {
                    model.Application = server.Application;
                }
                else if (model.Application.Capabilities == null) {
                    model.Application.Capabilities = server.Application.Capabilities;
                }
                else {
                    model.Application.Capabilities.AddRange(server.Application.Capabilities);
                }
            }

            if (server?.Endpoints?.Any() ?? false) {
                if (model.Endpoints == null) {
                    model.Endpoints = server.Endpoints;
                }
                else {
                    foreach (var ep in server.Endpoints) {
                        if (!model.Endpoints.Any(e => ep.IsEqual(e))) {
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
            model.SecurityAssessment = (SecurityAssessment)
                model.Endpoints.Average(e => (int)e.GetSecurityAssessment());
            return model;
        }

        /// <summary>
        /// Get security assessment
        /// </summary>
        /// <param name="model"></param>
        public static SecurityAssessment GetSecurityAssessment(
            this TwinRegistrationModel model) {
            if (model.Endpoint.SecurityMode == SecurityMode.None) {
                return SecurityAssessment.Low;
            }

            // TODO

            var cert = new X509Certificate2(model.Certificate);
            var securityProfile = model.Endpoint.SecurityPolicy.Remove(0,
                model.Endpoint.SecurityPolicy.IndexOf('#') + 1);

            var expiryDate = cert.NotAfter;
            var issuer = cert.Issuer.Extract("CN=", ",");

            if ((securityProfile == "None") ||
                (securityProfile == "sha1") ||
                (cert.PublicKey.Key.KeySize == 1024)) {
                return SecurityAssessment.Low;
            }
            if ((cert.IssuerName.Name == cert.SubjectName.Name) &&
                (securityProfile != "None")) {
                return SecurityAssessment.High;
            }
            return SecurityAssessment.Medium;
        }
    }
}
