// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
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

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(EndpointModel endpoint,
            BrowseRequestModel request) {
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var rootId = request.NodeId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId)) {
                    rootId = ObjectIds.RootFolder;
                }
                var typeId = request.ReferenceTypeId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(typeId)) {
                    typeId = ReferenceTypeIds.HierarchicalReferences;
                }
                var view = request.View.ToStackModel(session.MessageContext);
                var excludeReferences = request.MaxReferencesToReturn.HasValue &&
                    request.MaxReferencesToReturn.Value == 0;
                var result = new BrowseResultModel();
                var diagnostics = new List<OperationResult>();
                if (!excludeReferences) {
                    var direction = (request.Direction ?? OpcUa.Twin.Models.BrowseDirection.Forward)
                        .ToStackType();
                    // Browse and read children
                    result.References = new List<NodeReferenceModel>();

                    var response = await session.BrowseAsync(request.Diagnostics.ToStackModel(),
                        ViewDescription.IsDefault(view) ? null : view, rootId,
                        request.MaxReferencesToReturn ?? 0u, direction, typeId,
                        !(request?.NoSubtypes ?? false),
                        (uint)request.NodeClassFilter.ToStackMask(), BrowseResultMask.All);

                    OperationResultEx.Validate("Browse_" + rootId,
                        diagnostics, response.Results.Select(r => r.StatusCode),
                        response.DiagnosticInfos);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                    result.ContinuationToken = await AddReferencesToBrowseResult(session,
                        request.Diagnostics.ToStackModel(), request.TargetNodesOnly ?? false,
                        request.ReadVariableValues ?? false, result.References, diagnostics,
                        response.Results[0].ContinuationPoint, response.Results[0].References);
                }
                // Read root node
                result.Node = await ReadNodeModelAsync(session, request.Diagnostics.ToStackModel(),
                    rootId, true, !excludeReferences ? result.References.Count != 0 : (bool?)null,
                    diagnostics);
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            EndpointModel endpoint, BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var continuationPoint = request.ContinuationToken.DecodeAsBase64();
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var diagnostics = new List<OperationResult>();
                var result = new BrowseNextResultModel {
                    References = new List<NodeReferenceModel>()
                };
                var response = await session.BrowseNextAsync(request.Diagnostics.ToStackModel(),
                    request.Abort ?? false, new ByteStringCollection { continuationPoint });
                OperationResultEx.Validate("BrowseNext_" + request.ContinuationToken,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos);

                result.ContinuationToken = await AddReferencesToBrowseResult(session,
                    request.Diagnostics.ToStackModel(), request.TargetNodesOnly ?? false,
                    request.ReadVariableValues ?? false, result.References, diagnostics,
                    response.Results[0].ContinuationPoint, response.Results[0].References);
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            EndpointModel endpoint, BrowsePathRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.PathElements == null || request.PathElements.Length == 0) {
                throw new ArgumentNullException(nameof(request.PathElements));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var rootId = request?.NodeId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId)) {
                    rootId = ObjectIds.RootFolder;
                }
                var diagnostics = new List<OperationResult>();
                var result = new BrowsePathResultModel {
                    Targets = new List<NodePathTargetModel>()
                };
                var response = await session.TranslateBrowsePathsToNodeIdsAsync(
                    request.Diagnostics.ToStackModel(),
                    new BrowsePathCollection {
                        new BrowsePath {
                            StartingNode = rootId,
                            RelativePath = request.PathElements
                                .ToRelativePath(session.MessageContext)
                        }
                    });
                OperationResultEx.Validate("Translate" + request.NodeId,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos);

                await AddTargetsToBrowseResult(session,
                    request.Diagnostics.ToStackModel(),
                    request.ReadVariableValues ?? false, result.Targets, diagnostics,
                    response.Results[0].Targets);

                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointModel endpoint, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentException(nameof(request.MethodId));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var diagnostics = new List<OperationResult>();

                var response = await session.BrowseAsync(
                    request.Diagnostics.ToStackModel(), null, methodId, 0,
                    Opc.Ua.BrowseDirection.Both, ReferenceTypeIds.Aggregates,
                    true, 0, BrowseResultMask.All);
                OperationResultEx.Validate("Browse_" + methodId, diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
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
                    var value = await GenericNode.ReadValueAsync(session,
                        request.Diagnostics.ToStackModel(), node, diagnostics);
                    if (!(value?.Value is ExtensionObject[] argumentsList)) {
                        continue;
                    }

                    var argList = new List<MethodMetadataArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body)) {
                        var dataTypeIdNode = await ReadNodeModelAsync(session,
                            request.Diagnostics.ToStackModel(), argument.DataType,
                            false, false, diagnostics);
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
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(EndpointModel endpoint,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var readNode = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(readNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var diagnostics = new List<OperationResult>();
                var response = await session.ReadAsync(request.Diagnostics.ToStackModel(),
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
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                var values = response.Results;
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
                    result.DataType = type == BuiltInType.Null ? null : type.ToString();
                }
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(EndpointModel endpoint,
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
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var writeNode = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(writeNode)) {
                    throw new ArgumentException(nameof(request.NodeId));
                }
                var diagnostics = new List<OperationResult>();

                var dataTypeId = request.DataType?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(dataTypeId)) {
                    // Read data type
                    // TODO Async
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
                var response = await session.WriteAsync(request.Diagnostics.ToStackModel(),
                    nodesToWrite);
                OperationResultEx.Validate("WriteValue_" + writeNode, diagnostics, response.Results,
                    response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(EndpointModel endpoint,
            MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var methodId = request.MethodId?.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(methodId)) {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var diagnostics = new List<OperationResult>();

                // Get default input arguments and types
                var browse = await session.BrowseAsync(request.Diagnostics.ToStackModel(),
                    null, methodId, 0, Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeIds.HasProperty, true, 0, BrowseResultMask.All);
                OperationResultEx.Validate("Browse_" + methodId,
                    diagnostics, browse.Results.Select(r => r.StatusCode),
                    browse.DiagnosticInfos);
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
                        InputArguments = inputs == null ? null :
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
                        var value = _codec.Decode(arg.Value, builtinType,
                            session.MessageContext);
                        requests[0].InputArguments[i] = value;
                    }
                }

                // Call method
                var response = await session.CallAsync(request.Diagnostics.ToStackModel(),
                    requests);
                OperationResultEx.Validate("Call" + methodId, diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                var results = response.Results;
                var result = new MethodCallResultModel();
                // Create output argument list
                if (results != null && results.Count > 0) {
                    var args = results[0].OutputArguments?.Count ?? 0;
                    result.Results = new List<MethodCallArgumentModel>(
                        EnumerableEx.Repeat(() => new MethodCallArgumentModel(), args));
                    for (var i = 0; i < args; i++) {
                        var arg = results[0].OutputArguments[i];
                        if (arg == Variant.Null &&
                            (outputs?.Count ?? 0) > i && outputs[i].Item2 != null) {
                            // return default value
                            arg = new Variant(outputs[i].Item2);
                        }
                        result.Results[i].Value = _codec.Encode(arg, out var type,
                            session.MessageContext);
                        if (type == BuiltInType.Null && (outputs?.Count ?? 0) > i) {
                            // return default type from type info
                            type = outputs[i].Item1.BuiltInType;
                        }
                        result.Results[i].DataType = type == BuiltInType.Null ?
                            null : type.ToString();
                    }
                }
                result.ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                    session.MessageContext);
                return result;
            });
        }

        /// <inheritdoc/>
        public async Task<BatchReadResultModel> NodeBatchReadAsync(EndpointModel endpoint,
            BatchReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var requests = new ReadValueIdCollection(request.Attributes
                    .Select(a => new ReadValueId {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId?.ToNodeId(session.MessageContext)
                    }));
                var response = await session.ReadAsync(request.Diagnostics.ToStackModel(),
                    0, TimestampsToReturn.Both, requests);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos, requests);
                return new BatchReadResultModel {
                    Results = response.Results
                        .Select((value, index) => {
                            var diagnostics = response.DiagnosticInfos == null ||
                                        response.DiagnosticInfos.Count == 0 ? null :
                                response.DiagnosticInfos[index];
                            return new AttributeReadResultModel {
                                Value = _codec.Encode(value.WrappedValue, out var wellKnown,
                                    session.MessageContext),
                                ErrorInfo = diagnostics.ToServiceModel(
                                    value.StatusCode, "BatchRead", request.Diagnostics,
                                    session.MessageContext)
                            };
                        }).ToList()
                };
            });
        }

        /// <inheritdoc/>
        public async Task<BatchWriteResultModel> NodeBatchWriteAsync(EndpointModel endpoint,
            BatchWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var requests = new WriteValueCollection(request.Attributes
                    .Select(a => new WriteValue {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId?.ToNodeId(session.MessageContext),
                        Value = new DataValue(_codec.Decode(a.Value,
                            Attributes.GetBuiltInType((uint)a.Attribute),
                            session.MessageContext))
                    }));
                var response = await session.WriteAsync(request.Diagnostics.ToStackModel(),
                    requests);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos, requests);
                return new BatchWriteResultModel {
                    Results = response.Results
                        .Select((value, index) => {
                            var diagnostics = response.DiagnosticInfos == null ||
                                        response.DiagnosticInfos.Count == 0 ? null :
                                response.DiagnosticInfos[index];
                            return new AttributeWriteResultModel {
                                ErrorInfo = diagnostics.ToServiceModel(
                                    value, "BatchWrite", request.Diagnostics,
                                    session.MessageContext)
                            };
                        }).ToList()
                };
            });
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel> NodeHistoryReadAsync(EndpointModel endpoint,
            HistoryReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }
            if (request.Request == null) {
                throw new ArgumentNullException(nameof(request.Request));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                var details = _codec.Decode(request.Request,
                    BuiltInType.ExtensionObject, session.MessageContext);
                if (!(details.Value is ExtensionObject readDetails)) {
                    throw new ArgumentNullException(nameof(request.Request));
                }
                var diagnostics = new List<OperationResult>();
                var response = await session.HistoryReadAsync(request.Diagnostics.ToStackModel(),
                    readDetails, TimestampsToReturn.Both,
                    false, new HistoryReadValueIdCollection {
                        new HistoryReadValueId {
                            IndexRange = request.IndexRange,
                            NodeId = nodeId,
                            DataEncoding = null // TODO
                        }
                    });
                OperationResultEx.Validate("HistoryRead_" + nodeId,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                return new HistoryReadResultModel {
                    ContinuationToken = response.Results[0].ContinuationPoint.ToBase64String(),
                    History = _codec.Encode(new Variant(response.Results[0].HistoryData),
                        out var tmp, session.MessageContext),
                    ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                        session.MessageContext)
                };
            });
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel> NodeHistoryReadNextAsync(EndpointModel endpoint,
            HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var diagnostics = new List<OperationResult>();
                var response = await session.HistoryReadAsync(request.Diagnostics.ToStackModel(),
                    null, TimestampsToReturn.Both, request.Abort ?? false,
                    new HistoryReadValueIdCollection {
                        new HistoryReadValueId {
                            ContinuationPoint = request.ContinuationToken?.DecodeAsBase64(),
                            DataEncoding = null // TODO
                        }
                    });
                OperationResultEx.Validate("HistoryReadNext_" + request.ContinuationToken,
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                return new HistoryReadNextResultModel {
                    ContinuationToken = response.Results[0].ContinuationPoint.ToBase64String(),
                    History = _codec.Encode(new Variant(response.Results[0].HistoryData),
                        out var tmp, session.MessageContext),
                    ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                        session.MessageContext)
                };
            });
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> NodeHistoryUpdateAsync(EndpointModel endpoint,
            HistoryUpdateRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Request == null) {
                throw new ArgumentNullException(nameof(request.Request));
            }
            return await _client.ExecuteServiceAsync(endpoint, request.Elevation, async session => {
                var diagnostics = new List<OperationResult>();
                var details = _codec.Decode(request.Request,
                    BuiltInType.ExtensionObject, session.MessageContext);
                if (!(details.Value is ExtensionObject updateDetails)) {
                    throw new ArgumentNullException(nameof(request.Request));
                }
                var response = await session.HistoryUpdateAsync(request.Diagnostics.ToStackModel(),
                    new ExtensionObjectCollection { updateDetails });
                OperationResultEx.Validate("HistoryUpdate",
                    diagnostics, response.Results.Select(r => r.StatusCode),
                    response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);

                return new HistoryUpdateResultModel {
                    Results = response.Results[0].OperationResults.Select(s => new ServiceResultModel {
                        StatusCode = s.CodeBits,
                        ErrorMessage = StatusCode.LookupSymbolicId(s.CodeBits),
                        Diagnostics = null
                    }).ToList(),
                    ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                        session.MessageContext)
                };
            });
        }

        /// <summary>
        /// Read node properties as node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="skipValue"></param>
        /// <param name="children"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private async Task<NodeModel> ReadNodeModelAsync(Session session,
            RequestHeader header, NodeId nodeId, bool skipValue, bool? children,
            List<OperationResult> diagnostics) {
            var node = await GenericNode.ReadAsync(session, header, nodeId, skipValue,
                diagnostics);
            return new NodeModel {
                HasChildren = children,
                Id = nodeId.AsString(session.MessageContext),
                Name = node.BrowseName?.AsString(session.MessageContext),
                DisplayName = node.DisplayName?.ToString(),
                Description = node.Description?.ToString(),
                NodeClass = node.NodeClass.ToServiceType(),
                AccessRestrictions = node.AccessRestrictions == null ?
                    (NodeAccessRestrictions?)null : (NodeAccessRestrictions)node.AccessRestrictions,
                UserWriteMask = node.UserWriteMask,
                WriteMask = node.WriteMask,
                DataType = node.DataType?.AsString(session.MessageContext),
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
                Value = node.Value == null ? null : _codec.Encode(node.Value.Value, out var type,
                    session.MessageContext),
                EventNotifier = node.EventNotifier == null ?
                    (NodeEventNotifier?)null : (NodeEventNotifier)node.EventNotifier,
                DataTypeDefinition = node.DataTypeDefinition == null ? null :
                    _codec.Encode(new Variant(node.DataTypeDefinition)),
                InverseName = node.InverseName?.ToString(),
                Symmetric = node.Symmetric,
                ContainsNoLoops = node.ContainsNoLoops,
                Executable = node.Executable,
                UserExecutable = node.UserExecutable
            };
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="targetNodesOnly"></param>
        /// <param name="readValues"></param>
        /// <param name="result"></param>
        /// <param name="diagnostics"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        private async Task<string> AddReferencesToBrowseResult(Session session,
            RequestHeader header, bool targetNodesOnly, bool readValues,
            List<NodeReferenceModel> result, List<OperationResult> diagnostics,
            byte[] continuationPoint, List<ReferenceDescription> references) {
            if (references == null) {
                return null;
            }
            foreach (var reference in references) {
                try {
                    var nodeId = reference.NodeId.ToNodeId(session.NamespaceUris);
                    var id = nodeId.AsString(session.MessageContext);
                    if (targetNodesOnly && result.Any(r => r.Target.Id == id)) {
                        continue;
                    }
                    // Check for children
                    bool? children = null;
                    try {
                        var response = await session.BrowseAsync(header, null, nodeId, 1,
                            Opc.Ua.BrowseDirection.Forward,
                            ReferenceTypeIds.HierarchicalReferences,
                            true, 0, BrowseResultMask.All);
                        OperationResultEx.Validate("FetchChildren_" + nodeId, diagnostics,
                            response?.Results?.Select(r => r.StatusCode), response?.DiagnosticInfos);
                        if (response.Results.Count == 1) {
                            children = response.Results[0].References.Count != 0;
                        }
                    }
                    catch (Exception ex) {
                        _logger.Debug("Failed to obtain hasChildren information", () => ex);
                        // TODO: Add diagnostics result for diagnostics.
                    }
                    var model = await ReadNodeModelAsync(session, header, nodeId, !readValues,
                        children, diagnostics);
                    if (targetNodesOnly) {
                        result.Add(new NodeReferenceModel { Target = model });
                        continue;
                    }
                    result.Add(new NodeReferenceModel {
                        BrowseName = reference.BrowseName.AsString(session.MessageContext),
                        DisplayName = reference.DisplayName.ToString(),
                        Id = reference.ReferenceTypeId.AsString(session.MessageContext),
                        Direction = reference.IsForward ?
                            OpcUa.Twin.Models.BrowseDirection.Forward :
                            OpcUa.Twin.Models.BrowseDirection.Backward,
                        Target = model,
                        TypeDefinition = reference.TypeDefinition.AsString(session.MessageContext),
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
        /// <param name="header"></param>
        /// <param name="readValues"></param>
        /// <param name="result"></param>
        /// <param name="diagnostics"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        private async Task AddTargetsToBrowseResult(Session session,
            RequestHeader header, bool readValues,
            List<NodePathTargetModel> result, List<OperationResult> diagnostics,
            BrowsePathTargetCollection targets) {
            if (targets != null) {
                foreach (var target in targets) {
                    try {
                        var nodeId = target.TargetId.ToNodeId(session.NamespaceUris);
                        var model = await ReadNodeModelAsync(session, header, nodeId,
                            !readValues, false, diagnostics);
                        result.Add(new NodePathTargetModel {
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

        private readonly ILogger _logger;
        private readonly IVariantEncoder _codec;
        private readonly IEndpointServices _client;
    }
}
