// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationRegistrationModelEx {

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
                Endpoints = model.Endpoints?.Select(e => e.Clone()).ToList()
            };
        }

        /// <summary>
        /// Add or update a server list
        /// </summary>
        /// <param name="discovered"></param>
        /// <param name="server"></param>
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
        /// <param name="model"></param>
        /// <param name="server"></param>
        public static void UnionWith(this ApplicationRegistrationModel model,
            ApplicationRegistrationModel server) {

            if (model.Application == null) {
                model.Application = server.Application;
            }
            else {
                model.Application.Capabilities = model.Application.Capabilities.MergeWith(
                    server?.Application?.Capabilities);
                model.Application.DiscoveryUrls = model.Application.DiscoveryUrls.MergeWith(
                    server?.Application?.DiscoveryUrls);
                model.Application.HostAddresses = model.Application.HostAddresses.MergeWith(
                    server?.Application?.HostAddresses);
            }

            if (server?.Endpoints?.Any() ?? false) {
                if (model.Endpoints == null) {
                    model.Endpoints = server.Endpoints;
                }
                else {
                    foreach (var ep in server.Endpoints) {
                        var found = model.Endpoints.Where(ep.IsSameAs);
                        if (!found.Any()) {
                            model.Endpoints.Add(ep);
                        }
                        foreach (var existing in found) {
                            if (existing.Endpoint == null) {
                                existing.Endpoint = ep.Endpoint;
                                continue;
                            }
                            existing.Endpoint?.UnionWith(ep.Endpoint);
                        }
                    }
                }
            }
        }
    }
}
