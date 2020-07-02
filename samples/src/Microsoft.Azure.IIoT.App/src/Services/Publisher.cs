// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.App.Common;
    using System;
    using System.Threading.Tasks;
    using Serilog;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Publisher {

        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="publisherService"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public Publisher(IPublisherServiceApi publisherService, ITwinServiceApi twinService, IJsonSerializer serializer, ILogger logger, UICommon commonHelper) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _publisherService = publisherService ?? throw new ArgumentNullException(nameof(publisherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonHelper = commonHelper ?? throw new ArgumentNullException(nameof(commonHelper));
            _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
        }

        /// <summary>
        /// PublishedAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns>PublishedNode</returns>
        public async Task<PagedResult<ListNode>> PublishedAsync(string endpointId, bool readValues, CredentialModel credential = null) {
            var pageResult = new PagedResult<ListNode>();
            var model = new ValueReadRequestApiModel();

            try {
                var continuationToken = string.Empty;
                do {
                    var result = await _publisherService.NodePublishListAsync(endpointId, continuationToken);
                    continuationToken = result.ContinuationToken;

                    if (result.Items != null) {
                        foreach (var item in result.Items) {
                            model.NodeId = item.NodeId;
                            model.Header = Elevate(new RequestHeaderApiModel(), credential);
                            var readResponse = readValues ? await _twinService.NodeValueReadAsync(endpointId, model) : null;
                            pageResult.Results.Add(new ListNode {
                                PublishedItem = item,
                                Value = readResponse?.Value?.ToJson()?.TrimQuotes(),
                                DataType = readResponse ?.DataType
                            });
                        }
                    }
                } while (!string.IsNullOrEmpty(continuationToken));
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = $"Cannot get published nodes for endpointId'{endpointId}'";
                _logger.Error(e, message);
                pageResult.Error = message;
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
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StartPublishingAsync(string endpointId, string nodeId, string displayName,
            TimeSpan? samplingInterval, TimeSpan? publishingInterval, TimeSpan? heartBeatInterval, CredentialModel credential = null) {

            try {
                var requestApiModel = new PublishStartRequestApiModel() {
                    Item = new PublishedItemApiModel() {
                        NodeId = nodeId,
                        SamplingInterval = samplingInterval,
                        PublishingInterval = publishingInterval,
                        HeartbeatInterval = heartBeatInterval
                    }
                };

                requestApiModel.Header = Elevate(new RequestHeaderApiModel(), credential);

                var resultApiModel = await _publisherService.NodePublishStartAsync(endpointId, requestApiModel);
                return resultApiModel.ErrorInfo == null;
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot publish node {nodeId} on endpointId '{endpointId}'", nodeId, endpointId);
            }
            return false;
        }

        /// <summary>
        /// StopPublishing
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>ErrorStatus</returns>
        public async Task<bool> StopPublishingAsync(string endpointId, string nodeId, CredentialModel credential = null) {
            try {
                var requestApiModel = new PublishStopRequestApiModel() {
                    NodeId = nodeId,
                };
                requestApiModel.Header = Elevate(new RequestHeaderApiModel(), credential);

                var resultApiModel = await _publisherService.NodePublishStopAsync(endpointId, requestApiModel);
                return resultApiModel.ErrorInfo == null;
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot unpublish node {nodeId} on endpointId '{endpointId}'", nodeId, endpointId);
            }
            return false;
        }

        /// <summary>
        /// Set Elevation property with credential
        /// </summary>
        /// <param name="header"></param>
        /// <param name="credential"></param>
        /// <returns>RequestHeaderApiModel</returns>
        private RequestHeaderApiModel Elevate(RequestHeaderApiModel header, CredentialModel credential) {
            if (credential != null) {
                if (!string.IsNullOrEmpty(credential.Username) && !string.IsNullOrEmpty(credential.Password)) {
                    header.Elevation = new CredentialApiModel {
                        Type = CredentialType.UserName,
                        Value = _serializer.FromObject(new {
                            user = credential.Username,
                            password = credential.Password
                        })
                    };
                }
            }
            return header;
        }

        private readonly IJsonSerializer _serializer;
        private readonly IPublisherServiceApi _publisherService;
        private readonly ITwinServiceApi _twinService;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
    }
}
