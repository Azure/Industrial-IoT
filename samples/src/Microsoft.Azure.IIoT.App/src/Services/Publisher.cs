// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System;
    using System.Diagnostics;
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
        /// PublishedAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns>PublishedNode</returns>
        public async Task<PagedResult<PublishedItemApiModel>> PublishedAsync(string endpointId) {
            var pageResult = new PagedResult<PublishedItemApiModel>();
            try {
                string continuationToken = string.Empty;
                do {
                    var result = await _publisherService.NodePublishListAsync(endpointId, continuationToken);
                    continuationToken = result.ContinuationToken;

                    if (result.Items != null) {
                        foreach (var item in result.Items) {
                            pageResult.Results.Add(item);
                        }
                    }
                } while (!string.IsNullOrEmpty(continuationToken));
            }
            catch (Exception e) {
                // skip this node
                Trace.TraceError("Cannot get published nodes for endpointId'{0}'", endpointId);
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
                pageResult.Error = e.Message;
            }
            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / pageResult.PageSize);
            return pageResult;
        }

        /// <summary>
        /// StartPublishing
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="publishingInterval"></param>
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StartPublishing(string endpointId, string nodeId, 
            int samplingInterval, int publishingInterval) {

            try {
                var requestApiModel = new PublishStartRequestApiModel() {
                    Item = new PublishedItemApiModel() {
                        NodeId = nodeId,
                        SamplingInterval = TimeSpan.FromMilliseconds(samplingInterval),
                        PublishingInterval = TimeSpan.FromMilliseconds(publishingInterval)
                    }
                };

                var resultApiModel = await _publisherService.NodePublishStartAsync(endpointId, requestApiModel);
                return resultApiModel.ErrorInfo == null;
            }
            catch(Exception e) {
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
            }
            return false;
        }

        /// <summary>
        /// StopPublishing
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StopPublishing(string endpointId, string nodeId) {
            try { 
                var requestApiModel = new PublishStopRequestApiModel() {
                        NodeId = nodeId,
                };
                var resultApiModel = await _publisherService.NodePublishStopAsync(endpointId, requestApiModel);
                return resultApiModel.ErrorInfo == null;
            }
            catch (Exception e) {
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
            }
            return false;
        }

        private readonly IPublisherServiceApi _publisherService;
    }
}
