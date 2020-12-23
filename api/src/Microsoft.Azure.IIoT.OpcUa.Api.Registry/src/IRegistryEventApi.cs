// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry Event controller api
    /// </summary>
    public interface IRegistryEventApi {

        /// <summary>
        /// Subscribe client to discovery progress from discoverer
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="connectionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct = default);

        /// <summary>
        /// Unsubscribe client from discovery progress for specified request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="connectionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnsubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct = default);

        /// <summary>
        /// Unsubscribe client from discovery events
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="connectionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnsubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct = default);

        /// <summary>
        /// Subscribe client to progress on specifiy request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="connectionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct = default);
    }
}
