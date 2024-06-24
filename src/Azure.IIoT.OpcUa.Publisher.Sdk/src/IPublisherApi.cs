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
        /// Create a published nodes entry for a specific writer group and
        /// dataset writer.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateOrUpdateDataSetWriterEntryAsync(
            PublishedNodesEntryModel entry, CancellationToken ct = default);

        /// <summary>
        /// Get the published nodes entry for a specific writer group and dataset
        /// writer.
        /// </summary>
        /// <param name="dataSetWriterGroup"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesEntryModel> GetDataSetWriterEntryAsync(
            string dataSetWriterGroup, string dataSetWriterId,
            CancellationToken ct = default);

        /// <summary>
        /// Add Nodes to a dedicated data set writer in a writer group.
        /// </summary>
        /// <param name="dataSetWriterGroup"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="opcNodes"></param>
        /// <param name="insertAfterFieldId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddOrUpdateNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            IReadOnlyList<OpcNodeModel> opcNodes, string? insertAfterFieldId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Remove Nodes with the data set field ids from a data set writer in
        /// a writer group.
        /// </summary>
        /// <param name="dataSetWriterGroup"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="dataSetFieldIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            IReadOnlyList<string> dataSetFieldIds, CancellationToken ct = default);

        /// <summary>
        /// Get Nodes from a data set writer in a writer group.
        /// </summary>
        /// <param name="dataSetWriterGroup"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="lastDataSetFieldId"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyList<OpcNodeModel>> GetNodesAsync(string dataSetWriterGroup,
            string dataSetWriterId, string? lastDataSetFieldId = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Remove the published nodes entry for a specific data set
        /// writer in a writer group.
        /// </summary>
        /// <param name="dataSetWriterGroup"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveDataSetWriterEntryAsync(string dataSetWriterGroup, string dataSetWriterId,
            CancellationToken ct = default);

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
            IReadOnlyList<PublishedNodesEntryModel> request,
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
