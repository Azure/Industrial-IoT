// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module api
    /// </summary>
    public interface IPublisherApi
    {
        /// <summary>
        /// Get configured endpoints
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            GetConfiguredEndpointsRequestModel? request = null, CancellationToken ct = default);

        /// <summary>
        /// Get nodes of an endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Set configured endpoints
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SetConfiguredEndpointsAsync(SetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Add or update publishing endpoints
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Publish nodes
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Remove all nodes on endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Stop publishing specified nodes on endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStartResponseModel> PublishStartAsync(ConnectionModel connection,
            PublishStartRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStopResponseModel> PublishStopAsync(ConnectionModel connection,
            PublishStopRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Configure nodes to publish and unpublish in bulk
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishBulkResponseModel> PublishBulkAsync(ConnectionModel connection,
            PublishBulkRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedItemListResponseModel> PublishListAsync(ConnectionModel connection,
            PublishedItemListRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get diagnostic info
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Shutdown publisher
        /// </summary>
        /// <param name="failFast"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ShutdownAsync(bool failFast = false,
            CancellationToken ct = default);

        /// <summary>
        /// Get server certificate as PEM string
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string?> GetServerCertificateAsync(CancellationToken ct = default);

        /// <summary>
        /// Get api key as string
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string?> GetApiKeyAsync(CancellationToken ct = default);
    }
}
