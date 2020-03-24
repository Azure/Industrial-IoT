// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Serilog;
    using Microsoft.Azure.IIoT.App.Models;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using Microsoft.AspNetCore.Http;

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
        public Browser(ITwinServiceApi twinService, ILogger logger) {
            _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get tree
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        /// <param name="discovererId"></param>
        /// <param name="direction"></param>
        /// <returns>ListNode</returns>
        public async Task<PagedResult<ListNode>> GetTreeAsync(string endpointId, string id,
            List<string> parentId, string discovererId, BrowseDirection direction, int index, 
            CredentialModel credential = null) {
            var pageResult = new PagedResult<ListNode>();
            var model = new BrowseRequestApiModel {
                TargetNodesOnly = true,
                ReadVariableValues = true
            };

            if (direction == BrowseDirection.Forward) {
                model.MaxReferencesToReturn = _MAX_REFERENCES;
                model.NodeId = id;
                if (id == string.Empty) {
                    Path = new List<string>();
                }
            }
            else {
                model.NodeId = parentId.ElementAt(index - 1);
            }

            model.Header = Elevate(new RequestHeaderApiModel(), credential);

            try {
                var browseData = await _twinService.NodeBrowseAsync(endpointId, model);

                var continuationToken = browseData.ContinuationToken;
                var references = browseData.References;
                var browseDataNext = new BrowseNextResponseApiModel();

                if (direction == BrowseDirection.Forward) {
                    parentId.Add(browseData.Node.NodeId);
                    Path.Add(browseData.Node.DisplayName);
                }
                else {
                    parentId.RemoveAt(parentId.Count - 1);
                    Path.RemoveRange(index, Path.Count - index);
                }

                do {
                    if (references != null) {
                        foreach (var nodeReference in references) {
                            pageResult.Results.Add(new ListNode {
                                Id = nodeReference.Target.NodeId.ToString(),
                                NodeClass = nodeReference.Target.NodeClass ?? 0,
                                NodeName = nodeReference.Target.DisplayName.ToString(),
                                Children = (bool)nodeReference.Target.Children,
                                ParentIdList = parentId,
                                DiscovererId = discovererId,
                                AccessLevel = nodeReference.Target.AccessLevel ?? 0,
                                ParentName = browseData.Node.DisplayName,
                                DataType = nodeReference.Target.DataType,
                                Value = nodeReference.Target.Value?.ToString(),
                                Publishing = false,
                                PublishedItem = null
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(continuationToken)) {
                        bool? abort = null;
                        if (pageResult.Results.Count > 5) {
                            // TODO: !!! Implement real paging - need to make ux responsive for large # tags !!!
                            abort = true;
                        }
                        var modelNext = new BrowseNextRequestApiModel {
                            ContinuationToken = continuationToken,
                            Abort = abort
                        };
                        browseDataNext = await _twinService.NodeBrowseNextAsync(endpointId, modelNext);
                        if (abort == true) {
                            break;
                        }
                        references = browseDataNext.References;
                        continuationToken = browseDataNext.ContinuationToken;
                    }
                    else {
                        browseDataNext.References = null;
                    }

                } while (!string.IsNullOrEmpty(continuationToken) || browseDataNext.References != null);
            }
            catch (Exception e) {
                // skip this node
                _logger.Error($"Can not browse node '{id}'");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Error(errorMessage);
                string error = JToken.Parse(e.Message).ToString(Formatting.Indented);
                if (error.Contains(StatusCodes.Status401Unauthorized.ToString())) {
                    pageResult.Error = "Unauthorized access: Bad User Access Denied.";
                }
                else {
                    pageResult.Error = error;
                } 
            }

            pageResult.PageSize = 10;
            pageResult.RowCount = pageResult.Results.Count;
            pageResult.PageCount = (int)Math.Ceiling((decimal)pageResult.RowCount / 10);
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
                    return value.Value?.ToString();
                }
                else {
                    return value.ErrorInfo.ToString();
                }   
            }
            catch (Exception e) {
                _logger.Error($"Can not read value of node '{nodeId}'");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Error(errorMessage);
                string error = JToken.Parse(e.Message).ToString(Formatting.Indented);
                if (error.Contains(StatusCodes.Status401Unauthorized.ToString())) {
                    errorMessage = "Unauthorized access: Bad User Access Denied.";
                }
                else {
                    errorMessage = error;
                }
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
            catch (Exception e) {
                _logger.Error($"Can not write value of node '{nodeId}'");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Error(errorMessage);
                string error = JToken.Parse(e.Message).ToString(Formatting.Indented);
                if (error.Contains(StatusCodes.Status401Unauthorized.ToString())) {
                    errorMessage = "Unauthorized access: Bad User Access Denied.";
                }
                else {
                    errorMessage = error;
                }
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
            catch (Exception e) {
                _logger.Error($"Can not get method parameter from node '{nodeId}'");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Error(errorMessage);
                string error = JToken.Parse(e.Message).ToString(Formatting.Indented);
                if (error.Contains(StatusCodes.Status401Unauthorized.ToString())) {
                    errorMessage = "Unauthorized access: Bad User Access Denied.";
                }
                else {
                    errorMessage = error;
                }
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
            catch (Exception e) {
                _logger.Error($"Can not get method parameter from node '{nodeId}'");
                var errorMessage = string.Concat(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                _logger.Error(errorMessage);
                string error = JToken.Parse(e.Message).ToString(Formatting.Indented);
                if (error.Contains(StatusCodes.Status401Unauthorized.ToString())) {
                    errorMessage = "Unauthorized access: Bad User Access Denied.";
                }
                else {
                    errorMessage = error;
                }
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
                    header.Elevation = new CredentialApiModel();
                    header.Elevation.Type = CredentialType.UserName;
                    header.Elevation.Value = JToken.FromObject(new {
                        user = credential.Username,
                        password = credential.Password
                    });
                }
            }
            return header;
        }

        private readonly ITwinServiceApi _twinService;
        private readonly ILogger _logger;
        private const int _MAX_REFERENCES = 50;
    }
}
