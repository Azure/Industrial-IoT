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
    /// Publisher registry extensions
    /// </summary>
    public static class PublisherRegistryEx {

        /// <summary>
        /// Find publisher.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="publisherId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<PublisherModel> FindPublisherAsync(
            this IPublisherRegistry service, string publisherId,
            CancellationToken ct = default) {
            try {
                return await service.GetPublisherAsync(publisherId, false, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// List all publishers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<PublisherModel>> ListAllPublishersAsync(
            this IPublisherRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var publishers = new List<PublisherModel>();
            var result = await service.ListPublishersAsync(null, onlyServerState, null, ct);
            publishers.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                publishers.AddRange(result.Items);
            }
            return publishers;
        }

        /// <summary>
        /// Returns all publisher ids from the registry
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<string>> ListAllPublisherIdsAsync(
            this IPublisherRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var publishers = new List<string>();
            var result = await service.ListPublishersAsync(null, onlyServerState, null, ct);
            publishers.AddRange(result.Items.Select(s => s.Id));
            while (result.ContinuationToken != null) {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                publishers.AddRange(result.Items.Select(s => s.Id));
            }
            return publishers;
        }
    }
}
