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
    /// Network discovery services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INetworkDiscovery<T> where T : class
    {
        /// <summary>
        /// Discovery server in network with discovery url.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestModel request,
            T? context = null, CancellationToken ct = default);

        /// <summary>
        /// Start a discovery run for servers in network.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request,
            T? context = null, CancellationToken ct = default);

        /// <summary>
        /// Cancel a discovery run that is ongoing
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelAsync(DiscoveryCancelRequestModel request,
            T? context = null, CancellationToken ct = default);
    }
}
