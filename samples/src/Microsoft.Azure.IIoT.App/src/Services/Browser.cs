// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using global::Azure.IIoT.OpcUa.Services.Sdk;
    using global::Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Globalization;

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
        /// <param name="commonHelper"></param>
        public Browser(ITwinServiceApi twinService, ILogger logger, UICommon commonHelper) {
            _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
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
            var model = new BrowseFirstRequestModel {
                TargetNodesOnly = true,
                ReadVariableValues = true,
                MaxReferencesToReturn = _MAX_REFERENCES
            };

            if (direction == BrowseDirection.Forward) {
                model.NodeId = id;
                if (id?.Length == 0) {
                    Path = new List<string>();
                }
            }
            else {
                model.NodeId = parentId[index - 1];
            }
            try {
                var browseData = await _twinService.NodeBrowseAsync(endpointId, model).ConfigureAwait(false);

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
                            Id = nodeReference.Target.NodeId,
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
                _logger.LogError(e, "Cannot browse node '{Id}'", id);
                pageResult.Error = $"Cannot browse node '{id}'";
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
                var browseDataNext = await _twinService.NodeBrowseNextAsync(endpointId, modelNext).ConfigureAwait(false);

                pageResult.PageCount = string.IsNullOrEmpty(browseDataNext.ContinuationToken) ? previousPage.PageCount : previousPage.PageCount + 1;

                if (browseDataNext.References != null) {
                    foreach (var nodeReference in browseDataNext.References) {
                        previousPage.Results.Add(new ListNode {
                            Id = nodeReference.Target.NodeId,
                            NodeClass = nodeReference.Target.NodeClass ?? 0,
                            NodeName = nodeReference.Target.DisplayName,
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
                const string message = "Cannot browse";
                _logger.LogError(e, message);
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
                var value = await _twinService.NodeValueReadAsync(endpointId, model).ConfigureAwait(false);

                return value.ErrorInfo == null ? (value.Value?.ToJson()?.TrimQuotes()) : value.ErrorInfo.ToString();
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.LogError(e, "Cannot read value of node '{NodeId}'", nodeId);
                return string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
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
                var response = await _twinService.NodeValueWriteAsync(endpointId, model).ConfigureAwait(false);

                return response.ErrorInfo == null
                    ? string.Format(CultureInfo.InvariantCulture, "value successfully written to node '{0}'", nodeId)
                    : response.ErrorInfo.ErrorMessage;
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.LogError(e, "Cannot write value of node '{NodeId}'", nodeId);
                return string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
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
                Parameter = await _twinService.NodeMethodGetMetadataAsync(endpointId, model).ConfigureAwait(false);
                return Parameter.ErrorInfo?.ErrorMessage;
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.LogError(e, "Cannot get method parameter from node '{NodeId}'", nodeId);
                return string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
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
                MethodCallResponse = await _twinService.NodeMethodCallAsync(endpointId, model).ConfigureAwait(false);

                return MethodCallResponse.ErrorInfo?.ErrorMessage;
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.LogError(e, "Cannot get method parameter from node '{NodeId}'", nodeId);
                return string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
            }
        }

        private readonly ITwinServiceApi _twinService;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
        private const int _MAX_REFERENCES = 10;
        private static string _displayName;
    }
}
