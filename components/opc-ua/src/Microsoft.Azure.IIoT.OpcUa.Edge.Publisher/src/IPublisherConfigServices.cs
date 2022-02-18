// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables remote configuration of the publisher
    /// </summary>
    public interface IPublisherConfigServices {

        /// <summary>
        /// Add nodes to be published to the configuration
        /// </summary>
        Task PublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Remove node from the actual configuration
        /// </summary>
        Task UnpublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Resets the configuration for an endpoint
        /// </summary>
        Task UnpublishAllNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Update nodes of endpoints in the published nodes configuration.
        /// </summary>
        Task AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request,
            CancellationToken ct = default
        );

        /// <summary>
        /// returns the endpoints currently part of the configuration
        /// </summary>
        Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(CancellationToken ct = default);

        /// <summary>
        /// Get the configuration nodes for an endpoint
        /// </summary>
        Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(PublishedNodesEntryModel request, CancellationToken ct = default);

        /// <summary>
        /// Gets the diagnostic information for a specific endpoint
        /// </summary>
        Task<List<JobDiagnosticInfoModel>> GetDiagnosticInfoAsync(CancellationToken ct = default);
    }
}
