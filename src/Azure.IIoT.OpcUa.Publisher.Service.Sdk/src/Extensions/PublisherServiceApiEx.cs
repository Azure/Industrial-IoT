// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher service api extensions
    /// </summary>
    public static class PublisherServiceApiEx
    {
        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublishedItemModel>> NodePublishListAllAsync(
            this IPublisherServiceApi service, string endpointId)
        {
            var nodes = new List<PublishedItemModel>();
            var result = await service.NodePublishListAsync(endpointId).ConfigureAwait(false);
            if (result.Items != null)
            {
                nodes.AddRange(result.Items);
            }
            while (result.ContinuationToken != null)
            {
                result = await service.NodePublishListAsync(endpointId,
                    result.ContinuationToken).ConfigureAwait(false);
                if (result.Items != null)
                {
                    nodes.AddRange(result.Items);
                }
            }
            return nodes;
        }

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<PublishedItemListResponseModel> NodePublishListAsync(
            this IPublisherServiceApi service, string endpointId, string? continuation = null)
        {
            return service.NodePublishListAsync(endpointId, new PublishedItemListRequestModel
            {
                ContinuationToken = continuation
            });
        }
    }
}
