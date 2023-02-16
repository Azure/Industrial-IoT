// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.Api;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Browser {

        /// <summary>
        /// Current path
        /// </summary>
        public List<string> Path { get; set; }
        public MethodMetadataResponseModel Parameter { get; set; }
        public MethodCallResponseModel MethodCallResponse { get; set; }

        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="twinService"></param>
        /// <param name="logger"></param>
        /// <param name="serializer"></param>
        /// <param name="commonHelper"></param>
        public Browser(ITwinServiceApi twinService, IJsonSerializer serializer, ILogger logger, UICommon commonHelper) {
            _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonHelper = commonHelper ?? throw new ArgumentNullException(nameof(commonHelper));
        }

        /// <summary>
        /// Get tree
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        /// <param name="discovererId"></param>
        /// <param name="direction"></param>
        /// <param name="index"></param>
        /// <returns>ListNode</returns>
        public async Task<PagedResult<ListNode>> GetTreeAsync(string endpointId, string id,
            List<string> parentId, string discovererId, BrowseDirection direction, int index) {

            var pageResult = new PagedResult<ListNode>();
            var previousPage = new PagedResult<ListNode>();
            var model = new BrowseRequestModel {
                TargetNodesOnly = true,
                ReadVariableValues = true,
                MaxReferencesToReturn = _MAX_REFERENCES
            };

            if (direction == BrowseDirection.Forward) {
                model.NodeId = id;
                if (id == string.Empty) {
                    Path = new List<string>();
                }
            }
            else {
                model.NodeId = parentId.ElementAt(index - 1);
            }
            try {
                var browseData = await _twinService.NodeBrowseAsync(endpointId, model);

                _displayName = browseData.Node.DisplayName;

                if (direction == BrowseDirection.Forward) {
                    parentId.Add(browseData.Node.NodeId);
                    if (browseData.Node.DisplayName == null) {
                        browseData.Node.DisplayName = string.Empty;
                    }
                    Path.Add(browseData.Node.DisplayName);
                }
                else {
                    parentId.RemoveAt(parentId.Count - 1);
                    Path.RemoveRange(index, Path.Count - index);
                }

                if (!string.IsNullOrEmpty(browseData.ContinuationToken)) {
                    pageResult.PageCount = 2;
                }

                if (browseData.References != null) {
                    foreach (var nodeReference in browseData.References) {
                        previousPage.Results.Add(new ListNode {
                            Id = nodeReference.Target.NodeId.ToString(),
                            NodeClass = nodeReference.Target.NodeClass ?? 0,
                            NodeName = nodeReference.Target.DisplayName?.ToString(),
                            Children = (bool)nodeReference.Target.Children,
                            ParentIdList = parentId,
                            DiscovererId = discovererId,
                            AccessLevel = nodeReference.Target.AccessLevel ?? 0,
                            ParentName = _displayName,
                            DataType = nodeReference.Target.DataType,
                            Value = nodeReference.Target.Value?.ToJson()?.TrimQuotes(),
                            Publishing = false,
                            PublishedItem = null,
                            ErrorMessage = nodeReference.Target.ErrorInfo?.ErrorMessage
                        });
                    }
                }
                pageResult.Results = previousPage.Results;
                pageResult.ContinuationToken = browseData.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = $"Cannot browse node '{id}'";
                _logger.Error(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// Get tree next page
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="parentId"></param>
        /// <param name="discovererId"></param>
        /// <param name="previousPage"></param>
        /// <returns>ListNode</returns>
        public async Task<PagedResult<ListNode>> GetTreeNextAsync(string endpointId, List<string> parentId, string discovererId,
            PagedResult<ListNode> previousPage = null) {

            var pageResult = new PagedResult<ListNode>();
            var modelNext = new BrowseNextRequestModel {
                ContinuationToken = previousPage.ContinuationToken,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            try {
                var browseDataNext = await _twinService.NodeBrowseNextAsync(endpointId, modelNext);

                pageResult.PageCount = string.IsNullOrEmpty(browseDataNext.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;

                if (browseDataNext.References != null) {
                    foreach (var nodeReference in browseDataNext.References) {
                        previousPage.Results.Add(new ListNode {
                            Id = nodeReference.Target.NodeId.ToString(),
                            NodeClass = nodeReference.Target.NodeClass ?? 0,
                            NodeName = nodeReference.Target.DisplayName.ToString(),
                            Children = (bool)nodeReference.Target.Children,
                            ParentIdList = parentId,
                            DiscovererId = discovererId,
                            AccessLevel = nodeReference.Target.AccessLevel ?? 0,
                            ParentName = _displayName,
                            DataType = nodeReference.Target.DataType,
                            Value = nodeReference.Target.Value?.ToJson()?.TrimQuotes(),
                            Publishing = false,
                            PublishedItem = null
                        });
                    }
                }

                pageResult.Results = previousPage.Results;
                pageResult.ContinuationToken = browseDataNext.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                var message = "Cannot browse";
                _logger.Error(e, message);
                pageResult.Error = message;
            }
            return pageResult;
        }

        /// <summary>
        /// ReadValueAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>Read value</returns>
        public async Task<string> ReadValueAsync(string endpointId, string nodeId) {

            var model = new ValueReadRequestModel() {
                NodeId = nodeId
            };

            try {
                var value = await _twinService.NodeValueReadAsync(endpointId, model);

                return value.ErrorInfo == null ? (value.Value?.ToJson()?.TrimQuotes()) : value.ErrorInfo.ToString();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot read value of node '{nodeId}'", nodeId);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                return errorMessage;
            }
        }

        /// <summary>
        /// WriteValueAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        /// <returns>Status</returns>
        public async Task<string> WriteValueAsync(string endpointId, string nodeId, string value) {

            var model = new ValueWriteRequestModel() {
                NodeId = nodeId,
                Value = value
            };

            try {
                var response = await _twinService.NodeValueWriteAsync(endpointId, model);

                return response.ErrorInfo == null
                    ? string.Format("value successfully written to node '{0}'", nodeId)
                    : response.ErrorInfo.Diagnostics != null ? response.ErrorInfo.Diagnostics.ToString() : response.ErrorInfo.ToString();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot write value of node '{nodeId}'", nodeId);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                return errorMessage;
            }
        }

        /// <summary>
        /// GetParameterAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>Status</returns>
        public async Task<string> GetParameterAsync(string endpointId, string nodeId) {
            Parameter = new MethodMetadataResponseModel();
            var model = new MethodMetadataRequestModel() {
                MethodId = nodeId
            };
            try {
                Parameter = await _twinService.NodeMethodGetMetadataAsync(endpointId, model);

                return Parameter.ErrorInfo == null
                    ? null
                    : Parameter.ErrorInfo.Diagnostics != null ? Parameter.ErrorInfo.Diagnostics.ToString() : Parameter.ErrorInfo.ToString();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot get method parameter from node '{nodeId}'", nodeId);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                return errorMessage;
            }
        }

        /// <summary>
        /// MethodCallAsync
        /// </summary>
        /// <param name="parameterValues"></param>
        /// <param name="nodeId"></param>
        /// <returns>Status</returns>
        public async Task<string> MethodCallAsync(MethodMetadataResponseModel parameters, string[] parameterValues,
            string endpointId, string nodeId) {

            var argumentsList = new List<MethodCallArgumentModel>();
            var model = new MethodCallRequestModel() {
                MethodId = nodeId,
                ObjectId = parameters.ObjectId
            };

            try {
                if (parameters.InputArguments != null) {
                    var count = 0;
                    foreach (var item in parameters.InputArguments) {
                        var argument = new MethodCallArgumentModel {
                            Value = parameterValues[count] ?? string.Empty,
                            DataType = item.Type.DataType
                        };
                        argumentsList.Add(argument);
                        count++;
                    }
                    model.Arguments = argumentsList;
                }
                MethodCallResponse = await _twinService.NodeMethodCallAsync(endpointId, model);

                return MethodCallResponse.ErrorInfo == null
                    ? null
                    : MethodCallResponse.ErrorInfo.Diagnostics != null
                        ? MethodCallResponse.ErrorInfo.Diagnostics.ToString()
                        : MethodCallResponse.ErrorInfo.ToString();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Cannot get method parameter from node '{nodeId}'", nodeId);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                return errorMessage;
            }
        }

        private readonly ITwinServiceApi _twinService;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
        private const int _MAX_REFERENCES = 10;
        private static string _displayName;
    }
}
