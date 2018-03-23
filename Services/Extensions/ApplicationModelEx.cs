// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using System.Collections.Generic;
    using System.Linq;
    using System;

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
        public static ApplicationModel ToServiceModel(this OpcUaDiscoveryResult result,
            string supervisorId) {
            return new ApplicationModel {
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
                    Capabilities = new List<string>(result.Capabilities)
                },
                Endpoints = new List<EndpointModel> {
                    new EndpointModel {
                        Url = result.Description.EndpointUrl,
                        SecurityMode = result.Description.SecurityMode.ToServiceType() ??
                            SecurityMode.None,
                        SecurityPolicy = result.Description.SecurityPolicyUri,
                        SupervisorId = supervisorId
                    }
                }
            };
        }

        /// <summary>
        /// Update supervisor id on model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="supervisorId"></param>
        public static ApplicationModel SetSupervisorId(this ApplicationModel model,
            string supervisorId) {
            if (model == null) {
                return null;
            }
            if (model.Application != null) {
                model.Application.SupervisorId = supervisorId;
                model.Application.ApplicationId = CreateApplicationId(
                    supervisorId, model.Application.ApplicationUri);
            }
            if (model.Endpoints != null) {
                model.Endpoints.ForEach(e => e.SupervisorId = supervisorId);
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
            return that.ApplicationUri == model.ApplicationUri &&
                that.Certificate.SequenceEqualsSafe(model.Certificate);
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
        public static void AddOrUpdate(this List<ApplicationModel> discovered,
            ApplicationModel server) {
            lock (discovered) {
                var actual = discovered
                    .FirstOrDefault(s => s.Application.IsEqual(server.Application));
                if (actual != null) {
                    // Merge server info
                    actual.UnionWith(server);
                    return;
                }
                else {
                    discovered.Add(server);
                }
            }
        }

        /// <summary>
        /// Create Union with server
        /// </summary>
        /// <param name="server"></param>
        public static void UnionWith(this ApplicationModel model,
            ApplicationModel server) {

            if (server?.Application?.Capabilities?.Any() ?? false) {
                if (model.Application == null) {
                    model.Application = server.Application;
                }
                else if (model.Application.Capabilities == null) {
                    model.Application.Capabilities = server.Application.Capabilities;
                }
                else {
                    model.Application.Capabilities.AddRange(server.Application.Capabilities
                        .Where(c => !model.Application.Capabilities.Contains(c)));
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
    }
}
