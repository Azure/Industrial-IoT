// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables remote configuration of the publisher
    /// </summary>
    public interface IPublishedNodesServices : IPublishServices<ConnectionModel>
    {
        /// <summary>
        /// Create a published nodes entry for a specific writer group
        /// and dataset writer. The entry must specify a unique writer
        /// group and dataset writer id. If the entry is found it is
        /// updated, if it is not found, it is created. If more than
        /// one entry is found an error is returned. The entry can
        /// include nodes which will be the initial set. The entries
        /// must all specify a unique dataSetFieldId.
        /// </summary>
        /// <param name="entry">The entry to create for the writer</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateOrUpdateDataSetWriterEntryAsync(
            PublishedNodesEntryModel entry, CancellationToken ct = default);

        /// <summary>
        /// Get the published nodes entry for a specific writer group
        /// and dataset writer. Dedicated errors are returned if no,
        /// or no unique entry could be found. THe entry does not
        /// contain the nodes
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedNodesEntryModel> GetDataSetWriterEntryAsync(
            string writerGroupId, string dataSetWriterId,
            CancellationToken ct = default);

        /// <summary>
        /// Add Nodes to a dedicated data set writer in a writer group.
        /// Each node must have a unique DataSetFieldId. If the field
        /// already exists, the node is updated. If a node does not
        /// have a dataset field id an error is returned.
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="nodes">Nodes to add or update</param>
        /// <param name="insertAfterFieldId">Field after which to
        /// insert the nodes. If not specified, nodes are added at the
        /// end of the entry</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddOrUpdateNodesAsync(string writerGroupId, string dataSetWriterId,
            IReadOnlyList<OpcNodeModel> nodes, string? insertAfterFieldId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Remove Nodes with the data set field ids from a data set
        /// writer in a writer group. If the field is not found, no
        /// error is returned.
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="dataSetFieldIds">Fields to add</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveNodesAsync(string writerGroupId, string dataSetWriterId,
            IReadOnlyList<string> dataSetFieldIds, CancellationToken ct = default);

        /// <summary>
        /// Get Nodes from a data set writer in a writer group.
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="dataSetFieldId">the field id after which to start.
        /// If not specified, nodes from the beginning are returned.</param>
        /// <param name="count">Number of nodes to return</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyList<OpcNodeModel>> GetNodesAsync(string writerGroupId,
            string dataSetWriterId, string? dataSetFieldId = null,
            int? count = null, CancellationToken ct = default);

        /// <summary>
        /// Get a node from a data set writer in a writer group.
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="dataSetFieldId">the field id after which to start.
        /// If not specified, nodes from the beginning are returned.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpcNodeModel> GetNodeAsync(string writerGroupId,
            string dataSetWriterId, string dataSetFieldId,
            CancellationToken ct = default);

        /// <summary>
        /// Remove the published nodes entry for a specific data set
        /// writer in a writer group. Dedicated errors are returned if no,
        /// or no unique entry could be found.
        /// </summary>
        /// <param name="writerGroupId">The writer group</param>
        /// <param name="dataSetWriterId">The data set writer</param>
        /// <param name="force">Force delete all writers even if more than
        /// one were found. Does not error when none were found.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveDataSetWriterEntryAsync(string writerGroupId,
            string dataSetWriterId, bool force = false,
            CancellationToken ct = default);

        /// <summary>
        /// Add nodes to be published to the configuration
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task PublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Remove node from the actual configuration
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task UnpublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Resets the configuration for an endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task UnpublishAllNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Replace all configured endpoints with the new set.
        /// Using an empty list will remove all entries.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task SetConfiguredEndpointsAsync(IReadOnlyList<PublishedNodesEntryModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Update nodes of endpoints in the published nodes configuration.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task AddOrUpdateEndpointsAsync(IReadOnlyList<PublishedNodesEntryModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// returns the endpoints currently part of the configuration
        /// </summary>
        /// <param name="includeNodes"></param>
        /// <param name="ct"></param>
        Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            bool includeNodes = false, CancellationToken ct = default);

        /// <summary>
        /// Get the configuration nodes for an endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Gets the diagnostic information for a specific endpoint
        /// </summary>
        /// <param name="ct"></param>
        Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default);
    }
}
