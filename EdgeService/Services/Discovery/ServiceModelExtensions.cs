// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ServiceModelExtensions {

        /// <summary>
        /// Create unique server id
        /// </summary>
        /// <param name="applicationUri"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static string CreateServerId(string applicationUri,
            string supervisorId) =>
            $"{supervisorId ?? ""}{applicationUri?.ToLowerInvariant() ?? ""}"
                .ToSha1Hash();

        /// <summary>
        /// Create server model
        /// </summary>
        /// <param name="result"></param>
        public static ServerModel ToServiceModel(this OpcUaDiscoveryResult result,
            string supervisorId) {
            return new ServerModel {
                Server = new ServerInfoModel {
                    ServerId = CreateServerId(
                        result.Description.Server.ApplicationUri, supervisorId),
                    ApplicationUri = result.Description.Server.ApplicationUri,
                    ApplicationName = result.Description.Server.ApplicationName.Text,
                    ServerCertificate = result.Description.ServerCertificate,
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
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool Equals(this ServerInfoModel model, ServerInfoModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return that.ApplicationUri == model.ApplicationUri &&
                that.ServerCertificate.SequenceEqualsSafe(model.ServerCertificate);
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool Equals(this EndpointModel model, EndpointModel that) {
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
        public static void AddOrUpdate(this List<ServerModel> discovered,
            ServerModel server) {
            lock (discovered) {
                var actual = discovered
                    .FirstOrDefault(s => s.Server.Equals(server.Server));
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
        public static void UnionWith(this ServerModel model, ServerModel server) {

            if (server?.Server?.Capabilities?.Any() ?? false) {
                if (model.Server == null) {
                    model.Server = server.Server;
                }
                else if (model.Server.Capabilities == null) {
                    model.Server.Capabilities = server.Server.Capabilities;
                }
                else {
                    model.Server.Capabilities.AddRange(server.Server.Capabilities
                        .Where(c => !model.Server.Capabilities.Contains(c)));
                }
            }

            if (server?.Endpoints?.Any() ?? false) {
                if (model.Endpoints == null) {
                    model.Endpoints = server.Endpoints;
                }
                else {
                    foreach (var ep in server.Endpoints) {
                        if (!model.Endpoints.Any(e => ep.Equals(e))) {
                            model.Endpoints.Add(ep);
                        }
                    }
                }
            }
        }
    }
}
