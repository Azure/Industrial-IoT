// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services
{
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using global::Azure.IIoT.OpcUa.Models;
    using global::Azure.IIoT.OpcUa.Services.Sdk;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Publisher
    {
        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="publisherService"></param>
        /// <param name="twinService"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="commonHelper"></param>
        public Publisher(IPublisherServiceApi publisherService, ITwinServiceApi twinService, ILogger logger, UICommon commonHelper)
        {
            _publisherService = publisherService ?? throw new ArgumentNullException(nameof(publisherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonHelper = commonHelper ?? throw new ArgumentNullException(nameof(commonHelper));
            _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
        }

        /// <summary>
        /// PublishedAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="readValues"></param>
        /// <returns>PublishedNode</returns>
        public async Task<PagedResult<ListNode>> PublishedAsync(string endpointId, bool readValues)
        {
            var pageResult = new PagedResult<ListNode>();
            var model = new ValueReadRequestModel();

            try
            {
                var continuationToken = string.Empty;
                do
                {
                    var result = await _publisherService.NodePublishListAsync(endpointId, continuationToken).ConfigureAwait(false);
                    continuationToken = result.ContinuationToken;

                    if (result.Items != null)
                    {
                        foreach (var item in result.Items)
                        {
                            model.NodeId = item.NodeId;
                            var readResponse = readValues ? await _twinService.NodeValueReadAsync(endpointId, model).ConfigureAwait(false) : null;
                            pageResult.Results.Add(new ListNode
                            {
                                PublishedItem = item,
                                Value = readResponse?.Value?.ToJson()?.TrimQuotes(),
                                DataType = readResponse?.DataType
                            });
                        }
                    }
                } while (!string.IsNullOrEmpty(continuationToken));
            }
            catch (UnauthorizedAccessException)
            {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot get published nodes for {Endpoint}.", endpointId);
                pageResult.Error = $"Cannot get published nodes for endpointId'{endpointId}'";
            }
            pageResult.PageSize = _commonHelper.PageLength;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / pageResult.PageSize);
            return pageResult;
        }

        /// <summary>
        /// StartPublishing
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <param name="displayName"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="publishingInterval"></param>
        /// <param name="heartBeatInterval"></param>
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StartPublishingAsync(string endpointId, string nodeId, string displayName,
            TimeSpan? samplingInterval, TimeSpan? publishingInterval, TimeSpan? heartBeatInterval)
        {
            try
            {
                var requestModel = new PublishStartRequestModel()
                {
                    Item = new PublishedItemModel()
                    {
                        NodeId = nodeId,
                        SamplingInterval = samplingInterval,
                        PublishingInterval = publishingInterval,
                        HeartbeatInterval = heartBeatInterval
                    }
                };

                var resultModel = await _publisherService.NodePublishStartAsync(endpointId, requestModel).ConfigureAwait(false);
                return resultModel.ErrorInfo == null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot publish node {NodeId} on endpointId '{EndpointId}'", nodeId, endpointId);
            }
            return false;
        }

        /// <summary>
        /// StopPublishing
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StopPublishingAsync(string endpointId, string nodeId)
        {
            try
            {
                var requestModel = new PublishStopRequestModel()
                {
                    NodeId = nodeId
                };

                var resultModel = await _publisherService.NodePublishStopAsync(endpointId, requestModel).ConfigureAwait(false);
                return resultModel.ErrorInfo == null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot unpublish node {NodeId} on endpointId '{EndpointId}'", nodeId, endpointId);
            }
            return false;
        }

        private readonly IPublisherServiceApi _publisherService;
        private readonly ITwinServiceApi _twinService;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
    }
}
