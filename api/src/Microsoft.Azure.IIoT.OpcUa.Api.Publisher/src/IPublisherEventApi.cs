// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher Event controller api
    /// </summary>
    public interface IPublisherEventApi {

        /// <summary>
        /// Subscribe client to receive published samples
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task NodePublishSubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct = default);

        /// <summary>
        /// Unsubscribe client from receiving samples
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task NodePublishUnsubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct = default);
    }
}
