// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables remote configuration of the publisher
    /// </summary>
    public interface IConfigurationServices : IPublishServices<ConnectionModel>
    {
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
        /// Update nodes of endpoints in the published nodes configuration.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        Task AddOrUpdateEndpointsAsync(List<PublishedNodesEntryModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// returns the endpoints currently part of the configuration
        /// </summary>
        /// <param name="ct"></param>
        Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            CancellationToken ct = default);

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
