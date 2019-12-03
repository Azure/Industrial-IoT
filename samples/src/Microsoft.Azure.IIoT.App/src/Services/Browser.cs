// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Browser code behind
    /// </summary>
    public class Browser {

        /// <summary>
        /// Current path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="twinService"></param>
        public Browser(ITwinServiceApi twinService) {
            _twinService = twinService;
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
        public async Task<PagedResult<ListNode>> GetTreeAsync(string endpointId,
            string id, List<string> parentId, string supervisorId, BrowseDirection direction) {
            var pageResult = new PagedResult<ListNode>();
            var model = new BrowseRequestApiModel {
                TargetNodesOnly = true
            };

            if (direction == BrowseDirection.Forward) {
                model.MaxReferencesToReturn = 10;
                model.NodeId = id;
                if (id == string.Empty) {
                    Path = string.Empty;
                }
            }
            else {
                model.NodeId = parentId.ElementAt(parentId.Count - 2);
            }

            try {
                var browseData = await _twinService.NodeBrowseAsync(endpointId, model);

                var continuationToken = browseData.ContinuationToken;
                var references = browseData.References;
                var browseDataNext = new BrowseNextResponseApiModel();

                if (direction == BrowseDirection.Forward) {
                    parentId.Add(browseData.Node.NodeId);
                    Path += "/" + browseData.Node.DisplayName;
                }
                else {
                    parentId.RemoveAt(parentId.Count - 1);
                    Path = Path.Substring(0, Path.LastIndexOf("/"));
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
                                SupervisorId = supervisorId,
                                AccessLevel = nodeReference.Target.AccessLevel ?? 0,
                                ParentName = browseData.Node.DisplayName,
                                DataType = nodeReference.Target.DataType,
                                Value = nodeReference.Target.NodeClass.ToString() == "Variable" ? 
                                        await ReadValueAsync(endpointId, nodeReference.Target.NodeId.ToString()) :
                                        string.Empty
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(continuationToken)) {
                        var modelNext = new BrowseNextRequestApiModel {
                            ContinuationToken = continuationToken
                        };
                        browseDataNext = await _twinService.NodeBrowseNextAsync(endpointId, modelNext);
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
                Trace.TraceError("Can not browse node '{0}'", id);
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
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
        public async Task<string> ReadValueAsync(string endpointId, string nodeId) {

            var model = new ValueReadRequestApiModel() {
                NodeId = nodeId
            };
            
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
                Trace.TraceError("Can not read value of node '{0}'", nodeId);
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
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

            var model = new ValueWriteRequestApiModel() {
                NodeId = nodeId, 
                Value = value
            };

            try {
                var response = await _twinService.NodeValueWriteAsync(endpointId, model);

                if (response.ErrorInfo == null) {
                    return string.Format("value successfully written to node '{0}'", nodeId);
                }
                else {
                    return response.ErrorInfo.ToString();
                }
            }
            catch (Exception e) {
                Trace.TraceError("Can not write value of node '{0}'", nodeId);
                var errorMessage = string.Format(e.Message, e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
                return errorMessage;
            }
        }

        private readonly ITwinServiceApi _twinService;
    }
}
