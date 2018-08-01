// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
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
        public OpcUaNodeServices(IOpcUaClient client, IOpcUaVariantCodec codec,
            ILogger logger) {
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
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(EndpointModel endpoint,
            BrowseRequestModel request) {

            // browse child nodes
            return await _client.ExecuteServiceAsync(endpoint, async session => {
                var rootId = request?.NodeId?.ToNodeId(session.MessageContext) ??
                    ObjectIds.ObjectsFolder;
                var typeId = request?.ReferenceTypeId?.ToNodeId(session.MessageContext) ??
                    ReferenceTypeIds.HierarchicalReferences;
                var excludeReferences = request.MaxReferencesToReturn.HasValue &&
                    request.MaxReferencesToReturn.Value == 0;
                var result = new BrowseResultModel();
                if (!excludeReferences) {
                    var direction = (request.Direction ?? Models.BrowseDirection.Forward)
                        .ToStackType();
                    // Browse and read children
                    result.References = new List<NodeReferenceModel>();
                    var response = session.Browse(null, null, rootId,
                        (uint)(request.MaxReferencesToReturn ?? 0u),
                        direction, typeId, !(request?.NoSubtypes ?? false), 0,
                        out var continuationPoint, out var references);
                    result.ContinuationToken = await AddReferencesToBrowseResult(session,
                        result.References, continuationPoint, references);
                }
                // Read root node
                result.Node = await ReadNodeModelAsync(session, rootId,
                    !excludeReferences ? result.References.Count != 0 : (bool?)null);
                return result;
            });
        }

        /// <summary>
        /// Browse remainder of references
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server to talk to</param>
        /// <param name="request">Continuation token</param>
        /// <returns></returns>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(EndpointModel endpoint,
            BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var continuationPoint = request.ContinuationToken.DecodeAsBase64();
            return await _client.ExecuteServiceAsync(endpoint, async session => {
                // Browse and read children
                var result = new BrowseNextResultModel {
                    References = new List<NodeReferenceModel>()
                };
                var response = session.BrowseNext(null, request.Abort ?? false,
                    continuationPoint, out var revised, out var references);
                result.ContinuationToken = await AddReferencesToBrowseResult(session,
                    result.References, revised, references);
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
                    Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HasProperty, true, 0,
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
                    if (!(session.ReadNode(node) is VariableNode argumentsNode)) {
                        continue;
                    }
                    var value = session.ReadValue(argumentsNode.NodeId);
                    if (!(value.Value is ExtensionObject[] argumentsList)) {
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
        /// <param name="children"></param>
        /// <returns></returns>
        private static Task<NodeModel> ReadNodeModelAsync(Session session,
            NodeId nodeId, bool? children) {

            var currentNode = session.ReadNode(nodeId);
            var model = new NodeModel {
                Id = nodeId.AsString(session.MessageContext),
                DisplayName = currentNode.DisplayName.ToString(),
                Description = currentNode.Description.ToString(),
                NodeClass = currentNode.NodeClass.ToServiceType(),
                HasChildren = children
            };
            switch (currentNode) {
                case VariableNode vn:
                    model.AccessLevel = vn.AccessLevel.ToString();
                    // model.UserAccessLevel = vn.UserAccessLevel.ToString();
                    model.ValueRank = vn.ValueRank;
                    model.DataType = vn.DataType.AsString(session.MessageContext);
                    // model.ArrayDimensions = vn.ArrayDimensions;
                    break;
                case VariableTypeNode vtn:
                    model.IsAbstract = vtn.IsAbstract;
                    model.ValueRank = vtn.ValueRank;
                    model.DataType = vtn.DataType.AsString(session.MessageContext);
                    // model.ArrayDimensions = vtn.ArrayDimensions;
                    // model.DefaultValue = vtn.Value;
                    break;
                case ObjectTypeNode otn:
                    model.IsAbstract = otn.IsAbstract;
                    break;
                case ObjectNode on:
                    model.EventNotifier = on.EventNotifier.ToString();
                    break;
                case DataTypeNode dtn:
                    model.IsAbstract = dtn.IsAbstract;
                    break;
                case ReferenceTypeNode rtn:
                    model.IsAbstract = rtn.IsAbstract;
                    // model.InverseName = rtn.InverseName;
                    // model.Symmetric = rtn.Symmetric;
                    break;
                case ViewNode vn:
                    model.EventNotifier = vn.EventNotifier.ToString();
                    // model.ContainsNoLoops = vn.ContainsNoLoops;
                    break;
                case MethodNode mn:
                    model.Executable = mn.UserExecutable;
                    break;
            }
            return Task.FromResult(model);
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="result"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        private static async Task<string> AddReferencesToBrowseResult(Session session,
            List<NodeReferenceModel> result, byte[] continuationPoint,
            List<ReferenceDescription> references) {
            if (references != null) {
                foreach (var reference in references) {
                    try {
                        var nodeId = reference.NodeId.ToNodeId(session.NamespaceUris);
                        // Check for children
                        bool children;
                        try {
                            var response = session.Browse(null, null, nodeId, 0,
                                Opc.Ua.BrowseDirection.Forward,
                                ReferenceTypeIds.HierarchicalReferences, true, 1,
                                out var tmp, out var childReferences);
                            children = (childReferences.Count != 0);
                        }
                        catch {
                            children = false;
                        }
                        var model = await ReadNodeModelAsync(session, nodeId, children);
                        result.Add(new NodeReferenceModel {
                            BrowseName = reference.BrowseName.ToString(),
                            Id = reference.ReferenceTypeId.AsString(
                                session.MessageContext),
                            Direction = reference.IsForward ?
                                Models.BrowseDirection.Forward : Models.BrowseDirection.Backward,
                            Target = model
                        });
                    }
                    catch {
                        // Skip node - TODO: Should we add a failure
                        // reference into the yet unused diagnostics instead?
                        continue;
                    }
                }
                return continuationPoint?.ToBase64String();
            }
            return null;
        }

        private readonly ILogger _logger;
        private readonly IOpcUaVariantCodec _codec;
        private readonly IOpcUaClient _client;
    }
}
