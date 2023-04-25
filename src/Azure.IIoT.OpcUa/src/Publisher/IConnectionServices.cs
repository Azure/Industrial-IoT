// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connection services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConnectionServices<T>
    {
        /// <summary>
        /// Create a connection and keep it alive for the specified
        /// duration or until disconnected.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ConnectResponseModel> ConnectAsync(T endpoint,
            ConnectRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Test connection
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TestConnectionResponseModel> TestConnectionAsync(T endpoint,
            TestConnectionRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Disconnect using the handle provided by connect call.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisconnectAsync(T endpoint, DisconnectRequestModel request,
            CancellationToken ct = default);
    }
}
