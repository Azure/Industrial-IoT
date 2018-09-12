// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
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
    public class AddressSpaceServices : INodeServices<EndpointModel>,
        IBrowseServices<EndpointModel> {

        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public AddressSpaceServices(IEndpointServices client, IValueEncoder codec,
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
                var rootId = request?.NodeId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId)) {
                    rootId = ObjectIds.ObjectsFolder;
                }
                var typeId = request?.ReferenceTypeId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(typeId)) {
                    typeId = ReferenceTypeIds.HierarchicalReferences;
                }
                var view = request?.View == null ? null : new ViewDescription {
                    ViewId = request.View.ViewId.ToNodeId(session.MessageContext),
                    Timestamp = request.View.Timestamp ?? DateTime.MinValue,
                    ViewVersion = request.View.Version ?? 0
                };
                var excludeReferences = request.MaxReferencesToReturn.HasValue &&
                    request.MaxReferencesToReturn.Value == 0;
                var result = new BrowseResultModel();
                if (!excludeReferences) {
                    var direction = (request.Direction ?? OpcUa.Models.BrowseDirection.Forward)
                        .ToStackType();
                    // Browse and read children
                    result.References = new List<NodeReferenceModel>();
                    var response = session.Browse(
                        null, ViewDescription.IsDefault(view) ? null : view, rootId,
                        request.MaxReferencesToReturn ?? 0u,
                        direction, typeId, !(request?.NoSubtypes ?? false), 0,
                        out var continuationPoint, out var references);
                    result.ContinuationToken = await AddReferencesToBrowseResult(session,
                        request.TargetNodesOnly ?? false, result.References, continuationPoint,
                        references);
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
                    request.TargetNodesOnly ?? false, result.References, revised, references);
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
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var response = session.Browse(null, null, methodId, 0,
                    Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HasProperty, true, 0,
                    out var continuationPoint, out var references);
                var result = new MethodMetadataResultModel();
                foreach (var nodeReference in references) {
                    if (result.OutputArguments != null && result.InputArguments != null) {
                        break;
                    }
                    var isInput = nodeReference.BrowseName == BrowseNames.InputArguments;
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
                            Value = _codec.Encode(new Variant(argument.Value), session.MessageContext),
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
                var readNode = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(readNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var nodesToRead = new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = readNode,
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
                    result.Value = _codec.Encode(values[0].WrappedValue, session.MessageContext);
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
                var writeNode = request.Node.Id.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(writeNode)) {
                    throw new ArgumentException(nameof(request.Node.Id));
                }
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = writeNode,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(_codec.Decode(request.Value,
                            builtinType, request.Node.ValueRank, session.MessageContext)),
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
                        args.Add(_codec.Decode(arg.Value, builtinType, arg.ValueRank,
                            session.MessageContext));
                    }
                }
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var objectId = request.ObjectId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(objectId)) {
                    objectId = null;
                }
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = objectId,
                        MethodId = methodId,
                        InputArguments = args
                    }
                };
                var responseHeader = session.Call(null, requests, out var results,
                    out var diagnosticInfos);
                var result = new MethodCallResultModel();
                if (results != null && results.Count > 0 &&
                    StatusCode.IsGood(results[0].StatusCode)) {
                    result.Results = results[0].OutputArguments
                        .Select(v => _codec.Encode(v, session.MessageContext))
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
        private Task<NodeModel> ReadNodeModelAsync(Session session,
            NodeId nodeId, bool? children) {

            var currentNode = session.ReadNode(nodeId);
            var model = new NodeModel {
                Id = nodeId.AsString(session.MessageContext),
                DisplayName = currentNode.DisplayName?.ToString(),
                Description = currentNode.Description?.ToString(),
                NodeClass = currentNode.NodeClass.ToServiceType(),
                AccessRestrictions = currentNode.AccessRestrictions == 0 ?
                    (uint?)null : currentNode.AccessRestrictions,
                UserWriteMask = currentNode.UserWriteMask == 0 ?
                    (uint?)null : currentNode.UserWriteMask,
                WriteMask = currentNode.WriteMask == 0 ?
                    (uint?)null : currentNode.WriteMask,
                HasChildren = children
            };
            switch (currentNode) {
                case VariableNode vn:
                    model.DataType = vn.DataType.AsString(session.MessageContext);
                    model.ArrayDimensions = vn.ArrayDimensions == null ||
                        vn.ArrayDimensions.Count == 0 ? null : vn.ArrayDimensions.ToArray();
                    model.ValueRank = vn.ValueRank;
                    model.AccessLevel = (vn.AccessLevelEx | vn.AccessLevel) == 0 ?
                        (uint?)null : vn.AccessLevelEx | vn.AccessLevel;
                    model.UserAccessLevel = vn.UserAccessLevel == 0 ?
                        (uint?)null : vn.UserAccessLevel;
                    model.Historizing = !vn.Historizing ?
                        (bool?)null : true;
                    model.MinimumSamplingInterval = (int)vn.MinimumSamplingInterval == 0 ?
                        (double?)null : vn.MinimumSamplingInterval;
                    break;
                case VariableTypeNode vtn:
                    model.DataType = vtn.DataType.AsString(session.MessageContext);
                    model.ArrayDimensions = vtn.ArrayDimensions == null ||
                        vtn.ArrayDimensions.Count == 0 ? null : vtn.ArrayDimensions.ToArray();
                    model.ValueRank = vtn.ValueRank;
                    model.IsAbstract = !vtn.IsAbstract ?
                        (bool?)null : true;
                    model.DefaultValue = _codec.Encode(vtn.Value, session.MessageContext);
                    break;
                case ObjectTypeNode otn:
                    model.IsAbstract = !otn.IsAbstract ?
                        (bool?)null : true;
                    break;
                case ObjectNode on:
                    model.EventNotifier = on.EventNotifier == 0 ?
                        (byte?)null : on.EventNotifier;
                    break;
                case DataTypeNode dtn:
                    model.IsAbstract = !dtn.IsAbstract ?
                        (bool?)null : true;
                    model.DataTypeDefinition = dtn.DataTypeDefinition == null ? null :
                        _codec.Encode(new Variant(dtn.DataTypeDefinition));
                    break;
                case ReferenceTypeNode rtn:
                    model.IsAbstract = !rtn.IsAbstract ?
                        (bool?)null : true;
                    model.InverseName = rtn.InverseName?.ToString();
                    model.Symmetric = rtn.Symmetric;
                    break;
                case ViewNode vn:
                    model.EventNotifier = vn.EventNotifier == 0 ?
                        (byte?)null : vn.EventNotifier;
                    model.ContainsNoLoops = vn.ContainsNoLoops;
                    break;
                case MethodNode mn:
                    model.Executable = mn.Executable;
                    model.UserExecutable = !mn.Executable ?
                        (bool?)null : mn.UserExecutable;
                    break;
            }
            return Task.FromResult(model);
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="targetNodesOnly"></param>
        /// <param name="result"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        private async Task<string> AddReferencesToBrowseResult(Session session,
            bool targetNodesOnly, List<NodeReferenceModel> result, byte[] continuationPoint,
            List<ReferenceDescription> references) {
            if (references != null) {
                foreach (var reference in references) {
                    try {
                        var nodeId = reference.NodeId.ToNodeId(session.NamespaceUris);
                        if (targetNodesOnly &&
                            result.Any(r => r.Target.Id == nodeId.AsString(session.MessageContext))) {
                            continue;
                        }
                        // Check for children
                        bool? children = null;
                        try {
                            var response = session.Browse(null, null, nodeId, 1,
                                Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                                true, 0, out var tmp, out var childReferences);
                            children = childReferences.Count != 0;
                        }
                        catch (Exception ex) {
                            _logger.Debug("Failed to obtain hasChildren information", () => ex);
                        }
                        var model = await ReadNodeModelAsync(session, nodeId, children);
                        if (targetNodesOnly) {
                            result.Add(new NodeReferenceModel { Target = model });
                            continue;
                        }
                        result.Add(new NodeReferenceModel {
                            BrowseName = 
                                reference.BrowseName.AsString(session.MessageContext),
                            Id =
                                reference.ReferenceTypeId.AsString(session.MessageContext),
                            Direction = reference.IsForward ?
                                OpcUa.Models.BrowseDirection.Forward :
                                OpcUa.Models.BrowseDirection.Backward,
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
        private readonly IValueEncoder _codec;
        private readonly IEndpointServices _client;
    }
}
