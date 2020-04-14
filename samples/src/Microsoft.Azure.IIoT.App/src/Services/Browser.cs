// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.App.Common;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Browser {

        /// <summary>
        /// Current path
        /// </summary>
        public List<string> Path { get; set; }
        public MethodMetadataResponseApiModel Parameter { get; set; }
        public MethodCallResponseApiModel MethodCallResponse { get; set; }

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
        /// <param name="credential"></param>
        /// <returns>ListNode</returns>
        public async Task<PagedResult<ListNode>> GetTreeAsync(string endpointId, string id,
            List<string> parentId, string discovererId, BrowseDirection direction, int index,
            CredentialModel credential = null) {

            var pageResult = new PagedResult<ListNode>();
            var header = Elevate(new RequestHeaderApiModel(), credential);
            var previousPage = new PagedResult<ListNode>();
            var model = new BrowseRequestApiModel {
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
            model.Header = header;

            try {
                var browseData = await _twinService.NodeBrowseAsync(endpointId, model);

                _displayName = browseData.Node.DisplayName;

                if (direction == BrowseDirection.Forward) {
                    parentId.Add(browseData.Node.NodeId);
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
                pageResult.ContinuationToken = browseData.ContinuationToken;
                pageResult.PageSize = _commonHelper.PageLength;
                pageResult.RowCount = pageResult.Results.Count;
            }
            catch (UnauthorizedAccessException) {
                pageResult.Error = "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                // skip this node
                _logger.Error(e, "Can not browse node '{id}'", id);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                pageResult.Error = errorMessage;
            }
            return pageResult;
        }

        /// <summary>
        /// Get tree next page
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="parentId"></param>
        /// <param name="discovererId"></param>
        /// <param name="credential"></param>
        /// <param name="previousPage"></param>
        /// <returns>ListNode</returns>
        public async Task<PagedResult<ListNode>> GetTreeNextAsync(string endpointId, List<string> parentId, string discovererId,
            CredentialModel credential = null, PagedResult<ListNode> previousPage = null) {

            var pageResult = new PagedResult<ListNode>();
            var header = Elevate(new RequestHeaderApiModel(), credential);
            var modelNext = new BrowseNextRequestApiModel {
                ContinuationToken = previousPage.ContinuationToken,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            modelNext.Header = header;

            try {
                var browseDataNext = await _twinService.NodeBrowseNextAsync(endpointId, modelNext);

                if (string.IsNullOrEmpty(browseDataNext.ContinuationToken)) {
                    pageResult.PageCount = previousPage.PageCount;
                }
                else {
                    pageResult.PageCount = previousPage.PageCount + 1;
                }

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
                // skip this node
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                pageResult.Error = errorMessage;
            }
            return pageResult;
        }

        /// <summary>
        /// ReadValueAsync
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns>Read value</returns>
        public async Task<string> ReadValueAsync(string endpointId, string nodeId, CredentialModel credential = null) {

            var model = new ValueReadRequestApiModel() {
                NodeId = nodeId
            };

            model.Header = Elevate(new RequestHeaderApiModel(), credential);

            try {
                var value = await _twinService.NodeValueReadAsync(endpointId, model);

                if (value.ErrorInfo == null) {
                    return value.Value?.ToJson()?.TrimQuotes();
                }
                else {
                    return value.ErrorInfo.ToString();
                }
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Can not read value of node '{nodeId}'", nodeId);
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
        public async Task<string> WriteValueAsync(string endpointId, string nodeId, string value, CredentialModel credential = null) {

            var model = new ValueWriteRequestApiModel() {
                NodeId = nodeId,
                Value = value
            };

            model.Header = Elevate(new RequestHeaderApiModel(), credential);

            try {
                var response = await _twinService.NodeValueWriteAsync(endpointId, model);

                if (response.ErrorInfo == null) {
                    return string.Format("value successfully written to node '{0}'", nodeId);
                }
                else {
                    if (response.ErrorInfo.Diagnostics != null) {
                        return response.ErrorInfo.Diagnostics.ToString();
                    }
                    else {
                        return response.ErrorInfo.ToString();
                    }
                }
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Can not write value of node '{nodeId}'", nodeId);
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
        public async Task<string> GetParameterAsync(string endpointId, string nodeId, CredentialModel credential = null) {
            Parameter = new MethodMetadataResponseApiModel();
            var model = new MethodMetadataRequestApiModel() {
                MethodId = nodeId
            };

            model.Header = Elevate(new RequestHeaderApiModel(), credential);

            try {
                Parameter = await _twinService.NodeMethodGetMetadataAsync(endpointId, model);

                if (Parameter.ErrorInfo == null) {
                    return null;
                }
                else {
                    if (Parameter.ErrorInfo.Diagnostics != null) {
                        return Parameter.ErrorInfo.Diagnostics.ToString();
                    }
                    else {
                        return Parameter.ErrorInfo.ToString();
                    }
                }
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Can not get method parameter from node '{nodeId}'", nodeId);
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
        public async Task<string> MethodCallAsync(MethodMetadataResponseApiModel parameters, string[] parameterValues,
            string endpointId, string nodeId, CredentialModel credential = null) {

            var argumentsList = new List<MethodCallArgumentApiModel>();
            var model = new MethodCallRequestApiModel() {
                MethodId = nodeId,
                ObjectId = parameters.ObjectId
            };

            model.Header = Elevate(new RequestHeaderApiModel(), credential);

            try {
                var count = 0;
                foreach (var item in parameters.InputArguments) {
                    var argument = new MethodCallArgumentApiModel {
                        Value = parameterValues[count] ?? string.Empty,
                        DataType = item.Type.DataType
                    };
                    argumentsList.Add(argument);
                    count++;
                }
                model.Arguments = argumentsList;

                MethodCallResponse = await _twinService.NodeMethodCallAsync(endpointId, model);

                if (MethodCallResponse.ErrorInfo == null) {
                    return null;
                }
                else {
                    if (MethodCallResponse.ErrorInfo.Diagnostics != null) {
                        return MethodCallResponse.ErrorInfo.Diagnostics.ToString();
                    }
                    else {
                        return MethodCallResponse.ErrorInfo.ToString();
                    }
                }
            }
            catch (UnauthorizedAccessException) {
                return "Unauthorized access: Bad User Access Denied.";
            }
            catch (Exception e) {
                _logger.Error(e, "Can not get method parameter from node '{nodeId}'", nodeId);
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                return errorMessage;
            }
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

        private readonly ITwinServiceApi _twinService;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly UICommon _commonHelper;
        private const int _MAX_REFERENCES = 10;
        private static string _displayName;
    }
}
