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
    using Opc.Ua.Models;
    using System.Threading;

    /// <summary>
    /// This class provides access to a servers address space providing node
    /// and browse services.  It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    public class AddressSpaceServices : INodeServices<EndpointModel>,
        IBrowseServices<EndpointModel> {

        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public AddressSpaceServices(IEndpointServices client, IVariantEncoder codec,
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
                    rootId = ObjectIds.RootFolder;
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
                        request.TargetNodesOnly ?? false, request.ReadVariableValues ?? false,
                        result.References, continuationPoint, references);
                }
                // Read root node
                result.Node = await ReadNodeModelAsync(session, rootId, true,
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
                    request.TargetNodesOnly ?? false, request.ReadVariableValues ?? false,
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
            return await _client.ExecuteServiceAsync(endpoint, async session => {
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var response = session.Browse(null, null, methodId, 0,
                    Opc.Ua.BrowseDirection.Both, ReferenceTypeIds.Aggregates, true, 0,
                    out var continuationPoint, out var references);
                var result = new MethodMetadataResultModel();
                foreach (var nodeReference in references) {
                    if (result.OutputArguments != null &&
                        result.InputArguments != null &&
                        !string.IsNullOrEmpty(result.ObjectId)) {
                        break;
                    }
                    if (!nodeReference.IsForward) {
                        if (nodeReference.ReferenceTypeId == ReferenceTypeIds.HasComponent) {
                            result.ObjectId = nodeReference.NodeId.AsString(
                                session.MessageContext);
                        }
                        continue;
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
                    var argList = new List<MethodMetadataArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                        var dataTypeIdNode = await ReadNodeModelAsync(session,
                            argument.DataType, false, false);
                        var arg = new MethodMetadataArgumentModel {
                            Name = argument.Name,
                            DefaultValue = argument.Value == null ? null :
                                _codec.Encode(new Variant(argument.Value), out var type,
                                    session.MessageContext),
                            ValueRank = argument.ValueRank == ValueRanks.Scalar ?
                                (NodeValueRank?)null : (NodeValueRank)argument.ValueRank,
                            ArrayDimensions = argument.ArrayDimensions?.ToArray(),
                            Description = argument.Description?.ToString(),
                            Type = dataTypeIdNode
                        };
                        argList.Add(arg);
                    }
                    if (isInput) {
                        result.InputArguments = argList;
                    }
                    else {
                        result.OutputArguments = argList;
                    }
                }
                return result;
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
                        IndexRange = request.IndexRange,
                        //
                        // TODO:
                        // A QualifiedName that specifies the data encoding to
                        // be returned for the Value to be read.
                        // Only works for "Structure" types, which we need to
                        // check first.  However, then we should specify Xml
                        // and convert to json.
                        //
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
                    result.Value = _codec.Encode(values[0].WrappedValue, out var type,
                        session.MessageContext);
                    result.DataType = type.ToString();
                }
                if (values.Any(v => StatusCode.IsBad(v.StatusCode))) {
                    result.Diagnostics = JToken.FromObject(new {
                        StatusCodes = values.Select(v => v.StatusCode),
                        DiagnosticInfos =
                            (diagnosticInfos != null && diagnosticInfos.Count > 0) ?
                                diagnosticInfos : null
                    });
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
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return _client.ExecuteServiceAsync(endpoint, session => {
                var writeNode = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(writeNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var dataTypeId = request.DataType?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(dataTypeId)) {
                    // Read data type
                    if (!(session.ReadNode(writeNode) is VariableNode variable) ||
                        NodeId.IsNull(variable.DataType)) {
                        throw new ArgumentException(nameof(request.NodeId));
                    }
                    dataTypeId = variable.DataType;
                }
                var builtinType = TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);
                var value = _codec.Decode(request.Value, builtinType,
                    session.MessageContext);
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = writeNode,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(value),
                        IndexRange = request.IndexRange
                    }
                };
                var result = new ValueWriteResultModel();
                session.Write(null, nodesToWrite, out var results, out var diagnosticInfos);
                if (results.Any(StatusCode.IsBad)) {
                    result.Diagnostics = JToken.FromObject(new {
                        StatusCodes = results,
                        DiagnosticInfos =
                            (diagnosticInfos != null && diagnosticInfos.Count > 0) ?
                                diagnosticInfos : null
                    });
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
            return _client.ExecuteServiceAsync(endpoint, session => {
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }

                // Get default input arguments and types
                var response = session.Browse(null, null, methodId, 0,
                    Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HasProperty, true, 0,
                    out var continuationPoint, out var references);

                List<Tuple<TypeInfo, object>> inputs = null, outputs = null;
                foreach (var nodeReference in references) {
                    List<Tuple<TypeInfo, object>> args = null;
                    if (nodeReference.BrowseName == BrowseNames.InputArguments) {
                        args = inputs = new List<Tuple<TypeInfo, object>>();
                    }
                    else if (nodeReference.BrowseName == BrowseNames.OutputArguments) {
                        args = outputs = new List<Tuple<TypeInfo, object>>();
                    }
                    else {
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
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                        var builtInType = TypeInfo.GetBuiltInType(argument.DataType);
                        args.Add(Tuple.Create(new TypeInfo(builtInType,
                            argument.ValueRank), argument.Value));
                    }
                    if (inputs != null && outputs != null) {
                        break;
                    }
                }

                if ((request.Arguments?.Count ?? 0) > (inputs?.Count ?? 0)) {
                    // Too many arguments
                    throw new ArgumentException(nameof(request.Arguments));
                }

                var objectId = request.ObjectId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(objectId)) {
                    objectId = null;
                }

                // Set default input arguments from meta data
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = objectId,
                        MethodId = methodId,
                        InputArguments = new VariantCollection(inputs
                            .Select(arg => arg.Item1.CreateVariant(arg.Item2)))
                    }
                };

                // Update with input arguments provided in request payload
                if (request.Arguments != null) {
                    for (var i = 0; i < request.Arguments.Count; i++) {
                        var arg = request.Arguments[i];
                        if (arg == null) {
                            continue;
                        }
                        var builtinType = inputs[i].Item1.BuiltInType;
                        if (!string.IsNullOrEmpty(arg.DataType)) {
                            builtinType = TypeInfo.GetBuiltInType(
                                arg.DataType.ToNodeId(session.MessageContext),
                                    session.TypeTree);
                        }
                        var value = _codec.Decode(arg.Value, builtinType,
                            session.MessageContext);
                        requests[0].InputArguments[i] = value;
                    }
                }

                // Call method
                var responseHeader = session.Call(null, requests, out var results,
                    out var diagnosticInfos);
                var result = new MethodCallResultModel();

                // Create output argument list
                if (results != null && results.Count > 0) {
                    var args = results[0].OutputArguments?.Count ?? 0;
                    result.Results = new List<MethodCallArgumentModel>(
                        EnumerableEx.Repeat(() => new MethodCallArgumentModel(), args));
                    for (var i = 0; i < args; i++) {
                        var arg = results[0].OutputArguments[i];
                        if (arg == Variant.Null && outputs[i].Item2 != null) {
                            // return default value
                            arg = new Variant(outputs[i].Item2);
                        }
                        result.Results[i].Value = _codec.Encode(arg, out var type,
                            session.MessageContext);
                        if (type == BuiltInType.Null) {
                            // return default type from type info
                            type = outputs[i].Item1.BuiltInType;
                        }
                        result.Results[i].DataType = type.ToString();
                    }
                }
                if (results.Any(v => StatusCode.IsBad(v.StatusCode))) {
                    result.Diagnostics = JToken.FromObject(new {
                        StatusCodes = results.Select(v => v.StatusCode),
                        DiagnosticInfos =
                            (diagnosticInfos != null && diagnosticInfos.Count > 0) ?
                                diagnosticInfos : null
                    });
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Read node properties as node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="nodeId"></param>
        /// <param name="skipValue"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private async Task<NodeModel> ReadNodeModelAsync(Session session,
            NodeId nodeId, bool skipValue, bool? children) {
            var n = await GenericNode.ReadAsync(session, nodeId, skipValue,
                CancellationToken.None);
            return new NodeModel {
                HasChildren = children,
                Id = nodeId.AsString(session.MessageContext),
                DisplayName = n.DisplayName?.ToString(),
                Description = n.Description?.ToString(),
                NodeClass = n.NodeClass.ToServiceType(),
                AccessRestrictions = n.AccessRestrictions == null ||
                    n.AccessRestrictions.Value == 0 ?
                        null : n.AccessRestrictions,
                UserWriteMask =
                    n.UserWriteMask == null || n.UserWriteMask.Value == 0 ?
                        null : n.AccessRestrictions,
                WriteMask = n.WriteMask == null || n.WriteMask.Value == 0 ?
                    null : n.WriteMask,
                DataType = n.DataType?.AsString(session.MessageContext),
                ArrayDimensions = n.ArrayDimensions,
                ValueRank = (NodeValueRank?)n.ValueRank,
                AccessLevel = n.AccessLevelEx == null &&
                    n.AccessLevel == null ? (NodeAccessLevel?)null :
                        (NodeAccessLevel)((n.AccessLevelEx ?? 0) | (n.AccessLevel ?? 0)),
                UserAccessLevel =
                    n.UserAccessLevel == null || n.UserAccessLevel.Value == 0 ?
                        (NodeAccessLevel?)null : (NodeAccessLevel)n.UserAccessLevel,
                Historizing = n.Historizing == null || !n.Historizing.Value ?
                    null : n.Historizing,
                MinimumSamplingInterval = n.MinimumSamplingInterval == null ||
                    (int)n.MinimumSamplingInterval.Value == 0 ?
                        null : n.MinimumSamplingInterval,
                IsAbstract = n.IsAbstract == null || !n.IsAbstract.Value ?
                    null : n.IsAbstract,
                Value = n.Value == null ? null : _codec.Encode(n.Value.Value, out var type,
                    session.MessageContext),
                EventNotifier = n.EventNotifier == null || n.EventNotifier.Value == 0 ?
                    (NodeEventNotifier?)null : (NodeEventNotifier)n.EventNotifier,
                DataTypeDefinition = n.DataTypeDefinition == null ? null :
                    _codec.Encode(new Variant(n.DataTypeDefinition)),
                InverseName = n.InverseName?.ToString(),
                Symmetric = n.Symmetric,
                ContainsNoLoops = n.ContainsNoLoops,
                Executable = n.Executable,
                UserExecutable = n.UserExecutable == null || !n.Executable.Value ?
                    null : n.UserExecutable
            };
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="targetNodesOnly"></param>
        /// <param name="readValues"></param>
        /// <param name="result"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        private async Task<string> AddReferencesToBrowseResult(Session session, bool targetNodesOnly,
            bool readValues, List<NodeReferenceModel> result, byte[] continuationPoint,
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
                                Opc.Ua.BrowseDirection.Forward,
                                ReferenceTypeIds.HierarchicalReferences,
                                true, 0, out var tmp, out var childReferences);
                            children = childReferences.Count != 0;
                        }
                        catch (Exception ex) {
                            _logger.Debug("Failed to obtain hasChildren information", () => ex);
                        }
                        var model = await ReadNodeModelAsync(session, nodeId, !readValues,
                            children);
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
                return continuationPoint.ToBase64String();
            }
            return null;
        }

        private readonly ILogger _logger;
        private readonly IVariantEncoder _codec;
        private readonly IEndpointServices _client;
    }
}
