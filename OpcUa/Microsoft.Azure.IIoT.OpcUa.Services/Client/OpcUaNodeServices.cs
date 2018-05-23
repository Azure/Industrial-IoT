// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Client {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// This class provides access to a servers address space providing node
    /// and browse services.  It uses the OPC ua client interface to access
    /// the server, which can leverage tcp or proxy transport, i.e. can access
    /// the server locally and from the cloud.
    /// </summary>
    public class OpcUaNodeServices : IOpcUaNodeServices<EndpointModel>,
        IOpcUaBrowseServices<EndpointModel> {

        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public OpcUaNodeServices(IOpcUaClient client, IOpcUaVariantCodec codec, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes
        /// if not excluded.
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server to talk to</param>
        /// <param name="request">browse node and filters</param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseAsync(EndpointModel endpoint,
            BrowseRequestModel request) {

            // browse child nodes
            return await _client.ExecuteServiceAsync(endpoint, async session => {
                var rootId = request?.NodeId?.ToNodeId(session.MessageContext) ??
                ObjectIds.ObjectsFolder;

                var excludeReferences = request?.ExcludeReferences ?? false;
                var result = new BrowseResultModel();
                if (!excludeReferences) {
                    // Browse and read children
                    result.References = new List<NodeReferenceModel>();
                    var response = session.Browse(null, null, rootId, 0,
                        BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                        true, 0, out var continuationPoint, out var references);
                    if (references != null) {
                        foreach (var nodeReference in references) {
                            var nodeId = nodeReference.NodeId.ToNodeId(session.NamespaceUris);
                            try {
                                response = session.Browse(null, null, nodeId, 0,
                                    BrowseDirection.Forward,
                                    ReferenceTypeIds.HierarchicalReferences,
                                    true, 0, out var childContinuationPoint,
                                    out var childReferences);

                                var model = await ReadNodeModelAsync(session,
                                    nodeId, request?.NodeId, (childReferences.Count != 0));
                                result.References.Add(new NodeReferenceModel {
                                    BrowseName = nodeReference.BrowseName.ToString(),
                                    Id = nodeReference.ReferenceTypeId.AsString(
                                        session.MessageContext),
                                    Target = model
                                });
                            }
                            catch {
                                // Skip node - TODO: Should we add a failure
                                // reference into the yet unused diagnostics instead?
                                continue;
                            }
                        }
                    }
                }
                // Read root node
                result.Node = await ReadNodeModelAsync(session, rootId, request?.Parent,
                    !excludeReferences ? result.References.Count != 0 : (bool?)null);
                return result;
            });
        }

        /// <summary>
        /// Read method meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointModel endpoint, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentException(nameof(request.MethodId));
            }
            return await _client.ExecuteServiceAsync(endpoint, session => {
                var response = session.Browse(null, null, request.MethodId, 0,
                    BrowseDirection.Forward, ReferenceTypeIds.HasProperty, true, 0,
                    out var continuationPoint, out var references);

                var result = new MethodMetadataResultModel();
                foreach (var nodeReference in references) {
                    if (result.OutputArguments != null && result.InputArguments != null) {
                        break;
                    }
                    var isInput = (nodeReference.BrowseName == BrowseNames.InputArguments);
                    if (!isInput && nodeReference.BrowseName != BrowseNames.OutputArguments) {
                        continue;
                    }
                    var node = nodeReference.NodeId.ToNodeId(session.NamespaceUris);
                    var argumentsNode = session.ReadNode(
                         nodeReference.NodeId.ToNodeId(session.NamespaceUris)) as VariableNode;
                    if (argumentsNode == null) {
                        continue;
                    }
                    var value = session.ReadValue(argumentsNode.NodeId);
                    var argumentsList = value.Value as ExtensionObject[];
                    if (argumentsList == null) {
                        continue;
                    }
                    var argList = new List<MethodArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                        var dataTypeIdNode = session.ReadNode(argument.DataType);
                        var arg = new MethodArgumentModel {
                            Name = argument.Name,
                            Value = _codec.Encode(new Variant(argument.Value)),
                            ValueRank = argument.ValueRank,
                            ArrayDimensions = argument.ArrayDimensions.ToArray(),
                            Description = argument.Description.ToString(),
                            TypeId = argument.DataType.AsString(session.MessageContext),
                            TypeName = dataTypeIdNode.DisplayName.Text,
                        };
                        argList.Add(arg);
                    }
                    if (isInput) {
                        result.InputArguments = argList;
                        continue;
                    }
                    result.OutputArguments = argList;
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ValueReadResultModel> NodeValueReadAsync(EndpointModel endpoint,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return _client.ExecuteServiceAsync(endpoint, session => {
                var nodesToRead = new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = request.NodeId.ToNodeId(session.MessageContext),
                        AttributeId = Attributes.Value,
                        IndexRange = null,
                        DataEncoding = null
                    }
                };
                var responseHeader = session.Read(null, 0, TimestampsToReturn.Both,
                    nodesToRead, out var values, out var diagnosticInfos);
                var result = new ValueReadResultModel();
                if (values != null && values.Count > 0 && values[0] != null) {
                    result.ServerPicoseconds = values[0].ServerPicoseconds != 0 ?
                        values[0].ServerPicoseconds : (ushort?)null;
                    result.ServerTimestamp = values[0].ServerTimestamp != DateTime.MinValue ?
                        values[0].ServerTimestamp : (DateTime?)null;
                    result.SourcePicoseconds = values[0].SourcePicoseconds != 0 ?
                        values[0].SourcePicoseconds : (ushort?)null;
                    result.SourceTimestamp = values[0].SourceTimestamp != DateTime.MinValue ?
                        values[0].SourceTimestamp : (DateTime?)null;
                    result.Value = _codec.Encode(values[0].WrappedValue);
                }
                if (diagnosticInfos != null && diagnosticInfos.Count > 0) {
                    result.Diagnostics = JToken.FromObject(diagnosticInfos[0]);
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ValueWriteResultModel> NodeValueWriteAsync(EndpointModel endpoint,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Node == null) {
                throw new ArgumentNullException(nameof(request.Node));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            if (string.IsNullOrEmpty(request.Node.Id)) {
                throw new ArgumentException(nameof(request.Node.Id));
            }
            if (string.IsNullOrEmpty(request.Node.DataType)) {
                throw new ArgumentException(nameof(request.Node.DataType));
            }
            return _client.ExecuteServiceAsync(endpoint, session => {
                var builtinType = TypeInfo.GetBuiltInType(request.Node.DataType,
                    session.TypeTree);
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = request.Node.Id.ToNodeId(session.MessageContext),
                        AttributeId = Attributes.Value,
                        Value = new DataValue(_codec.Decode(
                            request.Value, builtinType, request.Node.ValueRank)),
                        IndexRange = null
                    }
                };
                var result = new ValueWriteResultModel();
                session.Write(null, nodesToWrite, out var results, out var diagnosticInfos);
                if (diagnosticInfos != null && diagnosticInfos.Count > 0) {
                    result.Diagnostics = JToken.FromObject(diagnosticInfos[0]);
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Call method on endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<MethodCallResultModel> NodeMethodCallAsync(EndpointModel endpoint,
            MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            if (request.InputArguments != null) {
                foreach (var arg in request.InputArguments) {
                    if (string.IsNullOrEmpty(arg.TypeId)) {
                        throw new ArgumentNullException(nameof(arg.TypeId));
                    }
                }
            }
            return _client.ExecuteServiceAsync(endpoint, session => {
                var args = new VariantCollection();
                if (request.InputArguments != null) {
                    foreach (var arg in request.InputArguments) {
                        var builtinType = TypeInfo.GetBuiltInType(
                            arg.TypeId.ToNodeId(session.MessageContext), session.TypeTree);
                        args.Add(_codec.Decode(arg.Value, builtinType, arg.ValueRank));
                    }
                }
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = request.ObjectId?.ToNodeId(session.MessageContext),
                        MethodId = request.MethodId.ToNodeId(session.MessageContext),
                        InputArguments = args
                    }
                };
                var responseHeader = session.Call(null, requests,
                    out var results, out var diagnosticInfos);
                var result = new MethodCallResultModel();
                if (results != null && results.Count > 0 &&
                    StatusCode.IsGood(results[0].StatusCode)) {
                    result.Results = results[0].OutputArguments
                        .Select(_codec.Encode)
                        .ToList();
                }
                if (diagnosticInfos != null && diagnosticInfos.Count > 0) {
                    result.Diagnostics = JToken.FromObject(diagnosticInfos[0]);
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Read node properties as node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="nodeId"></param>
        /// <param name="parentNode"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private static Task<NodeModel> ReadNodeModelAsync(Session session,
            NodeId nodeId, string parentNode, bool? children) {

            var currentNode = session.ReadNode(nodeId);
            var model = new NodeModel {
                Id = nodeId.AsString(session.MessageContext),
                ParentNode = parentNode,
                Text = currentNode.DisplayName.ToString(),
                NodeClass = currentNode.NodeClass.ToString(),
                HasChildren = children
            };
            if (currentNode is VariableNode variableNode) {
                model.AccessLevel = variableNode.UserAccessLevel.ToString();
                model.ValueRank = variableNode.ValueRank;
                model.DataType =
                    variableNode.DataType.AsString(session.MessageContext);
            }
            if (currentNode is ObjectNode objectNode) {
                model.EventNotifier = objectNode.EventNotifier.ToString();
            }
            if (currentNode is ViewNode viewNode) {
                model.EventNotifier = viewNode.EventNotifier.ToString();
            }
            if (currentNode is MethodNode methodNode) {
                model.Executable = methodNode.UserExecutable;
            }
            return Task.FromResult(model);
        }

        private readonly ILogger _logger;
        private readonly IOpcUaVariantCodec _codec;
        private readonly IOpcUaClient _client;
    }
}
