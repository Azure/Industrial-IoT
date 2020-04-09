// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Opc.Ua.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// This class provides access to a servers address space providing node
    /// and browse services.  It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    public sealed class AddressSpaceServices : INodeServices<EndpointModel>,
        IHistoricAccessServices<EndpointModel>, IBrowseServices<EndpointModel> {

        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public AddressSpaceServices(IEndpointServices client,
            IVariantEncoderFactory codec, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<BrowseResultModel> NodeBrowseFirstAsync(EndpointModel endpoint,
            BrowseRequestModel request) {
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var rootId = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId)) {
                    rootId = ObjectIds.RootFolder;
                }
                var typeId = request.ReferenceTypeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(typeId)) {
                    typeId = ReferenceTypeIds.HierarchicalReferences;
                }
                var view = request.View.ToStackModel(session.MessageContext);
                var excludeReferences = false;
                var rawMode = request.NodeIdsOnly ?? false;
                if (!rawMode) {
                    excludeReferences = request.MaxReferencesToReturn.HasValue &&
                        request.MaxReferencesToReturn.Value == 0;
                }
                var codec = _codec.Create(session.MessageContext);
                var result = new BrowseResultModel();
                var diagnostics = new List<OperationResultModel>();
                if (!excludeReferences) {
                    var direction = (request.Direction ?? Core.Models.BrowseDirection.Forward)
                        .ToStackType();
                    // Browse and read children
                    result.References = new List<NodeReferenceModel>();

                    var response = await session.BrowseAsync(
                        (request.Header?.Diagnostics).ToStackModel(),
                        ViewDescription.IsDefault(view) ? null : view, rootId,
                        request.MaxReferencesToReturn ?? 0u, direction, typeId,
                        !(request?.NoSubtypes ?? false),
                        (uint)request.NodeClassFilter.ToStackMask(), BrowseResultMask.All);

                    OperationResultEx.Validate("Browse_" + rootId,
                        diagnostics, response.Results.Select(r => r.StatusCode),
                        response.DiagnosticInfos, false);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                    result.ContinuationToken = await AddReferencesToBrowseResultAsync(session, codec,
                        (request.Header?.Diagnostics).ToStackModel(), request.TargetNodesOnly ?? false,
                        request.ReadVariableValues ?? false, rawMode, result.References, diagnostics,
                        response.Results[0].ContinuationPoint, response.Results[0].References);
                }
                // Read root node
                result.Node = await ReadNodeModelAsync(session, codec,
                    (request.Header?.Diagnostics).ToStackModel(), rootId, null, true, rawMode,
                    !excludeReferences ? result.References.Count != 0 : (bool?)null, diagnostics, true);
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<BrowseNextResultModel> NodeBrowseNextAsync(
            EndpointModel endpoint, BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var continuationPoint = request.ContinuationToken.DecodeAsBase64();
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                var result = new BrowseNextResultModel {
                    References = new List<NodeReferenceModel>()
                };
                var response = await session.BrowseNextAsync(
                    (request.Header?.Diagnostics).ToStackModel(),
                    request.Abort ?? false, new ByteStringCollection { continuationPoint });
                OperationResultEx.Validate("BrowseNext_" + request.ContinuationToken,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos, false);

                var codec = _codec.Create(session.MessageContext);
                result.ContinuationToken = await AddReferencesToBrowseResultAsync(session, codec,
                    (request.Header?.Diagnostics).ToStackModel(), request.TargetNodesOnly ?? false,
                    request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                    result.References, diagnostics, response.Results[0].ContinuationPoint,
                    response.Results[0].References);
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<BrowsePathResultModel> NodeBrowsePathAsync(
            EndpointModel endpoint, BrowsePathRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var rootId = request?.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId)) {
                    rootId = ObjectIds.RootFolder;
                }
                var diagnostics = new List<OperationResultModel>();
                var result = new BrowsePathResultModel {
                    Targets = new List<NodePathTargetModel>()
                };
                var requests = new BrowsePathCollection(request.BrowsePaths.Select(p =>
                    new BrowsePath {
                        StartingNode = rootId,
                        RelativePath = p.ToRelativePath(session.MessageContext)
                    }));
                var response = await session.TranslateBrowsePathsToNodeIdsAsync(
                    (request.Header?.Diagnostics).ToStackModel(), requests);
                OperationResultEx.Validate("Translate" + request.NodeId,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos, requests, false);
                var codec = _codec.Create(session.MessageContext);
                for (var index = 0; index < response.Results.Count; index++) {
                    await AddTargetsToBrowseResultAsync(session, codec,
                        (request.Header?.Diagnostics).ToStackModel(),
                        request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                        result.Targets, diagnostics, response.Results[index].Targets,
                        request.BrowsePaths[index]);
                }
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointModel endpoint, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId) &&
                (request.MethodBrowsePath == null || request.MethodBrowsePath.Length == 0)) {
                throw new ArgumentException(nameof(request.MethodId));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                var methodId = request.MethodId.ToNodeId(session.MessageContext);
                if (request.MethodBrowsePath != null && request.MethodBrowsePath.Length > 0) {
                    methodId = await ResolveBrowsePathToNodeAsync(session, methodId,
                        nameof(request.MethodBrowsePath), request.MethodBrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }

                var codec = _codec.Create(session.MessageContext);
                var response = await session.BrowseAsync(
                    (request.Header?.Diagnostics).ToStackModel(), null, methodId, 0,
                    Opc.Ua.BrowseDirection.Both, ReferenceTypeIds.Aggregates,
                    true, 0, BrowseResultMask.All);
                OperationResultEx.Validate("Browse_" + methodId, diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos, false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                var continuationPoint = response.Results[0].ContinuationPoint;
                var references = response.Results[0].References;

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
                    var value = await RawNodeModel.ReadValueAsync(session,
                        (request.Header?.Diagnostics).ToStackModel(), node, diagnostics, false);
                    if (!(value?.Value is ExtensionObject[] argumentsList)) {
                        continue;
                    }

                    var argList = new List<MethodMetadataArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                        var dataTypeIdNode = await ReadNodeModelAsync(session, codec,
                            (request.Header?.Diagnostics).ToStackModel(), argument.DataType, null,
                            false, false, false, diagnostics, false);
                        var arg = new MethodMetadataArgumentModel {
                            Name = argument.Name,
                            DefaultValue = argument.Value == null ? VariantValue.Null :
                                codec.Encode(new Variant(argument.Value), out var type),
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
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<MethodCallResultModel> NodeMethodCallAsync(EndpointModel endpoint,
            MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ObjectId) &&
                (request.ObjectBrowsePath == null || request.ObjectBrowsePath.Length == 0)) {
                throw new ArgumentException(nameof(request.ObjectId));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                //
                // A method call request can specify the targets in several ways:
                //
                // * Specify methodId and optionally objectId node ids with null browse paths.
                // * Specify an objectBrowsePath to a real object node from the node specified
                //   with objectId.  If objectId is null, the root node is used.
                // * Specify a methodBrowsePath from the above object node to the actual
                //   method node to call on the object. MethodId remains null.
                // * Like previously, but specify methodId and method browse path from it to a
                //   real method node.
                //
                var objectId = request.ObjectId.ToNodeId(session.MessageContext);
                if (request.ObjectBrowsePath != null && request.ObjectBrowsePath.Length > 0) {
                    objectId = await ResolveBrowsePathToNodeAsync(session, objectId,
                        nameof(request.ObjectBrowsePath), request.ObjectBrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                if (NodeId.IsNull(objectId)) {
                    throw new ArgumentException(nameof(request.ObjectId));
                }

                var methodId = request.MethodId.ToNodeId(session.MessageContext);
                if (request.MethodBrowsePath != null && request.MethodBrowsePath.Length > 0) {
                    if (NodeId.IsNull(methodId)) {
                        // Browse from object id to method if possible
                        methodId = objectId ?? throw new ArgumentException(nameof(request.MethodId));
                    }
                    methodId = await ResolveBrowsePathToNodeAsync(session, methodId,
                        nameof(request.MethodBrowsePath), request.MethodBrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                else if (NodeId.IsNull(methodId)) {
                    // Method is null and cannot browse to method from object
                    throw new ArgumentException(nameof(request.MethodId));
                }

                // Get default input arguments and types
                var browse = await session.BrowseAsync(
                    (request.Header?.Diagnostics).ToStackModel(), null, methodId,
                    0, Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HasProperty,
                    true, 0, BrowseResultMask.All);
                OperationResultEx.Validate("Browse_" + methodId,
                    diagnostics, browse.Results.Select(r => r.StatusCode),
                    browse.DiagnosticInfos, false);
                SessionClientEx.Validate(browse.Results, browse.DiagnosticInfos);

                List<Tuple<TypeInfo, object>> inputs = null, outputs = null;
                foreach (var nodeReference in browse.Results[0].References) {
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
                    if (session.ReadNode(node) is VariableNode argumentsNode) {
                        var value = session.ReadValue(argumentsNode.NodeId);
                        if (value.Value is ExtensionObject[] argumentsList) {
                            foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                                var builtInType = TypeInfo.GetBuiltInType(argument.DataType);
                                args.Add(Tuple.Create(new TypeInfo(builtInType,
                                    argument.ValueRank), argument.Value));
                            }
                            if (inputs != null && outputs != null) {
                                break;
                            }
                        }
                    }
                }

                if ((request.Arguments?.Count ?? 0) > (inputs?.Count ?? 0)) {
                    // Too many arguments
                    throw new ArgumentException(nameof(request.Arguments));
                }

                var codec = _codec.Create(session.MessageContext);

                // Set default input arguments from meta data
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = objectId,
                        MethodId = methodId,
                        InputArguments = inputs == null ? new VariantCollection() :
                            new VariantCollection(inputs
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
                        var value = codec.Decode(arg.Value, builtinType);
                        requests[0].InputArguments[i] = value;
                    }
                }

                // Call method
                var response = await session.CallAsync(
                    (request.Header?.Diagnostics).ToStackModel(), requests);
                OperationResultEx.Validate("Call" + methodId, diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos,
                    false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                var results = response.Results;
                var result = new MethodCallResultModel();
                // Create output argument list
                if (results != null && results.Count > 0) {
                    var args = results[0].OutputArguments?.Count ?? 0;
                    result.Results = new List<MethodCallArgumentModel>(
                        LinqEx.Repeat(() => new MethodCallArgumentModel(), args));
                    for (var i = 0; i < args; i++) {
                        var arg = results[0].OutputArguments[i];
                        if (arg == Variant.Null &&
                            (outputs?.Count ?? 0) > i && outputs[i].Item2 != null) {
                            // return default value
                            arg = new Variant(outputs[i].Item2);
                        }
                        result.Results[i].Value = codec.Encode(arg, out var type);
                        if (type == BuiltInType.Null && (outputs?.Count ?? 0) > i) {
                            // return default type from type info
                            type = outputs[i].Item1.BuiltInType;
                        }
                        result.Results[i].DataType = type == BuiltInType.Null ?
                            null : type.ToString();
                    }
                }
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<ValueReadResultModel> NodeValueReadAsync(EndpointModel endpoint,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Length == 0)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                var readNode = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath != null && request.BrowsePath.Length > 0) {
                    readNode = await ResolveBrowsePathToNodeAsync(session, readNode,
                        nameof(request.BrowsePath), request.BrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                if (NodeId.IsNull(readNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var response = await session.ReadAsync((request.Header?.Diagnostics).ToStackModel(),
                    0, TimestampsToReturn.Both, new ReadValueIdCollection {
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
                });
                OperationResultEx.Validate("ReadValue_" + readNode, diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos, false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                var values = response.Results;
                var result = new ValueReadResultModel();
                var codec = _codec.Create(session.MessageContext);
                if (values != null && values.Count > 0 && values[0] != null) {
                    result.ServerPicoseconds = values[0].ServerPicoseconds != 0 ?
                        values[0].ServerPicoseconds : (ushort?)null;
                    result.ServerTimestamp = values[0].ServerTimestamp != DateTime.MinValue ?
                        values[0].ServerTimestamp : (DateTime?)null;
                    result.SourcePicoseconds = values[0].SourcePicoseconds != 0 ?
                        values[0].SourcePicoseconds : (ushort?)null;
                    result.SourceTimestamp = values[0].SourceTimestamp != DateTime.MinValue ?
                        values[0].SourceTimestamp : (DateTime?)null;
                    result.Value = codec.Encode(values[0].WrappedValue, out var type);
                    result.DataType = type == BuiltInType.Null ? null : type.ToString();
                }
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<ValueWriteResultModel> NodeValueWriteAsync(EndpointModel endpoint,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Length == 0)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                var writeNode = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath != null && request.BrowsePath.Length > 0) {
                    writeNode = await ResolveBrowsePathToNodeAsync(session, writeNode,
                        nameof(request.BrowsePath), request.BrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                if (NodeId.IsNull(writeNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var dataTypeId = request.DataType.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(dataTypeId)) {
                    // Read data type
                    // TODO Async
                    if (!(session.ReadNode(writeNode) is VariableNode variable) ||
                        NodeId.IsNull(variable.DataType)) {
                        throw new ArgumentException(nameof(request.NodeId));
                    }
                    dataTypeId = variable.DataType;
                }
                var codec = _codec.Create(session.MessageContext);
                var builtinType = TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);
                var value = codec.Decode(request.Value, builtinType);
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = writeNode,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(value),
                        IndexRange = request.IndexRange
                    }
                };
                var result = new ValueWriteResultModel();
                var response = await session.WriteAsync(
                    (request.Header?.Diagnostics).ToStackModel(), nodesToWrite);
                OperationResultEx.Validate("WriteValue_" + writeNode, diagnostics, response.Results,
                    response.DiagnosticInfos, false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                result.ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics);
                return result;
            });
        }

        /// <inheritdoc/>
        public Task<ReadResultModel> NodeReadAsync(EndpointModel endpoint,
            ReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation,
                async session => {
                    var codec = _codec.Create(session.MessageContext);
                    var requests = new ReadValueIdCollection(request.Attributes
                        .Select(a => new ReadValueId {
                            AttributeId = (uint)a.Attribute,
                            NodeId = a.NodeId.ToNodeId(session.MessageContext)
                        }));
                    var response = await session.ReadAsync(
                        (request.Header?.Diagnostics).ToStackModel(), 0, TimestampsToReturn.Both,
                        requests);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos, requests);
                    return new ReadResultModel {
                        Results = response.Results
                            .Select((value, index) => {
                                var diagnostics = response.DiagnosticInfos == null ||
                                            response.DiagnosticInfos.Count == 0 ? null :
                                    response.DiagnosticInfos[index];
                                return new AttributeReadResultModel {
                                    Value = codec.Encode(value.WrappedValue, out var wellKnown),
                                    ErrorInfo = codec.Encode(diagnostics,
                                        value.StatusCode, "NodeRead", request.Header?.Diagnostics)
                                };
                            }).ToList()
                    };
                });
        }

        /// <inheritdoc/>
        public Task<WriteResultModel> NodeWriteAsync(EndpointModel endpoint,
            WriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation,
                async session => {
                    var codec = _codec.Create(session.MessageContext);
                    var requests = new WriteValueCollection(request.Attributes
                        .Select(a => new WriteValue {
                            AttributeId = (uint)a.Attribute,
                            NodeId = a.NodeId.ToNodeId(session.MessageContext),
                            Value = new DataValue(codec.Decode(a.Value,
                                AttributeMap.GetBuiltInType((uint)a.Attribute)))
                        }));
                    var response = await session.WriteAsync(
                        (request.Header?.Diagnostics).ToStackModel(), requests);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos, requests);
                    return new WriteResultModel {
                        Results = response.Results
                            .Select((value, index) => {
                                var diagnostics = response.DiagnosticInfos == null ||
                                            response.DiagnosticInfos.Count == 0 ? null :
                                    response.DiagnosticInfos[index];
                                return new AttributeWriteResultModel {
                                    ErrorInfo = codec.Encode(diagnostics,
                                        value, "NodeWrite", request.Header?.Diagnostics)
                                };
                            }).ToList()
                    };
                });
        }

        /// <inheritdoc/>
        public Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(EndpointModel endpoint,
            HistoryReadRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Length == 0)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var diagnostics = new List<OperationResultModel>();
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath != null && request.BrowsePath.Length > 0) {
                    nodeId = await ResolveBrowsePathToNodeAsync(session, nodeId,
                        nameof(request.BrowsePath), request.BrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                if (NodeId.IsNull(nodeId)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var codec = _codec.Create(session.MessageContext);
                var details = codec.Decode(request.Details, BuiltInType.ExtensionObject);
                if (!(details.Value is ExtensionObject readDetails)) {
                    throw new ArgumentNullException(nameof(request.Details));
                }
                var response = await session.HistoryReadAsync(
                    (request.Header?.Diagnostics).ToStackModel(), readDetails,
                    TimestampsToReturn.Both, false, new HistoryReadValueIdCollection {
                        new HistoryReadValueId {
                            IndexRange = request.IndexRange,
                            NodeId = nodeId,
                            DataEncoding = null // TODO
                        }
                    });
                OperationResultEx.Validate("HistoryRead_" + nodeId,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos, false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                return new HistoryReadResultModel<VariantValue> {
                    ContinuationToken = response.Results[0].ContinuationPoint.ToBase64String(),
                    History = codec.Encode(new Variant(response.Results[0].HistoryData), out var tmp),
                    ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics)
                };
            });
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(EndpointModel endpoint,
            HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation,
                async session => {
                    var codec = _codec.Create(session.MessageContext);
                    var diagnostics = new List<OperationResultModel>();
                    var response = await session.HistoryReadAsync(
                        (request.Header?.Diagnostics).ToStackModel(), null, TimestampsToReturn.Both,
                        request.Abort ?? false, new HistoryReadValueIdCollection {
                        new HistoryReadValueId {
                            ContinuationPoint = request.ContinuationToken.DecodeAsBase64(),
                            DataEncoding = null // TODO
                        }
                    });
                    OperationResultEx.Validate("HistoryReadNext_" + request.ContinuationToken,
                        diagnostics, response.Results.Select(r => r.StatusCode),
                        response.DiagnosticInfos, false);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                    return new HistoryReadNextResultModel<VariantValue> {
                        ContinuationToken = response.Results[0].ContinuationPoint.ToBase64String(),
                        History = codec.Encode(new Variant(response.Results[0].HistoryData),
                            out var tmp),
                        ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics)
                    };
                });
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryUpdateAsync(EndpointModel endpoint,
            HistoryUpdateRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            return _client.ExecuteServiceAsync(endpoint, request.Header?.Elevation, async session => {
                var codec = _codec.Create(session.MessageContext);
                var diagnostics = new List<OperationResultModel>();
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath != null && request.BrowsePath.Length > 0) {
                    nodeId = await ResolveBrowsePathToNodeAsync(session, nodeId,
                        nameof(request.BrowsePath), request.BrowsePath,
                        request.Header?.Diagnostics, diagnostics);
                }
                var details = codec.Decode(request.Details, BuiltInType.ExtensionObject);
                if (!(details.Value is ExtensionObject extensionObject)) {
                    throw new ArgumentNullException(nameof(request.Details));
                }
                if (extensionObject.Body is HistoryUpdateDetails updateDetails) {
                    // Update the node id to target based on the request
                    if (!NodeId.IsNull(nodeId)) {
                        updateDetails.NodeId = nodeId;
                    }
                    if (NodeId.IsNull(updateDetails.NodeId)) {
                        throw new ArgumentNullException(nameof(request.NodeId));
                    }
                }
                var response = await session.HistoryUpdateAsync(
                    (request.Header?.Diagnostics).ToStackModel(),
                    new ExtensionObjectCollection { extensionObject });
                OperationResultEx.Validate("HistoryUpdate",
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos, false);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                return new HistoryUpdateResultModel {
                    Results = response.Results[0].OperationResults.Select(s => new ServiceResultModel {
                        StatusCode = s.CodeBits,
                        ErrorMessage = StatusCode.LookupSymbolicId(s.CodeBits),
                        Diagnostics = null
                    }).ToList(),
                    ErrorInfo = codec.Encode(diagnostics, request.Header?.Diagnostics)
                };
            });
        }

        /// <summary>
        /// Read node properties as node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="nodeClass"></param>
        /// <param name="skipValue"></param>
        /// <param name="rawMode"></param>
        /// <param name="children"></param>
        /// <param name="diagnostics"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        private async Task<NodeModel> ReadNodeModelAsync(Session session, IVariantEncoder codec,
            RequestHeader header, NodeId nodeId, Opc.Ua.NodeClass? nodeClass, bool skipValue,
            bool rawMode, bool? children, List<OperationResultModel> diagnostics, bool traceOnly) {
            var id = nodeId.AsString(session.MessageContext);
            if (rawMode) {
                return new NodeModel { NodeId = id, NodeClass = nodeClass?.ToServiceType() };
            }
            var node = await RawNodeModel.ReadAsync(session, header, nodeId, skipValue,
                diagnostics, traceOnly);
            var value = node.DataValue;
            return new NodeModel {
                Children = children,
                NodeId = id,
                BrowseName = node.BrowseName.AsString(session.MessageContext),
                DisplayName = node.DisplayName?.ToString(),
                Description = node.Description?.ToString(),
                NodeClass = node.NodeClass.ToServiceType() ?? nodeClass?.ToServiceType(),
                AccessRestrictions = node.AccessRestrictions == null || node.AccessRestrictions == 0 ?
                    (NodeAccessRestrictions?)null : (NodeAccessRestrictions)node.AccessRestrictions,
                UserWriteMask = node.UserWriteMask,
                WriteMask = node.WriteMask,
                DataType = node.DataType.AsString(session.MessageContext),
                ArrayDimensions = node.ArrayDimensions,
                ValueRank = (NodeValueRank?)node.ValueRank,
                AccessLevel = node.AccessLevelEx == null &&
                    node.AccessLevel == null ? (NodeAccessLevel?)null :
                        (NodeAccessLevel)((node.AccessLevelEx ?? 0) | (node.AccessLevel ?? 0)),
                UserAccessLevel =
                    node.UserAccessLevel == null ?
                        (NodeAccessLevel?)null : (NodeAccessLevel)node.UserAccessLevel,
                Historizing = node.Historizing,
                MinimumSamplingInterval = node.MinimumSamplingInterval,
                IsAbstract = node.IsAbstract,
                Value = codec.Encode(value?.WrappedValue, out var type),
                SourceTimestamp = value?.SourceTimestamp,
                SourcePicoseconds = value?.SourcePicoseconds,
                ServerTimestamp = value?.ServerTimestamp,
                ServerPicoseconds = value?.ServerPicoseconds,
                ErrorInfo = codec.Encode(value?.StatusCode),
                TypeDefinitionId = node.TypeDefinitionId.AsString(session.MessageContext),
                EventNotifier = node.EventNotifier == null || node.EventNotifier == 0x0 ?
                    (NodeEventNotifier?)null : (NodeEventNotifier)node.EventNotifier,
                DataTypeDefinition = node.DataTypeDefinition == null ? null :
                    codec.Encode(new Variant(node.DataTypeDefinition)),
                InverseName = node.InverseName?.ToString(),
                Symmetric = node.Symmetric,
                ContainsNoLoops = node.ContainsNoLoops,
                Executable = node.Executable,
                UserExecutable = node.UserExecutable,
                UserRolePermissions = node.UserRolePermissions?
                    .Select(p => p.ToServiceModel(session.MessageContext)).ToList(),
                RolePermissions = node.RolePermissions?
                    .Select(p => p.ToServiceModel(session.MessageContext)).ToList()
            };
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <param name="header"></param>
        /// <param name="targetNodesOnly"></param>
        /// <param name="readValues"></param>
        /// <param name="rawMode"></param>
        /// <param name="result"></param>
        /// <param name="diagnostics"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        private async Task<string> AddReferencesToBrowseResultAsync(Session session,
            IVariantEncoder codec, RequestHeader header, bool targetNodesOnly, bool readValues,
            bool rawMode, List<NodeReferenceModel> result, List<OperationResultModel> diagnostics,
            byte[] continuationPoint, List<ReferenceDescription> references) {
            if (references == null) {
                return null;
            }
            foreach (var reference in references) {
                try {
                    var nodeId = reference.NodeId.ToNodeId(session.NamespaceUris);
                    var id = nodeId.AsString(session.MessageContext);
                    if (targetNodesOnly && result.Any(r => r.Target.NodeId == id)) {
                        continue;
                    }
                    bool? children = null;
                    if (!rawMode) {
                        // Check for children
                        try {
                            var response = await session.BrowseAsync(header, null,
                                nodeId, 1, Opc.Ua.BrowseDirection.Forward,
                                ReferenceTypeIds.HierarchicalReferences,
                                true, 0, BrowseResultMask.All);
                            OperationResultEx.Validate("FetchChildren_" + nodeId,
                                diagnostics, response?.Results?.Select(r => r.StatusCode),
                                response?.DiagnosticInfos, true);
                            if (response.Results.Count > 0) {
                                children = response.Results[0].References.Count != 0;
                                if (response.Results[0].ContinuationPoint != null) {
                                    await session.BrowseNextAsync(header, true,
                                        new ByteStringCollection {
                                            response.Results[0].ContinuationPoint
                                        });
                                }
                            }
                        }
                        catch (Exception ex) {
                            _logger.Information(ex, "Failed to obtain child information");
                        }
                    }
                    var model = await ReadNodeModelAsync(session, codec, header, nodeId,
                        reference.NodeClass, !readValues, rawMode, children, diagnostics,
                        true);
                    if (rawMode) {
                        model.BrowseName = reference.BrowseName.AsString(
                            session.MessageContext);
                        model.DisplayName = reference.DisplayName?.ToString();
                    }
                    model.TypeDefinitionId = reference.TypeDefinition.AsString(
                        session.MessageContext);
                    if (targetNodesOnly) {
                        result.Add(new NodeReferenceModel { Target = model });
                        continue;
                    }
                    result.Add(new NodeReferenceModel {
                        ReferenceTypeId = reference.ReferenceTypeId.AsString(
                            session.MessageContext),
                        Direction = reference.IsForward ?
                            Core.Models.BrowseDirection.Forward :
                            Core.Models.BrowseDirection.Backward,
                        Target = model
                    });
                }
                catch {
                    // TODO: Add diagnostics result for diagnostics.
                    continue;
                }
            }
            return continuationPoint.ToBase64String();
        }

        /// <summary>
        /// Add targets
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <param name="header"></param>
        /// <param name="readValues"></param>
        /// <param name="rawMode"></param>
        /// <param name="result"></param>
        /// <param name="diagnostics"></param>
        /// <param name="targets"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task AddTargetsToBrowseResultAsync(Session session, IVariantEncoder codec,
            RequestHeader header, bool readValues, bool rawMode, List<NodePathTargetModel> result,
            List<OperationResultModel> diagnostics, BrowsePathTargetCollection targets,
            string[] path) {
            if (targets != null) {
                foreach (var target in targets) {
                    try {
                        var nodeId = target.TargetId.ToNodeId(session.NamespaceUris);
                        var model = await ReadNodeModelAsync(session, codec, header, nodeId,
                            null, !readValues, rawMode, false, diagnostics, true);
                        result.Add(new NodePathTargetModel {
                            BrowsePath = path,
                            Target = model,
                            RemainingPathIndex = target.RemainingPathIndex == 0 ?
                                (int?)null : (int)target.RemainingPathIndex
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

        /// <summary>
        /// Resolve provided path to node.
        /// </summary>
        /// <param name="rootId"></param>
        /// <param name="session"></param>
        /// <param name="paramName"></param>
        /// <param name="paths"></param>
        /// <param name="diagnostics"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        private async Task<NodeId> ResolveBrowsePathToNodeAsync(Session session,
            NodeId rootId, string paramName, string[] paths, DiagnosticsModel diagnostics,
            List<OperationResultModel> operations) {
            if (paths == null || paths.Length == 0) {
                return rootId;
            }
            if (NodeId.IsNull(rootId)) {
                rootId = ObjectIds.RootFolder;
            }
            var result = new BrowsePathResultModel {
                Targets = new List<NodePathTargetModel>()
            };
            var response = await session.TranslateBrowsePathsToNodeIdsAsync(
                diagnostics.ToStackModel(), new BrowsePathCollection {
                    new BrowsePath {
                        StartingNode = rootId,
                        RelativePath = paths.ToRelativePath(session.MessageContext)
                    }
                });
            OperationResultEx.Validate($"Resolve_" + paramName, operations,
                response.Results.Select(r => r.StatusCode), response.DiagnosticInfos,
                false);
            var count = response.Results[0].Targets?.Count ?? 0;
            if (count == 0) {
                throw new ResourceNotFoundException(
                    $"{paramName} did not resolve to any node.");
            }
            if (count != 1) {
                throw new ConflictingResourceException(
                    $"{paramName} resolved to {count} nodes.");
            }
            return response.Results[0].Targets[0].TargetId
                .ToNodeId(session.NamespaceUris);
        }

        private readonly ILogger _logger;
        private readonly IVariantEncoderFactory _codec;
        private readonly IEndpointServices _client;
    }
}
