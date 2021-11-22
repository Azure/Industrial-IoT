// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    /// <summary>
    /// Enables remote configuration of the publisher
    /// </summary>
    public interface IPublisherConfigServices {

        /// <summary>
        /// Add nodes to be published to the configuration
        /// </summary>
        Task<List<string>> PublishNodesAsync(PublishedNodesEntryModel request);

        /// <summary>
        /// Remove node from the actual configuration
        /// </summary>
        Task<List<string>> UnpublishNodesAsync(PublishedNodesEntryModel request);

        /// <summary>
        /// resets the configuration
        /// </summary>
        /// <returns></returns>
        Task UnpublishAllNodesAsync();

        /// <summary>
        /// returns the endpoints currently part of the configuration
        /// </summary>
        /// <returns></returns>
        Task GetConfiguredEndpointsAsync();

        /// <summary>
        /// Get the configuration nodes for an endpoint
        /// </summary>
        /// <returns></returns>
        Task GetConfiguredNodesOnEndpointAsync();
    }
}
