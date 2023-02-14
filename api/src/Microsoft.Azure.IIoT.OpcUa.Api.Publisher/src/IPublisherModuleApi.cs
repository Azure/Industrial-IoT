// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module api
    /// </summary>
    public interface IPublisherModuleApi {

        /// <summary>
        /// Get the certificate of an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> GetEndpointCertificateAsync(EndpointApiModel endpoint,
            CancellationToken ct = default);

        /// <summary>
        /// Add or update publishing endpoints
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> AddOrUpdateEndpointsAsync(
            List<PublishNodesEndpointApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Get configured endpoints
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GetConfiguredEndpointsResponseApiModel> GetConfiguredEndpointsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get nodes of an endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GetConfiguredNodesOnEndpointResponseApiModel> GetConfiguredNodesOnEndpointAsync(
            PublishNodesEndpointApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Publish nodes
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> PublishNodesAsync(
            PublishNodesEndpointApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Remove all nodes on endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> UnpublishAllNodesAsync(
            PublishNodesEndpointApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Stop publishing specified nodes on endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> UnpublishNodesAsync(
            PublishNodesEndpointApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Get diagnostic info
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<PublishDiagnosticInfoApiModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default);
    }
}