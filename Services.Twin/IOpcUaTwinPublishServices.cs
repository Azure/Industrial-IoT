// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish services
    /// </summary>
    public interface IOpcUaTwinPublishServices {

        /// <summary>
        /// Publish or unpublish from node
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResultModel> NodePublishAsync(string twinId,
            PublishRequestModel request);

        /// <summary>
        /// Get all published node ids for endpoint - provides for inter
        /// service communication between opc node services and publisher
        /// to enable filtering and tagging of published nodes.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<PublishedNodeListModel> ListPublishedNodesAsync(
            string continuation, string twinId);
    }
}