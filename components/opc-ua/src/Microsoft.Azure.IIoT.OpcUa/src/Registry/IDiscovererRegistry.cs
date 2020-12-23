// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry
    /// </summary>
    public interface IDiscovererRegistry {

        /// <summary>
        /// Get all discoverers in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererListModel> ListDiscoverersAsync(
            string continuation, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find discoverers using specific criterias.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get discoverer registration by identifer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererModel> GetDiscovererAsync(string id,
            CancellationToken ct = default);

        /// <summary>
        /// Update discoverer, e.g. set discovery mode
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateDiscovererAsync(string id,
            DiscovererUpdateModel request,
            CancellationToken ct = default);
    }
}
