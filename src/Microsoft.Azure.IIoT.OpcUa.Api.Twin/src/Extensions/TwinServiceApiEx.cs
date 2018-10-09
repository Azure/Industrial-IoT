// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin service api extensions
    /// </summary>
    public static class TwinServiceApiEx {

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublishedNodeApiModel>> ListAllPublishedNodesAsync(
            this ITwinServiceApi service, string endpointId) {
            var nodes = new List<PublishedNodeApiModel>();
            var result = await service.ListPublishedNodesAsync(null, endpointId);
            nodes.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListPublishedNodesAsync(result.ContinuationToken,
                    endpointId);
                nodes.AddRange(result.Items);
            }
            return nodes;
        }
    }
}
