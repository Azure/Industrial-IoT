// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using BrowseDirection = Models.BrowseDirection;
    using NodeClass = Models.NodeClass;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class provides access to a servers address space providing node
    /// and browse services. It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class NodeServices<T> : INodeServices<T>, INodeServicesInternal<T>,
        IDisposable
    {
        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="parser"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="timeProvider"></param>
        public NodeServices(IOpcUaClientManager<T> client, IFilterParser parser,
            ILogger<NodeServices<T>> logger, IOptions<PublisherOptions> options,
            TimeProvider? timeProvider = null)
        {
            _logger = logger;
            _client = client;
            _parser = parser;
            _options = options;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(T endpoint,
            RequestHeaderModel? header, CancellationToken ct)
        {
            return _client.ExecuteAsync(endpoint, async context =>
                await context.Session.GetServerCapabilitiesAsync(
                    header.GetNamespaceFormat(_options), ct).ConfigureAwait(false), header, ct);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(T endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("BrowseFirst");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var rootId = request.NodeId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(rootId))
                {
                    rootId = ObjectIds.RootFolder;
                }
                var typeId = request.ReferenceTypeId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(typeId))
                {
                    typeId = ReferenceTypeIds.HierarchicalReferences;
                }
                var view = request.View.ToStackModel(context.Session.MessageContext);
                var excludeReferences = false;
                var rawMode = request.NodeIdsOnly ?? false;
                if (!rawMode)
                {
                    excludeReferences = request.MaxReferencesToReturn == 0;
                }

                var references = new List<NodeReferenceModel>();
                ServiceResultModel? errorInfo = null;
                if (!excludeReferences)
                {
                    var direction = (request.Direction ?? BrowseDirection.Forward)
                        .ToStackType();
                    var browseDescriptions = new BrowseDescriptionCollection
                    {
                        new BrowseDescription
                        {
                            BrowseDirection = direction,
                            IncludeSubtypes = !(request.NoSubtypes ?? false),
                            NodeClassMask = (uint)request.NodeClassFilter.ToStackMask(),
                            NodeId = rootId,
                            ReferenceTypeId = typeId,
                            ResultMask = (uint)BrowseResultMask.All
                        }
                    };
                    // Browse and read children
                    var response = await context.Session.Services.BrowseAsync(
                        request.Header.ToRequestHeader(_timeProvider), ViewDescription.IsDefault(view)
                            ? null : view, request.MaxReferencesToReturn ?? 0, browseDescriptions,
                        ct).ConfigureAwait(false);

                    var results = response.Validate(response.Results, r => r.StatusCode,
                        response.DiagnosticInfos, browseDescriptions);
                    errorInfo = results.ErrorInfo;
                    if (errorInfo == null)
                    {
                        errorInfo = results[0].ErrorInfo;
                        context.TrackedToken = await AddReferencesToBrowseResultAsync(context.Session,
                            request.Header, request.TargetNodesOnly ?? false,
                            request.ReadVariableValues ?? false, rawMode, references,
                            results[0].Result.ContinuationPoint,
                            results[0].Result.References, ct).ConfigureAwait(false);
                    }
                }

                var (node, nodeError) = await context.Session.ReadNodeAsync(
                    request.Header.ToRequestHeader(_timeProvider), rootId, null, true, rawMode,
                    request.Header.GetNamespaceFormat(_options),
                    !excludeReferences ? references.Count != 0 : null, ct).ConfigureAwait(false);

                return new BrowseFirstResponseModel
                {
                    // Read root node
                    Node = node,
                    References = excludeReferences ? Array.Empty<NodeReferenceModel>() : references,
                    ContinuationToken = context.TrackedToken,
                    ErrorInfo = errorInfo ?? nodeError
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(T endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Missing continuation token", nameof(request));
            }
            using var trace = _activitySource.StartActivity("BrowseNext");
            var continuationPoint = Convert.FromBase64String(request.ContinuationToken);
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var references = new List<NodeReferenceModel>();

                var continuationPoints = new ByteStringCollection { continuationPoint };
                var response = await context.Session.Services.BrowseNextAsync(
                    request.Header.ToRequestHeader(_timeProvider), request.Abort ?? false,
                    continuationPoints, ct).ConfigureAwait(false);

                context.UntrackedToken = request.ContinuationToken;
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, continuationPoints);
                if (results.ErrorInfo != null)
                {
                    return new BrowseNextResponseModel
                    {
                        References = references,
                        ErrorInfo = results.ErrorInfo
                    };
                }

                context.TrackedToken = await AddReferencesToBrowseResultAsync(context.Session,
                    request.Header, request.TargetNodesOnly ?? false,
                    request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                    references, results[0].Result.ContinuationPoint,
                    results[0].Result.References, ct).ConfigureAwait(false);
                return new BrowseNextResponseModel
                {
                    References = references,
                    ContinuationToken = context.TrackedToken,
                    ErrorInfo = results[0].ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(T endpoint,
            BrowseStreamRequestModel request, CancellationToken ct)
        {
            var stream = new BrowseStream(request, _options, _activitySource, _logger,
                _timeProvider, ct);
            return _client.ExecuteAsync(endpoint, stream, request.Header, ct);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(T endpoint,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.BrowsePaths == null ||
                request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Bad browse path", nameof(request));
            }
            using var trace = _activitySource.StartActivity("BrowsePath");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var rootId = request.NodeId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(rootId))
                {
                    rootId = ObjectIds.RootFolder;
                }
                var targets = new List<NodePathTargetModel>();
                var requests = new BrowsePathCollection(request.BrowsePaths.Select(p =>
                    new BrowsePath
                    {
                        StartingNode = rootId,
                        RelativePath = p.ToRelativePath(context.Session.MessageContext)
                    }));
                var response = await context.Session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    request.Header.ToRequestHeader(_timeProvider), requests, context.Ct).ConfigureAwait(false);
                var results = response.Validate(
                    response.Results, r => r.StatusCode, response.DiagnosticInfos, request.BrowsePaths);
                if (results.ErrorInfo != null)
                {
                    return new BrowsePathResponseModel
                    {
                        ErrorInfo = results.ErrorInfo
                    };
                }
                foreach (var operation in results)
                {
                    await AddTargetsToBrowseResultAsync(context.Session,
                        request.Header, request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                        targets, operation.Result.Targets,
                        [.. operation.Request], context.Ct).ConfigureAwait(false);
                }
                return new BrowsePathResponseModel
                {
                    Targets = targets,
                    ErrorInfo = results[0].ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            T endpoint, NodeMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Node id or browse path missing", nameof(request));
            }
            using var trace = _activitySource.StartActivity("GetMetadata");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var nodeId = await context.Session.ResolveNodeIdAsync(request.Header, request.NodeId,
                    request.BrowsePath, nameof(request.BrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Node id missing", nameof(request));
                }

                var (node, errorInfo) = await context.Session.ReadNodeAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId,
                    request.Header.GetNamespaceFormat(_options), ct: context.Ct).ConfigureAwait(false);
                if (errorInfo != null || node.NodeClass == null)
                {
                    return new NodeMetadataResponseModel
                    {
                        ErrorInfo = errorInfo ?? ((StatusCode)StatusCodes.BadNodeIdUnknown)
                            .CreateResultModel()
                    };
                }

                VariableMetadataModel? variableMetadata = null;
                MethodMetadataModel? methodMetadata = null;
                DataTypeMetadataModel? dataTypeMetadata = null;
                var typeId = nodeId;
                if (node.NodeClass == NodeClass.Method)
                {
                    (methodMetadata, errorInfo) = await context.Session.GetMethodMetadataAsync(
                        request.Header.ToRequestHeader(_timeProvider), nodeId,
                        request.Header.GetNamespaceFormat(_options), context.Ct).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        return new NodeMetadataResponseModel { ErrorInfo = errorInfo };
                    }
                }
                else if (node.NodeClass == NodeClass.DataType)
                {
                    // TODO
                    dataTypeMetadata = null;
                }
                else if (node.NodeClass is NodeClass.Variable or NodeClass.Object)
                {
                    // Get type definition
                    var (references, ei) = await context.Session.FindAsync(
                        request.Header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                        ReferenceTypeIds.HasTypeDefinition, maxResults: 1, ct: context.Ct).ConfigureAwait(false);
                    typeId = references.FirstOrDefault(r => r.ErrorInfo == null).Node;
                    if (NodeId.IsNull(typeId))
                    {
                        typeId = nodeId;
                    }
                    if (node.NodeClass == NodeClass.Variable)
                    {
                        (variableMetadata, errorInfo) = await context.Session.GetVariableMetadataAsync(
                            request.Header.ToRequestHeader(_timeProvider), nodeId,
                            request.Header.GetNamespaceFormat(_options), context.Ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return new NodeMetadataResponseModel { ErrorInfo = errorInfo };
                        }
                    }
                }

                var type = node;
                if (typeId != nodeId)
                {
                    (type, errorInfo) = await context.Session.ReadNodeAsync(
                        request.Header.ToRequestHeader(_timeProvider), typeId,
                        request.Header.GetNamespaceFormat(_options), ct: context.Ct).ConfigureAwait(false);
                    if (errorInfo != null || type.NodeClass == null)
                    {
                        return new NodeMetadataResponseModel
                        {
                            ErrorInfo = errorInfo ?? ((StatusCode)StatusCodes.BadTypeDefinitionInvalid)
                                .CreateResultModel()
                        };
                    }
                }

                // process the types starting from the top of the tree.
                var map = new Dictionary<ImmutableRelativePath, InstanceDeclarationModel>();
                var declarations = new List<InstanceDeclarationModel>();

                var hierarchy = new List<(NodeId, ReferenceDescription)>();
                await context.Session.CollectTypeHierarchyAsync(request.Header.ToRequestHeader(_timeProvider),
                    typeId, hierarchy, context.Ct).ConfigureAwait(false);
                hierarchy.Reverse(); // Start from Root super type
                foreach (var (subType, superType) in hierarchy)
                {
                    errorInfo = await context.Session.CollectInstanceDeclarationsAsync(
                        request.Header.ToRequestHeader(_timeProvider), (NodeId)superType.NodeId,
                        null, declarations, map, request.Header.GetNamespaceFormat(_options),
                        ct: context.Ct).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        break;
                    }
                }
                if (errorInfo == null)
                {
                    // collect the fields for the selected type.
                    errorInfo = await context.Session.CollectInstanceDeclarationsAsync(
                        request.Header.ToRequestHeader(_timeProvider), typeId, null,
                        declarations, map, request.Header.GetNamespaceFormat(_options),
                        ct: context.Ct).ConfigureAwait(false);
                }
                return new NodeMetadataResponseModel
                {
                    ErrorInfo = errorInfo,
                    Description = node.Description,
                    DisplayName = node.DisplayName,
                    NodeClass = node.NodeClass.Value,
                    NodeId = node.NodeId,
                    MethodMetadata = methodMetadata,
                    VariableMetadata = variableMetadata,
                    DataTypeMetadata = dataTypeMetadata,
                    TypeDefinition = errorInfo != null ? null : new TypeDefinitionModel
                    {
                        TypeDefinitionId = type.NodeId,
                        DisplayName = type.DisplayName,
                        BrowseName = type.BrowseName,
                        Description = type.Description,
                        TypeHierarchy = hierarchy.ConvertAll(e => new NodeModel
                        {
                            NodeId = request.Header.AsString(e.Item2.NodeId,
                                context.Session.MessageContext, _options),
                            DisplayName = e.Item2.DisplayName.AsString(),
                            BrowseName = request.Header.AsString(e.Item2.BrowseName,
                                context.Session.MessageContext, _options),
                            NodeClass = e.Item2.NodeClass.ToServiceType()
                        }),
                        NodeType = GetNodeType(hierarchy
                            .Select(r => (NodeId)r.Item2.NodeId)
                            .Append(typeId)
                            .ToList()),
                        Declarations = declarations
                    }
                };
            }, request.Header, ct).ConfigureAwait(false);

            static NodeType GetNodeType(List<NodeId> hierarchy)
            {
                if (hierarchy.Count > 1)
                {
                    if (hierarchy[1] == ObjectTypeIds.BaseEventType)
                    {
                        return NodeType.Event;
                    }
                    if (hierarchy[1] == ObjectTypeIds.BaseInterfaceType)
                    {
                        return NodeType.Interface;
                    }
                    if (hierarchy[1] == VariableTypeIds.PropertyType)
                    {
                        return NodeType.Property;
                    }
                    if (hierarchy[1] == VariableTypeIds.BaseDataVariableType)
                    {
                        return NodeType.DataVariable;
                    }
                }
                if (hierarchy.Count > 0)
                {
                    if (hierarchy[0] == VariableTypeIds.BaseVariableType)
                    {
                        return NodeType.Variable;
                    }
                    if (hierarchy[0] == ObjectTypeIds.BaseObjectType)
                    {
                        return NodeType.Object;
                    }
                }
                return NodeType.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<QueryCompilationResponseModel> CompileQueryAsync(T endpoint,
            QueryCompilationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.Query))
            {
                throw new ArgumentException("Query must not be empty", nameof(request));
            }
            using var trace = _activitySource.StartActivity("CompileQuery");
            try
            {
                return await _client.ExecuteAsync(endpoint, async context =>
                {
                    var parserContext = new SessionParserContext(context.Session,
                        request.Header.ToRequestHeader(_timeProvider),
                        request.Header.GetNamespaceFormat(_options));
                    var eventFilter = await _parser.ParseEventFilterAsync(request.Query,
                        parserContext, context.Ct).ConfigureAwait(false);
                    return new QueryCompilationResponseModel
                    {
                        EventFilter = eventFilter,
                        ErrorInfo = parserContext.ErrorInfo
                    };
                }, request.Header, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // This is where the parser exceptions will end up
                return new QueryCompilationResponseModel
                {
                    ErrorInfo = ex.ToServiceResultModel()
                };
            }
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            T endpoint, MethodMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.MethodId) &&
                (request.MethodBrowsePath == null || request.MethodBrowsePath.Count == 0))
            {
                throw new ArgumentException("Node id missing", nameof(request));
            }
            using var trace = _activitySource.StartActivity("GetMethodMetadata");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var methodId = await context.Session.ResolveNodeIdAsync(request.Header, request.MethodId,
                    request.MethodBrowsePath, nameof(request.MethodBrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);

                if (NodeId.IsNull(methodId))
                {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var browseDescriptions = new BrowseDescriptionCollection
                {
                    new BrowseDescription
                    {
                        BrowseDirection = Opc.Ua.BrowseDirection.Both,
                        IncludeSubtypes = true,
                        NodeClassMask = 0,
                        NodeId = methodId,
                        ReferenceTypeId = ReferenceTypeIds.Aggregates,
                        ResultMask = (uint)BrowseResultMask.All
                    }
                };
                // Get default input arguments and types
                var browse = await context.Session.Services.BrowseAsync(
                    request.Header.ToRequestHeader(_timeProvider), null, 0, browseDescriptions,
                    context.Ct).ConfigureAwait(false);

                var browseresults = browse.Validate(browse.Results, r => r.StatusCode,
                    browse.DiagnosticInfos, browseDescriptions);
                if (browseresults.ErrorInfo != null)
                {
                    return new MethodMetadataResponseModel
                    {
                        ErrorInfo = browseresults.ErrorInfo
                    };
                }
                var result = new MethodMetadataResponseModel();
                foreach (var nodeReference in browseresults[0].Result.References)
                {
                    if (result.OutputArguments != null &&
                        result.InputArguments != null &&
                        !string.IsNullOrEmpty(result.ObjectId))
                    {
                        break;
                    }
                    if (!nodeReference.IsForward)
                    {
                        if (nodeReference.ReferenceTypeId == ReferenceTypeIds.HasComponent)
                        {
                            result.ObjectId = request.Header.AsString(nodeReference.NodeId,
                                context.Session.MessageContext, _options);
                        }
                        continue;
                    }
                    var isInput = nodeReference.BrowseName == BrowseNames.InputArguments;
                    if (!isInput && nodeReference.BrowseName != BrowseNames.OutputArguments)
                    {
                        continue;
                    }

                    var node = nodeReference.NodeId.ToNodeId(context.Session.MessageContext.NamespaceUris);
                    var (value, errorInfo) = await context.Session.ReadValueAsync(
                        request.Header.ToRequestHeader(_timeProvider), node, context.Ct).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        return new MethodMetadataResponseModel
                        {
                            ErrorInfo = errorInfo
                        };
                    }
                    if (value?.Value is not ExtensionObject[] argumentsList)
                    {
                        continue;
                    }

                    var argList = new List<MethodMetadataArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body))
                    {
                        var (dataTypeIdNode, errorInfo2) = await context.Session.ReadNodeAsync(
                            request.Header.ToRequestHeader(_timeProvider), argument.DataType, null,
                            false, false, request.Header.GetNamespaceFormat(_options),
                            false, context.Ct).ConfigureAwait(false);
                        var arg = new MethodMetadataArgumentModel
                        {
                            Name = argument.Name,
                            DefaultValue = argument.Value == null ? VariantValue.Null :
                                context.Session.Codec.Encode(new Variant(argument.Value), out var type),
                            ValueRank = argument.ValueRank == ValueRanks.Scalar ?
                                null : (global::Azure.IIoT.OpcUa.Publisher.Models.NodeValueRank)argument.ValueRank,
                            ArrayDimensions = argument.ArrayDimensions?.ToArray(),
                            Description = argument.Description?.ToString(),
                            ErrorInfo = errorInfo2,
                            Type = dataTypeIdNode
                        };
                        argList.Add(arg);
                    }
                    if (isInput)
                    {
                        result.InputArguments = argList;
                    }
                    else
                    {
                        result.OutputArguments = argList;
                    }
                }
                return result;
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(T endpoint,
            MethodCallRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ObjectId) &&
                (request.ObjectBrowsePath == null || request.ObjectBrowsePath.Count == 0))
            {
                throw new ArgumentException("Object id missing or bad browse path", nameof(request));
            }
            using var trace = _activitySource.StartActivity("MethodCall");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
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
                var objectId = await context.Session.ResolveNodeIdAsync(request.Header, request.ObjectId,
                    request.ObjectBrowsePath, nameof(request.ObjectBrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);
                if (NodeId.IsNull(objectId))
                {
                    throw new ArgumentException("Object id missing", nameof(request));
                }

                var methodId = request.MethodId.ToNodeId(context.Session.MessageContext);
                if (request.MethodBrowsePath?.Count > 0)
                {
                    if (NodeId.IsNull(methodId))
                    {
                        // Browse from object id to method if possible
                        methodId = objectId ??
                            throw new ArgumentException("Method id and object id missing",
                                nameof(request));
                    }
                    methodId = await context.Session.ResolveBrowsePathToNodeAsync(request.Header,
                        methodId, [.. request.MethodBrowsePath], nameof(request.MethodBrowsePath),
                        _timeProvider, context.Ct).ConfigureAwait(false);
                }
                else if (NodeId.IsNull(methodId))
                {
                    // Method is null and cannot browse to method from object
                    throw new ArgumentException("Method id missing", nameof(request));
                }

                var browseDescriptions = new BrowseDescriptionCollection {
                    new BrowseDescription {
                        BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                        IncludeSubtypes = true,
                        NodeClassMask = 0,
                        NodeId = methodId,
                        ReferenceTypeId = ReferenceTypeIds.HasProperty,
                        ResultMask = (uint)BrowseResultMask.All
                    }
                };
                // Get default input arguments and types
                var browse = await context.Session.Services.BrowseAsync(
                    request.Header.ToRequestHeader(_timeProvider), null, 0, browseDescriptions,
                    context.Ct).ConfigureAwait(false);

                var browseresults = browse.Validate(browse.Results, r => r.StatusCode,
                    browse.DiagnosticInfos, browseDescriptions);
                List<(TypeInfo, object)>? inputs = null, outputs = null;
                if (browseresults.ErrorInfo == null)
                {
                    foreach (var nodeReference in browseresults[0].Result.References)
                    {
                        List<(TypeInfo, object)>? temp = null;
                        if (nodeReference.BrowseName == BrowseNames.InputArguments)
                        {
                            temp = inputs = [];
                        }
                        else if (nodeReference.BrowseName == BrowseNames.OutputArguments)
                        {
                            temp = outputs = [];
                        }
                        else
                        {
                            continue;
                        }
                        var node = nodeReference.NodeId.ToNodeId(context.Session.MessageContext.NamespaceUris);
                        var (value, _) = await context.Session.ReadValueAsync(
                            request.Header.ToRequestHeader(_timeProvider),
                            node, context.Ct).ConfigureAwait(false);
                        // value is also null if the type is not a variable node
                        if (value?.Value is ExtensionObject[] argumentsList)
                        {
                            foreach (var argument in argumentsList.Select(a => (Argument)a.Body))
                            {
                                var builtInType = TypeInfo.GetBuiltInType(argument.DataType);
                                temp.Add((new TypeInfo(builtInType,
                                    argument.ValueRank), argument.Value));
                            }
                            if (inputs != null && outputs != null)
                            {
                                break;
                            }
                        }
                    }
                }

                if ((request.Arguments?.Count ?? 0) > (inputs?.Count ?? 0))
                {
                    // Too many arguments provided
                    if (browseresults.ErrorInfo != null)
                    {
                        return new MethodCallResponseModel
                        {
                            Results = Array.Empty<MethodCallArgumentModel>(),
                            ErrorInfo = browseresults.ErrorInfo
                        };
                    }
                    throw new ArgumentException("Arguments missing", nameof(request));
                }

                // Set default input arguments from meta data
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = objectId,
                        MethodId = methodId,
                        InputArguments = inputs == null ? [] :
                            new VariantCollection(inputs
                                .Select(arg => arg.Item1.CreateVariant(arg.Item2)))
                    }
                };

                // Update with input arguments provided in request payload
                if ((request.Arguments?.Count ?? 0) != 0)
                {
                    Debug.Assert(request.Arguments != null);
                    Debug.Assert(inputs != null);
                    for (var i = 0; i < request.Arguments.Count; i++)
                    {
                        var arg = request.Arguments[i];
                        if (arg == null)
                        {
                            continue;
                        }
                        var builtinType = inputs[i].Item1.BuiltInType;
                        if (!string.IsNullOrEmpty(arg.DataType))
                        {
                            builtinType = await context.Session.LruNodeCache.GetBuiltInTypeAsync(
                                arg.DataType.ToNodeId(context.Session.MessageContext),
                                context.Ct).ConfigureAwait(false);
                        }
                        requests[0].InputArguments[i] = context.Session.Codec.Decode(
                            arg.Value ?? VariantValue.Null, builtinType);
                    }
                }

                // Call method
                var response = await context.Session.Services.CallAsync(
                    request.Header.ToRequestHeader(_timeProvider), requests,
                    context.Ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new MethodCallResponseModel
                    {
                        Results = Array.Empty<MethodCallArgumentModel>(),
                        ErrorInfo = results.ErrorInfo
                    };
                }
                // Create output argument list
                var arguments = new List<MethodCallArgumentModel>();

                var args = results[0].Result.OutputArguments?.Count ?? 0;
                for (var i = 0; i < args; i++)
                {
                    var arg = results[0].Result.OutputArguments[i];
                    if (arg == Variant.Null &&
                        (outputs?.Count ?? 0) > i && outputs![i].Item2 != null)
                    {
                        // return default value
                        arg = new Variant(outputs[i].Item2);
                    }
                    var value = context.Session.Codec.Encode(arg, out var type);
                    if (type == BuiltInType.Null && (outputs?.Count ?? 0) > i)
                    {
                        // return default type from type info
                        type = outputs![i].Item1.BuiltInType;
                    }
                    var dataType = type == BuiltInType.Null ?
                        null : type.ToString();
                    arguments.Add(new MethodCallArgumentModel
                    {
                        Value = value,
                        DataType = dataType
                    });
                }
                return new MethodCallResponseModel
                {
                    Results = arguments,
                    ErrorInfo = results[0].ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(T endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Bad node id or browse path missing", nameof(request));
            }
            using var trace = _activitySource.StartActivity("ValueRead");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var readNode = await context.Session.ResolveNodeIdAsync(request.Header, request.NodeId,
                    request.BrowsePath, nameof(request.BrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);
                if (NodeId.IsNull(readNode))
                {
                    throw new ArgumentException("Node id missing", nameof(request));
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
                var response = await context.Session.Services.ReadAsync(
                    request.Header.ToRequestHeader(_timeProvider),
                    request.MaxAge?.TotalMilliseconds ?? 0,
                    request.TimestampsToReturn.ToStackType(),
                    nodesToRead, context.Ct).ConfigureAwait(false);

                var values = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, nodesToRead);
                if (values.ErrorInfo != null)
                {
                    return new ValueReadResponseModel
                    {
                        ErrorInfo = values.ErrorInfo
                    };
                }
                return new ValueReadResponseModel
                {
                    ServerPicoseconds = values[0].Result.ServerPicoseconds != 0 ?
                        values[0].Result.ServerPicoseconds : null,
                    ServerTimestamp = values[0].Result.ServerTimestamp != DateTime.MinValue ?
                        values[0].Result.ServerTimestamp : null,
                    SourcePicoseconds = values[0].Result.SourcePicoseconds != 0 ?
                        values[0].Result.SourcePicoseconds : null,
                    SourceTimestamp = values[0].Result.SourceTimestamp != DateTime.MinValue ?
                        values[0].Result.SourceTimestamp : null,
                    Value = context.Session.Codec.Encode(values[0].Result.WrappedValue, out var type),
                    DataType = type == BuiltInType.Null ? null : type.ToString(),
                    ErrorInfo = values[0].ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(T endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Value is null)
            {
                throw new ArgumentException("Missing value", nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Bad node id or browse path missing", nameof(request));
            }
            using var trace = _activitySource.StartActivity("ValueWrite");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var writeNode = await context.Session.ResolveNodeIdAsync(request.Header, request.NodeId,
                    request.BrowsePath, nameof(request.BrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);
                if (NodeId.IsNull(writeNode))
                {
                    throw new ArgumentException("Node id missing", nameof(request));
                }
                var dataTypeId = request.DataType.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(dataTypeId))
                {
                    // Read data type
                    (dataTypeId, _) = await context.Session.ReadAttributeAsync<NodeId?>(
                        request.Header.ToRequestHeader(_timeProvider), writeNode,
                        Attributes.DataType, context.Ct).ConfigureAwait(false);
                    if (NodeId.IsNull(dataTypeId))
                    {
                        throw new ArgumentException("Data type missing", nameof(request));
                    }
                }

                var builtinType = await context.Session.LruNodeCache.GetBuiltInTypeAsync(dataTypeId,
                    context.Ct).ConfigureAwait(false);
                var value = context.Session.Codec.Decode(request.Value, builtinType);
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = writeNode,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(value),
                        IndexRange = request.IndexRange
                    }
                };
                var result = new ValueWriteResponseModel();
                var response = await context.Session.Services.WriteAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodesToWrite,
                    context.Ct).ConfigureAwait(false);
                var values = response.Validate(response.Results, s => s,
                    response.DiagnosticInfos, nodesToWrite);
                return new ValueWriteResponseModel
                {
                    ErrorInfo = values.ErrorInfo ?? values[0].ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(T endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null)
            {
                throw new ArgumentException("Missing attributes", nameof(request));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId)))
            {
                throw new ArgumentException("Bad attributes", nameof(request));
            }
            using var trace = _activitySource.StartActivity("Read");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var requests = new ReadValueIdCollection(request.Attributes
                    .Select(a => new ReadValueId
                    {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId.ToNodeId(context.Session.MessageContext)
                    }));
                var response = await context.Session.Services.ReadAsync(
                    request.Header.ToRequestHeader(_timeProvider), 0, Opc.Ua.TimestampsToReturn.Both,
                    requests, context.Ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new ReadResponseModel
                    {
                        Results = Array.Empty<AttributeReadResponseModel>(),
                        ErrorInfo = results.ErrorInfo
                    };
                }
                return new ReadResponseModel
                {
                    Results = results
                        .Select(result =>
                        {
                            return new AttributeReadResponseModel
                            {
                                Value = context.Session.Codec.Encode(result.Result.WrappedValue,
                                    out var wellKnown),
                                ErrorInfo = result.ErrorInfo
                            };
                        }).ToList()
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(T endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null)
            {
                throw new ArgumentException("Missing attributes", nameof(request));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId)))
            {
                throw new ArgumentException("Missing node id in attributes", nameof(request));
            }
            using var trace = _activitySource.StartActivity("Write");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var requests = new WriteValueCollection(request.Attributes
                    .Select(a => new WriteValue
                    {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId.ToNodeId(context.Session.MessageContext),
                        Value = new DataValue(context.Session.Codec.Decode(a.Value ?? VariantValue.Null,
                            AttributeMap.GetBuiltInType((uint)a.Attribute)))
                    }));
                var response = await context.Session.Services.WriteAsync(
                    request.Header.ToRequestHeader(_timeProvider), requests,
                    context.Ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, s => s,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new WriteResponseModel
                    {
                        Results = Array.Empty<AttributeWriteResponseModel>(),
                        ErrorInfo = results.ErrorInfo
                    };
                }
                return new WriteResponseModel
                {
                    Results = results
                        .Select(result =>
                        {
                            return new AttributeWriteResponseModel
                            {
                                ErrorInfo = result.ErrorInfo
                            };
                        }).ToList()
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            T endpoint, RequestHeaderModel? header, CancellationToken ct)
        {
            return _client.ExecuteAsync(endpoint, async context =>
                await context.Session.GetHistoryCapabilitiesAsync(header.GetNamespaceFormat(_options),
                context.Ct).ConfigureAwait(false), header, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            T endpoint, HistoryConfigurationRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.NodeId))
            {
                throw new ArgumentException("Bad node id missing", nameof(request));
            }
            using var trace = _activitySource.StartActivity("HistoryGetConfiguration");
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var nodeId = request.NodeId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Bad node id", nameof(request));
                }

                // load the defaults for the historical configuration object.
                var config =
                    new HistoricalDataConfigurationState(null);
                config.Definition =
                    new PropertyState<string>(config);
                config.MaxTimeInterval =
                    new PropertyState<double>(config);
                config.MinTimeInterval =
                    new PropertyState<double>(config);
                config.ExceptionDeviation =
                    new PropertyState<double>(config);
                config.ExceptionDeviationFormat =
                    new PropertyState<ExceptionDeviationFormat>(config);
                config.StartOfArchive =
                    new PropertyState<DateTime>(config);
                config.StartOfOnlineArchive =
                    new PropertyState<DateTime>(config);
                config.Stepped =
                    new PropertyState<bool>(config);
                config.ServerTimestampSupported =
                    new PropertyState<bool>(config);
                config.AggregateFunctions =
                    new FolderState(config);

                var aggregate = new AggregateConfigurationState(config);
                aggregate.TreatUncertainAsBad =
                    new PropertyState<bool>(aggregate);
                aggregate.UseSlopedExtrapolation =
                    new PropertyState<bool>(aggregate);
                aggregate.PercentDataBad =
                    new PropertyState<byte>(aggregate);
                aggregate.PercentDataGood =
                    new PropertyState<byte>(aggregate);
                config.AggregateConfiguration =
                    aggregate;

                config.Create(context.Session.SystemContext, null,
                    BrowseNames.HAConfiguration, null, false);

                var relativePath = new RelativePath();
                relativePath.Elements.Add(new RelativePathElement
                {
                    ReferenceTypeId = ReferenceTypeIds.HasHistoricalConfiguration,
                    IsInverse = false,
                    IncludeSubtypes = false,
                    TargetName = BrowseNames.HAConfiguration
                });
                var errorInfo = await context.Session.ReadNodeStateAsync(
                    request.Header.ToRequestHeader(_timeProvider), config,
                    nodeId, relativePath, context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return new HistoryConfigurationResponseModel
                    {
                        ErrorInfo = errorInfo
                    };
                }
                var startTime = config.StartOfOnlineArchive.GetValueOrDefaultEx()
                    ?? config.StartOfArchive.GetValueOrDefaultEx();
                if (startTime == null)
                {
                    startTime = await HistoryReadTimestampAsync(
                        context.Session, request.Header, nodeId, true,
                        _timeProvider, context.Ct).ConfigureAwait(false);
                }
                DateTime? endTime = null;
                if (endTime == null)
                {
                    endTime = await HistoryReadTimestampAsync(
                        context.Session, request.Header, nodeId, false,
                        _timeProvider, context.Ct).ConfigureAwait(false);
                }
                var children = new List<BaseInstanceState>();
                config.AggregateFunctions.GetChildren(context.Session.SystemContext, children);
                var aggregateFunctions = children.OfType<BaseObjectState>().ToDictionary(
                    c => request.Header.AsString(c.BrowseName, context.Session.MessageContext, _options),
                    c => request.Header.AsString(c.NodeId, context.Session.MessageContext, _options));
                return new HistoryConfigurationResponseModel
                {
                    Configuration = errorInfo != null ? null : new HistoryConfigurationModel
                    {
                        MinTimeInterval =
                            config.MinTimeInterval.GetValueOrDefaultEx(
                                v => v.HasValue && v.Value != 0 ?
                                TimeSpan.FromMilliseconds(v.Value) : (TimeSpan?)null),
                        MaxTimeInterval =
                            config.MaxTimeInterval.GetValueOrDefaultEx(
                                v => v.HasValue && v.Value != 0 ?
                                TimeSpan.FromMilliseconds(v.Value) : (TimeSpan?)null),
                        ExceptionDeviation =
                            config.ExceptionDeviation.GetValueOrDefaultEx(),
                        ExceptionDeviationType =
                            config.ExceptionDeviationFormat.GetValueOrDefaultEx(
                                v => v.ToExceptionDeviationType()),
                        ServerTimestampSupported =
                            config.ServerTimestampSupported.GetValueOrDefaultEx(),
                        Stepped =
                            config.Stepped.GetValueOrDefaultEx(),
                        Definition =
                            config.Definition.GetValueOrDefaultEx(),
                        AggregateFunctions =
                            aggregateFunctions.Count == 0 ? null : aggregateFunctions,
                        AggregateConfiguration = new AggregateConfigurationModel
                        {
                            PercentDataBad =
                                aggregate.PercentDataBad.GetValueOrDefaultEx(),
                            PercentDataGood =
                                aggregate.PercentDataGood.GetValueOrDefaultEx(),
                            TreatUncertainAsBad =
                                aggregate.TreatUncertainAsBad.GetValueOrDefaultEx(),
                            UseSlopedExtrapolation =
                                aggregate.UseSlopedExtrapolation.GetValueOrDefaultEx()
                        },
                        StartOfOnlineArchive = startTime ??
                            config.StartOfOnlineArchive.GetValueOrDefaultEx(
                                v => v == DateTime.MinValue ? startTime : v),
                        StartOfArchive =
                            config.StartOfArchive.GetValueOrDefaultEx(
                                v => v == DateTime.MinValue ? startTime : v) ?? startTime,
                        EndOfArchive = endTime
                    }
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(T endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            return HistoryReadAsync(endpoint, request, (details, session) =>
            {
                var variant = session.Codec.Decode(details, BuiltInType.ExtensionObject);
                if (variant.Value is not ExtensionObject extensionObject)
                {
                    throw new ArgumentException("Bad details", nameof(request));
                }
                return extensionObject;
            }, (details, session) => session.Codec.Encode(new Variant(details), out _), ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            T endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return HistoryReadNextAsync(endpoint, request,
                (details, session) => session.Codec.Encode(new Variant(details), out _), ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpdateAsync(T endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            return HistoryUpdateAsync(endpoint, request, (nodeId, details, session) =>
            {
                var variant = session.Codec.Decode(details, BuiltInType.ExtensionObject);
                if (variant.Value is not ExtensionObject extensionObject)
                {
                    throw new ArgumentException("Bad details", nameof(request));
                }
                if (extensionObject.Body is HistoryUpdateDetails updateDetails)
                {
                    updateDetails.NodeId = nodeId;
                }
                return Task.FromResult(extensionObject);
            }, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<TResult>> HistoryReadAsync<TInput, TResult>(
            T connectionId, HistoryReadRequestModel<TInput> request,
            Func<TInput, IOpcUaSession, ExtensionObject> decode,
            Func<ExtensionObject, IOpcUaSession, TResult> encode,
            CancellationToken ct) where TInput : class where TResult : class
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Missing details", nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Bad node id or browse path missing",
                    nameof(request));
            }
            using var trace = _activitySource.StartActivity("HistoryRead");
            return await _client.ExecuteAsync(connectionId, async context =>
            {
                var nodeId = await context.Session.ResolveNodeIdAsync(request.Header, request.NodeId,
                    request.BrowsePath, nameof(request.BrowsePath), _timeProvider,
                    context.Ct).ConfigureAwait(false);
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Bad node id", nameof(request));
                }
                var readDetails = decode(request.Details, context.Session);
                var historytoread = new HistoryReadValueIdCollection
                {
                    new HistoryReadValueId
                    {
                        IndexRange = request.IndexRange,
                        NodeId = nodeId,
                        DataEncoding = null // TODO
                    }
                };
                var response = await context.Session.Services.HistoryReadAsync(
                    request.Header.ToRequestHeader(_timeProvider), readDetails,
                    request.TimestampsToReturn.ToStackType(),
                    false, historytoread, context.Ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, historytoread);
                if (results.ErrorInfo != null)
                {
                    return new HistoryReadResponseModel<TResult>
                    {
                        History = null,
                        ErrorInfo = results.ErrorInfo
                    };
                }

                var history = encode(results[0].Result.HistoryData, context.Session);
                var errorInfo = results[0].ErrorInfo;
                if (errorInfo?.StatusCode == StatusCodes.GoodNoData &&
                    history is Array array && array.Length > 0)
                {
                    errorInfo = null; // There is data, so fix up error
                }
                context.TrackedToken =
                    results[0].Result.ContinuationPoint == null ||
                    results[0].Result.ContinuationPoint.Length == 0 ? null :
                    Convert.ToBase64String(results[0].Result.ContinuationPoint);
                return new HistoryReadResponseModel<TResult>
                {
                    ContinuationToken = context.TrackedToken,
                    History = history,
                    ErrorInfo = errorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<TResult>> HistoryReadNextAsync<TResult>(
            T connectionId, HistoryReadNextRequestModel request,
            Func<ExtensionObject, IOpcUaSession, TResult> encode, CancellationToken ct)
            where TResult : class
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Missing continuation", nameof(request));
            }
            using var trace = _activitySource.StartActivity("HistoryReadNext");
            return await _client.ExecuteAsync(connectionId, async context =>
            {
                var historytoread = new HistoryReadValueIdCollection
                {
                    new HistoryReadValueId
                    {
                        ContinuationPoint = Convert.FromBase64String(request.ContinuationToken),
                        DataEncoding = null // TODO
                    }
                };
                var response = await context.Session.Services.HistoryReadAsync(
                    request.Header.ToRequestHeader(_timeProvider), null, Opc.Ua.TimestampsToReturn.Both,
                    request.Abort ?? false, historytoread, context.Ct).ConfigureAwait(false);
                context.UntrackedToken = request.ContinuationToken;

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, historytoread);
                if (results.ErrorInfo != null)
                {
                    return new HistoryReadNextResponseModel<TResult>
                    {
                        History = null,
                        ErrorInfo = results.ErrorInfo
                    };
                }
                var history = encode(results[0].Result.HistoryData, context.Session);
                var errorInfo = results[0].ErrorInfo;
                if (errorInfo?.StatusCode == StatusCodes.GoodNoData &&
                    history is Array array && array.Length > 0)
                {
                    errorInfo = null; // There is data, so fix up error
                }
                context.TrackedToken =
                    results[0].Result.ContinuationPoint == null ||
                    results[0].Result.ContinuationPoint.Length == 0 ? null :
                    Convert.ToBase64String(results[0].Result.ContinuationPoint);
                return new HistoryReadNextResponseModel<TResult>
                {
                    ContinuationToken = context.TrackedToken,
                    History = history,
                    ErrorInfo = errorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync<TInput>(
            T connectionId, HistoryUpdateRequestModel<TInput> request,
            Func<NodeId, TInput, IOpcUaSession, Task<ExtensionObject>> decode,
            CancellationToken ct) where TInput : class
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Missing details", nameof(request));
            }
            using var trace = _activitySource.StartActivity("HistoryUpdate");
            return await _client.ExecuteAsync(connectionId, async context =>
            {
                var nodeId = await context.Session.ResolveNodeIdAsync(request.Header,
                    request.NodeId, request.BrowsePath, nameof(request.BrowsePath),
                    _timeProvider, context.Ct).ConfigureAwait(false);
                // Update the node id to target based on the request
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Missing node id", nameof(request));
                }
                var details = await decode(nodeId, request.Details,
                    context.Session).ConfigureAwait(false);
                if (details == null)
                {
                    throw new ArgumentException("Bad details", nameof(request));
                }
                var updates = new ExtensionObjectCollection { details };
                var response = await context.Session.Services.HistoryUpdateAsync(
                    request.Header.ToRequestHeader(_timeProvider), updates,
                    context.Ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, updates);
                if (results.ErrorInfo != null)
                {
                    return new HistoryUpdateResponseModel { ErrorInfo = results.ErrorInfo };
                }
                var inner = response.Validate(response.Results[0].OperationResults, s => s,
                    response.Results[0].DiagnosticInfos);
                return new HistoryUpdateResponseModel
                {
                    Results = inner.Select(r => r.ResultInfo).ToList(),
                    ErrorInfo = inner.ErrorInfo
                };
            }, request.Header, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="targetNodesOnly"></param>
        /// <param name="readValues"></param>
        /// <param name="rawMode"></param>
        /// <param name="result"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string?> AddReferencesToBrowseResultAsync(IOpcUaSession session,
            RequestHeaderModel? header, bool targetNodesOnly, bool readValues,
            bool rawMode, List<NodeReferenceModel> result, byte[] continuationPoint,
            List<ReferenceDescription> references, CancellationToken ct)
        {
            if (references == null)
            {
                return null;
            }
            foreach (var reference in references)
            {
                try
                {
                    var nodeId = reference.NodeId.ToNodeId(session.MessageContext.NamespaceUris);
                    var id = header.AsString(nodeId, session.MessageContext, _options);
                    if (targetNodesOnly && result.Any(r => r.Target.NodeId == id))
                    {
                        continue;
                    }
                    bool? children = null;
                    if (!rawMode)
                    {
                        // Check for children
                        try
                        {
                            var browseDescriptions = new BrowseDescriptionCollection {
                                new BrowseDescription {
                                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                                    IncludeSubtypes = true,
                                    NodeClassMask = 0,
                                    NodeId = nodeId,
                                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                                    ResultMask = (uint)BrowseResultMask.All
                                }
                            };
                            var response = await session.Services.BrowseAsync(
                                header.ToRequestHeader(_timeProvider), null, 1,
                                browseDescriptions, ct).ConfigureAwait(false);
                            Debug.Assert(response != null);
                            var results = response.Validate(response.Results, r => r.StatusCode,
                                response.DiagnosticInfos, browseDescriptions);
                            if (results.ErrorInfo == null && results.Count > 0)
                            {
                                children = results[0].Result.References.Count != 0;
                                if (results[0].Result.ContinuationPoint != null)
                                {
                                    await session.Services.BrowseNextAsync(header.ToRequestHeader(_timeProvider),
                                        true,
                                        [
                                            response.Results[0].ContinuationPoint
                                        ], ct: ct).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.BrowseStreamChildInfoFailed(ex);
                        }
                    }
                    var (model, _) = await session.ReadNodeAsync(header.ToRequestHeader(_timeProvider), nodeId,
                        reference.NodeClass, !readValues, rawMode, header.GetNamespaceFormat(_options),
                        children, ct).ConfigureAwait(false);
                    if (rawMode)
                    {
                        model = model with
                        {
                            BrowseName = header.AsString(reference.BrowseName,
                                session.MessageContext, _options),
                            DisplayName = reference.DisplayName?.ToString()
                        };
                    }
                    model = model with
                    {
                        TypeDefinitionId = header.AsString(reference.TypeDefinition,
                            session.MessageContext, _options)
                    };
                    if (targetNodesOnly)
                    {
                        result.Add(new NodeReferenceModel { Target = model });
                        continue;
                    }
                    result.Add(new NodeReferenceModel
                    {
                        ReferenceTypeId = header.AsString(reference.ReferenceTypeId,
                            session.MessageContext, _options),
                        Direction = reference.IsForward ?
                            BrowseDirection.Forward : BrowseDirection.Backward,
                        Target = model
                    });
                }
                catch
                {
                    // TODO: Add trace result for trace.
                }
            }
            return continuationPoint?.ToBase64String();
        }

        /// <summary>
        /// Add targets
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="readValues"></param>
        /// <param name="rawMode"></param>
        /// <param name="result"></param>
        /// <param name="targets"></param>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task AddTargetsToBrowseResultAsync(IOpcUaSession session,
            RequestHeaderModel? header, bool readValues, bool rawMode,
            List<NodePathTargetModel> result, BrowsePathTargetCollection targets,
            string[] path, CancellationToken ct)
        {
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    try
                    {
                        var nodeId = target.TargetId.ToNodeId(session.MessageContext.NamespaceUris);
                        var (model, _) = await session.ReadNodeAsync(header.ToRequestHeader(_timeProvider),
                            nodeId, null, !readValues, rawMode, header.GetNamespaceFormat(_options), false,
                            ct).ConfigureAwait(false);
                        result.Add(new NodePathTargetModel
                        {
                            BrowsePath = path,
                            Target = model,
                            RemainingPathIndex = target.RemainingPathIndex == 0 ?
                                null : (int)target.RemainingPathIndex
                        });
                    }
                    catch
                    {
                        // Skip node - TODO: Should we add a failure
                        // reference into the yet unused trace instead?
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the first or last date of the archive
        /// (truncates milliseconds).
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="first"></param>
        /// <param name="timeProvider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task<DateTime?> HistoryReadTimestampAsync(
            IOpcUaSession session, RequestHeaderModel? header, NodeId nodeId,
            bool first, TimeProvider timeProvider, CancellationToken ct)
        {
            // do it the hard way (may take a long time with some servers).
            var nodesToRead = new HistoryReadValueIdCollection {
                new HistoryReadValueId {
                    NodeId = nodeId
                }
            };
            var details = new ExtensionObject(new ReadRawModifiedDetails
            {
                StartTime = first ? new DateTime(1970, 1, 1) : DateTime.MinValue,
                EndTime = first ? DateTime.MinValue : timeProvider.GetUtcNow().AddDays(1).UtcDateTime,
                NumValuesPerNode = 1,
                IsReadModified = false,
                ReturnBounds = false
            });
            var response = await session.Services.HistoryReadAsync(header.ToRequestHeader(timeProvider),
                details, Opc.Ua.TimestampsToReturn.Source, false,
                nodesToRead, ct).ConfigureAwait(false);
            var results = response.Validate(response.Results, s => s.StatusCode,
                response.DiagnosticInfos, nodesToRead);
            try
            {
                if (StatusCode.IsBad(results.StatusCode) ||
                    StatusCode.IsBad(results[0].StatusCode) ||
                    ExtensionObject.ToEncodeable(results[0].Result.HistoryData)
                        is not HistoryData historyData)
                {
                    return null;
                }
                var date = historyData.DataValues[0].SourceTimestamp;
                date = new DateTime(date.Year, date.Month,
                    date.Day, date.Hour, date.Minute,
                    date.Second, 0, DateTimeKind.Utc);
                return !first ? date.AddSeconds(1) : date;
            }
            finally
            {
                // Abort read if needed
                if (results[0].Result.ContinuationPoint?.Length > 0)
                {
                    nodesToRead = [
                        new HistoryReadValueId {
                            NodeId = nodeId,
                            ContinuationPoint = results[0].Result.ContinuationPoint
                        }
                    ];
                    await session.Services.HistoryReadAsync(header.ToRequestHeader(timeProvider),
                        details, Opc.Ua.TimestampsToReturn.Source, true,
                        nodesToRead, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Browse stream helper class
        /// </summary>
        internal sealed class BrowseStream : AsyncEnumerableEnumerableStack<BrowseStreamChunkModel>
        {
            /// <summary>
            /// Create browse stream helper
            /// </summary>
            /// <param name="request"></param>
            /// <param name="options"></param>
            /// <param name="activitySource"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            /// <param name="ct"></param>
            public BrowseStream(BrowseStreamRequestModel request, IOptions<PublisherOptions> options,
                ActivitySource activitySource, ILogger logger, TimeProvider timeProvider,
                CancellationToken ct)
            {
                _activitySource = activitySource;
                _sw = Stopwatch.StartNew();
                _logger = logger;
                _timeProvider = timeProvider;
                _options = options;
                _ct = ct;
                _request = request;
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                base.Reset();
                Push(ReadNodeAsync);
            }

            /// <summary>
            /// Read node
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<BrowseStreamChunkModel>> ReadNodeAsync(
                ServiceCallContext context)
            {
                using var trace = _activitySource.StartActivity("ReadNode");

                // Lazy initialize to capture session context
                if (_nodeIds == null)
                {
                    // Initialize
                    _nodeIds = _request.NodeIds == null ? [] : _request.NodeIds
                        .Select(n => n.ToNodeId(context.Session.MessageContext))
                        .Where(n => !NodeId.IsNull(n))
                        .ToArray();
                    if (_nodeIds.Length == 0)
                    {
                        _browseStack.Push(ObjectIds.RootFolder);
                    }
                    else
                    {
                        foreach (var resolvedId in _nodeIds)
                        {
                            _browseStack.Push(resolvedId);
                        }
                    }
                }

                BrowseStreamChunkModel? chunk = null;
                var nodeId = PopNode();
                if (nodeId == null)
                {
                    // Done - no more nodes on the browse stack to browse
                    _logger.BrowseStreamSummary(_nodes, _references, _sw.Elapsed);
                    return [];
                }

                var (node, errorInfo) = await context.Session.ReadNodeAsync(
                    _request.Header.ToRequestHeader(_timeProvider), nodeId,
                    _request.Header.GetNamespaceFormat(_options), null,
                    !(_request.ReadVariableValues ?? false), null, _ct).ConfigureAwait(false);

                _visited.Add(nodeId); // Mark as visited

                var id = _request.Header.AsString(nodeId, context.Session.MessageContext, _options);
                if (id == null)
                {
                    return [];
                }

                chunk = new BrowseStreamChunkModel
                {
                    SourceId = id,
                    Attributes = node,
                    Reference = null,
                    ErrorInfo = errorInfo
                };
                _nodes++;

                // Browse the node now
                Push(context => BrowseAsync(context, id, nodeId));

                // Read another node from the browse stack
                Push(ReadNodeAsync);
                return chunk.YieldReturn();
            }

            /// <summary>
            /// Browse references
            /// </summary>
            /// <param name="context"></param>
            /// <param name="sourceId"></param>
            /// <param name="nodeId"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<BrowseStreamChunkModel>> BrowseAsync(
                ServiceCallContext context, string sourceId, NodeId nodeId)
            {
                using var trace = _activitySource.StartActivity("Browse");

                if (_typeId == null)
                {
                    _typeId = _request.ReferenceTypeId.ToNodeId(context.Session.MessageContext);
                    if (NodeId.IsNull(_typeId))
                    {
                        _typeId = ReferenceTypeIds.HierarchicalReferences;
                    }
                }
                _view ??= _request.View.ToStackModel(context.Session.MessageContext);
                var browseDescriptions = new BrowseDescriptionCollection
                {
                    new BrowseDescription
                    {
                        BrowseDirection = (_request.Direction ?? BrowseDirection.Both)
                            .ToStackType(),
                        IncludeSubtypes = !(_request.NoSubtypes ?? false),
                        NodeClassMask = (uint)_request.NodeClassFilter.ToStackMask(),
                        NodeId = nodeId,
                        ReferenceTypeId = _typeId,
                        ResultMask = (uint)BrowseResultMask.All
                    }
                };
                // Browse and read children
                var response = await context.Session.Services.BrowseAsync(
                    _request.Header.ToRequestHeader(_timeProvider),
                    ViewDescription.IsDefault(_view) ? null : _view, 0,
                    browseDescriptions, _ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, browseDescriptions);
                if (results.ErrorInfo != null)
                {
                    var chunk = new BrowseStreamChunkModel
                    {
                        SourceId = sourceId,
                        ErrorInfo = results.ErrorInfo
                    };
                    return chunk.YieldReturn();
                }
                var refs = CollectReferences(context.Session, sourceId,
                    results[0].Result.References, results[0].ErrorInfo,
                    _request.NoRecurse ?? false);
                var continuation = results[0].Result.ContinuationPoint ?? [];
                if (continuation.Length > 0)
                {
                    Push(context => BrowseNextAsync(context, sourceId, continuation));
                }
                else
                {
                    // Read another node from the browse stack
                    Push(ReadNodeAsync);
                }
                return refs;
            }

            /// <summary>
            /// Browse remainder of references
            /// </summary>
            /// <param name="context"></param>
            /// <param name="sourceId"></param>
            /// <param name="continuationPoint"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<BrowseStreamChunkModel>> BrowseNextAsync(
                ServiceCallContext context, string sourceId, byte[] continuationPoint)
            {
                using var trace = _activitySource.StartActivity("BrowseNext");

                var continuationPoints = new ByteStringCollection { continuationPoint };
                var response = await context.Session.Services.BrowseNextAsync(
                    _request.Header.ToRequestHeader(_timeProvider), false, continuationPoints,
                    _ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, continuationPoints);
                if (results.ErrorInfo != null)
                {
                    var chunk = new BrowseStreamChunkModel
                    {
                        SourceId = sourceId,
                        ErrorInfo = results.ErrorInfo
                    };
                    return chunk.YieldReturn();
                }

                var refs = CollectReferences(context.Session, sourceId,
                    results[0].Result.References, results[0].ErrorInfo,
                    _request.NoRecurse ?? false);

                var continuation = results[0].Result.ContinuationPoint ?? [];
                if (continuation.Length > 0)
                {
                    Push(session => BrowseNextAsync(session, sourceId, continuation));
                }
                else
                {
                    // Read another node from the browse stack
                    Push(ReadNodeAsync);
                }
                return refs;
            }

            /// <summary>
            /// Helper to push nodes onto the browse stack
            /// </summary>
            /// <param name="nodeId"></param>
            private void PushNode(ExpandedNodeId nodeId)
            {
                if ((nodeId?.ServerIndex ?? 1u) != 0)
                {
                    return;
                }
                var local = (NodeId)nodeId;
                if (!NodeId.IsNull(local) && !_visited.Contains(local))
                {
                    _browseStack.Push(local);
                }
            }

            /// <summary>
            /// Helper to pop nodes from the browse stack
            /// </summary>
            /// <returns></returns>
            private NodeId? PopNode()
            {
                while (_browseStack.TryPop(out var nodeId))
                {
                    if (!NodeId.IsNull(nodeId) && !_visited.Contains(nodeId))
                    {
                        return nodeId;
                    }
                }
                return null;
            }

            /// <summary>
            /// Collect references
            /// </summary>
            /// <param name="session"></param>
            /// <param name="sourceId"></param>
            /// <param name="refs"></param>
            /// <param name="errorInfo"></param>
            /// <param name="noRecurse"></param>
            /// <returns></returns>
            private IEnumerable<BrowseStreamChunkModel> CollectReferences(
                IOpcUaSession session, string sourceId, ReferenceDescriptionCollection refs,
                ServiceResultModel? errorInfo, bool noRecurse)
            {
                foreach (var reference in refs)
                {
                    if (!noRecurse)
                    {
                        PushNode(reference.NodeId);
                        PushNode(reference.ReferenceTypeId);
                        PushNode(reference.TypeDefinition);
                    }

                    _references++;

                    var id = _request.Header.AsString(reference.NodeId, session.MessageContext,
                        _options);
                    if (id == null)
                    {
                        continue;
                    }
                    yield return new BrowseStreamChunkModel
                    {
                        SourceId = sourceId,
                        ErrorInfo = errorInfo,
                        Reference = new NodeReferenceModel
                        {
                            ReferenceTypeId = _request.Header.AsString(reference.ReferenceTypeId,
                                session.MessageContext, _options),
                            Direction = reference.IsForward ?
                                BrowseDirection.Forward : BrowseDirection.Backward,
                            Target = new NodeModel
                            {
                                NodeId = id,
                                DisplayName = reference.DisplayName?.ToString(),
                                TypeDefinitionId = _request.Header.AsString(reference.TypeDefinition,
                                    session.MessageContext, _options),
                                BrowseName = _request.Header.AsString(reference.BrowseName,
                                    session.MessageContext, _options)
                            }
                        }
                    };
                }
            }

            private readonly Stack<NodeId> _browseStack = new();
            private readonly HashSet<NodeId> _visited = [];
            private int _nodes;
            private int _references;
            private NodeId? _typeId;
            private NodeId[]? _nodeIds;
            private ViewDescription? _view;
            private readonly Stopwatch _sw;
            private readonly BrowseStreamRequestModel _request;
            private readonly ILogger _logger;
            private readonly TimeProvider _timeProvider;
            private readonly IOptions<PublisherOptions> _options;
            private readonly CancellationToken _ct;
            private readonly ActivitySource _activitySource;
        }

        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
        private readonly IOptions<PublisherOptions> _options;
        private readonly TimeProvider _timeProvider;
        private readonly IFilterParser _parser;
        private readonly IOpcUaClientManager<T> _client;
        private readonly ILogger _logger;
    }

    internal static partial class NodeServicesLogging
    {
        private const int EventClass = 220;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "Browsed {Nodes} nodes and {References} references in address space in {Elapsed}...")]
        public static partial void BrowseStreamSummary(this ILogger logger, int Nodes, int References, TimeSpan Elapsed);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "Failed to obtain child information")]
        public static partial void BrowseStreamChildInfoFailed(this ILogger logger, Exception exception);
    }
}
