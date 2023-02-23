// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery services
    /// </summary>
    public interface IDiscoveryServices
    {
        /// <summary>
        /// Register server from discovery url.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Discover using discovery request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Cancel discovery request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct = default);
    }
}
