// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery api
    /// </summary>
    public interface IDiscoveryApi
    {
        /// <summary>
        /// Kick off onboarding of new server
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Kick off a one time discovery on all supervisors
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Cancel a discovery request with a particular id
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Find a server using the endpoint url in the query
        /// object. Returns a application registration object only
        /// if the endpoint is part of the application's endpoint
        /// list.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct = default);

        /// <summary>
        /// Get the certificate of an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct = default);

        /// <summary>
        /// Get all reverse connect endpoints
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<System.Collections.Generic.IReadOnlyList<ReverseConnectEndpointModel>> GetReverseConnectEndpointsAsync(
            CancellationToken ct = default);
    }
}
