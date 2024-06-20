// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationRegistrationModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IReadOnlyList<ApplicationRegistrationModel>? model,
            IReadOnlyList<ApplicationRegistrationModel>? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.Count != that.Count)
            {
                return false;
            }
            foreach (var a in model)
            {
                if (!that.Any(b => b.IsSameAs(a)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ApplicationRegistrationModel? model,
            ApplicationRegistrationModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (!that.Endpoints.IsSameAs(model.Endpoints))
            {
                return false;
            }
            if (!that.Application.IsSameAs(model.Application))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ApplicationRegistrationModel? Clone(this ApplicationRegistrationModel? model,
            TimeProvider timeProvider)
        {
            return model == null ? null : (model with
            {
                Application = model.Application.Clone(timeProvider),
                Endpoints = model.Endpoints?.Select(e => e.Clone()).ToList()
            });
        }

        /// <summary>
        /// Add or update a server list
        /// </summary>
        /// <param name="discovered"></param>
        /// <param name="server"></param>
        public static void AddOrUpdate(this List<ApplicationRegistrationModel> discovered,
            ApplicationRegistrationModel server)
        {
            var actual = discovered
                .Find(s => s.Application.IsSameAs(server.Application));
            if (actual != null)
            {
                // Merge server info
                actual.UnionWith(server);
            }
            else
            {
                discovered.Add(server);
            }
        }

        /// <summary>
        /// Create Union with server
        /// </summary>
        /// <param name="model"></param>
        /// <param name="server"></param>
        public static void UnionWith(this ApplicationRegistrationModel model,
            ApplicationRegistrationModel server)
        {
            if (model.Application == null)
            {
                model.Application = server.Application;
            }
            else
            {
                model.Application.Capabilities = model.Application.Capabilities.MergeWith(
                    server?.Application?.Capabilities);
                model.Application.DiscoveryUrls = model.Application.DiscoveryUrls.MergeWith(
                    server?.Application?.DiscoveryUrls);
                model.Application.HostAddresses = model.Application.HostAddresses.MergeWith(
                    server?.Application?.HostAddresses);
            }

            if (server?.Endpoints?.Any() ?? false)
            {
                if (model.Endpoints == null)
                {
                    model.Endpoints = server.Endpoints;
                }
                else
                {
                    foreach (var ep in server.Endpoints)
                    {
                        var found = model.Endpoints.Where(ep.IsSameAs).ToList();
                        if (found.Count == 0)
                        {
                            model.Endpoints = model.Endpoints.Append(ep).ToList();
                        }
                        foreach (var existing in found)
                        {
                            if (existing.Endpoint == null)
                            {
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
