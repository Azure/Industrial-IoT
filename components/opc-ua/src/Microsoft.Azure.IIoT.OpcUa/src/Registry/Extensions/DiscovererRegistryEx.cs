// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry extensions
    /// </summary>
    public static class DiscovererRegistryEx {

        /// <summary>
        /// Find discoverer.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="discovererId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<DiscovererModel> FindDiscovererAsync(
            this IDiscovererRegistry service, string discovererId,
            CancellationToken ct = default) {
            try {
                return await service.GetDiscovererAsync(discovererId, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// List all discoverers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<DiscovererModel>> ListAllDiscoverersAsync(
            this IDiscovererRegistry service, CancellationToken ct = default) {
            var discoverers = new List<DiscovererModel>();
            var result = await service.ListDiscoverersAsync(null, null, ct);
            discoverers.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct);
                discoverers.AddRange(result.Items);
            }
            return discoverers;
        }

        /// <summary>
        /// Returns all discoverer ids from the registry
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<string>> ListAllDiscovererIdsAsync(
            this IDiscovererRegistry service, CancellationToken ct = default) {
            var discoverers = new List<string>();
            var result = await service.ListDiscoverersAsync(null, null, ct);
            discoverers.AddRange(result.Items.Select(s => s.Id));
            while (result.ContinuationToken != null) {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct);
                discoverers.AddRange(result.Items.Select(s => s.Id));
            }
            return discoverers;
        }
    }
}
