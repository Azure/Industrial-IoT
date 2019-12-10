// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.AspNetCore.Authentication.AzureAD.UI.Pages.Internal;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Publisher {


        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name=""></param>
        public Publisher(IPublisherServiceApi publisherService) {
            _publisherService = publisherService;
        }

        /// <summary>
        /// Get tree
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public async Task<PagedResult<PublishedNode>> PublishedAsync(string endpointId) {
            var pageResult = new PagedResult<PublishedNode>();
            try {
                string continuationToken = string.Empty;
                do {
                    var result = await _publisherService.NodePublishListAsync(endpointId, continuationToken);
                    continuationToken = result.ContinuationToken;

                    if (result.Items != null) {
                        foreach (var item in result.Items) {
                            pageResult.Results.Add(new PublishedNode {
                                NodeId = item.NodeId,
                                PublishingInterval = item.PublishingInterval,
                                SampligInterval = item.SamplingInterval,
                            });
                        }
                    }
                } while (!string.IsNullOrEmpty(continuationToken));
            }
            catch (Exception e) {
                // skip this node
                Trace.TraceError("Cannot get published nodes for endpointId'{0}'", endpointId);
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
            }
            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / pageResult.PageSize);
            return pageResult;
        }

        public async Task<bool> StartPublishing(string endpointId, string nodeId, 
            int samplingInterval, int publishingInterval) {
            var requestApiModel = new PublishStartRequestApiModel() {
                Item = new PublishedItemApiModel() {
                    NodeId = nodeId,
                    SamplingInterval = samplingInterval,
                    PublishingInterval = publishingInterval
                }
            };

            var resultApiModel = await _publisherService.NodePublishStartAsync(endpointId, requestApiModel);
            return (resultApiModel.ErrorInfo == null);
        }

        public async Task<bool> StopPublishing(string endpointId, string nodeId) {
            var requestApiModel = new PublishStopRequestApiModel() {
                    NodeId = nodeId,
            };
            var resultApiModel = await _publisherService.NodePublishStopAsync(endpointId, requestApiModel);
            return (resultApiModel.ErrorInfo == null);
        }

        private readonly IPublisherServiceApi _publisherService;
    }
}
