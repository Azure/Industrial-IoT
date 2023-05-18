// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the publisher services through the publisher client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPublisherServices<T>
    {
        /// <summary>
        /// Set configured endpoints on the publisher
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SetConfiguredEndpointsAsync(T publisherId,
            SetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get configured endpoints on the publisher
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<PublishedNodesEntryModel> GetConfiguredEndpointsAsync(
            T publisherId, GetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default);
    }
}
