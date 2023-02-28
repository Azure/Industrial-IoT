// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using BrowseDirection = OpcUa.Models.BrowseDirection;
    using NodeClass = OpcUa.Models.NodeClass;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class provides access to a servers address space providing node
    /// and browse services.  It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class NodeServices<T> : INodeServices<T>, INodeServicesInternal<T>
    {
        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        ///
        public NodeServices(ISessionProvider<T> client, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(T id,
            CancellationToken ct)
        {
            return _client.ExecuteServiceAsync(id, async session =>
                await session.GetServerCapabilitiesAsync(ct).ConfigureAwait(false),
                ct: ct);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(T id,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            using var trace = kActivity.StartActivity("BrowseFirst");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var rootId = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId))
                {
                    rootId = ObjectIds.RootFolder;
                }
                var typeId = request.ReferenceTypeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(typeId))
                {
                    typeId = ReferenceTypeIds.HierarchicalReferences;
                }
                var view = request.View.ToStackModel(session.MessageContext);
                var excludeReferences = false;
                var rawMode = request.NodeIdsOnly ?? false;
                if (!rawMode)
                {
                    excludeReferences = request.MaxReferencesToReturn == 0;
                }

                var references = new List<NodeReferenceModel>();
                string? continuationToken = null;
                ServiceResultModel? errorInfo = null;
                if (!excludeReferences)
                {
                    var direction = (request.Direction ?? BrowseDirection.Forward)
                        .ToStackType();
                    var browseDescriptions = new BrowseDescriptionCollection {
                        new BrowseDescription {
                            BrowseDirection = direction,
                            IncludeSubtypes = !(request.NoSubtypes ?? false),
                            NodeClassMask = (uint)request.NodeClassFilter.ToStackMask(),
                            NodeId = rootId,
                            ReferenceTypeId = typeId,
                            ResultMask = (uint)BrowseResultMask.All
                        }
                    };
                    // Browse and read children
                    var response = await session.Services.BrowseAsync(request.Header.ToRequestHeader(),
                        ViewDescription.IsDefault(view) ? null : view,
                        request.MaxReferencesToReturn ?? 0, browseDescriptions,
                        ct).ConfigureAwait(false);

                    var results = response.Validate(response.Results, r => r.StatusCode,
                        response.DiagnosticInfos, browseDescriptions);
                    errorInfo = results.ErrorInfo;
                    if (errorInfo == null)
                    {
                        errorInfo = results[0].ErrorInfo;
                        continuationToken = await AddReferencesToBrowseResultAsync(session,
                            request.Header.ToRequestHeader(), request.TargetNodesOnly ?? false,
                            request.ReadVariableValues ?? false, rawMode, references,
                            results[0].Result.ContinuationPoint,
                            results[0].Result.References, ct).ConfigureAwait(false);
                    }
                }

                var (node, nodeError) = await session.ReadNodeAsync(
                    request.Header.ToRequestHeader(), rootId, null, true, rawMode,
                    !excludeReferences ? references.Count != 0 : null, ct).ConfigureAwait(false);

                return new BrowseFirstResponseModel
                {
                    // Read root node
                    Node = node,
                    References = excludeReferences ? null : references,
                    ContinuationToken = continuationToken,
                    ErrorInfo = errorInfo ?? nodeError
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(T id,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Missing continuation token", nameof(request));
            }
            using var trace = kActivity.StartActivity("BrowseNext");
            var continuationPoint = Convert.FromBase64String(request.ContinuationToken);
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var references = new List<NodeReferenceModel>();

                var continuationPoints = new ByteStringCollection { continuationPoint };
                var response = await session.Services.BrowseNextAsync(
                    request.Header.ToRequestHeader(), request.Abort ?? false,
                    continuationPoints, ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, continuationPoints);
                if (results.ErrorInfo != null)
                {
                    return new BrowseNextResponseModel
                    {
                        ErrorInfo = results.ErrorInfo
                    };
                }

                var continuationToken = await AddReferencesToBrowseResultAsync(session,
                    request.Header.ToRequestHeader(), request.TargetNodesOnly ?? false,
                    request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                    references, results[0].Result.ContinuationPoint,
                    results[0].Result.References, ct).ConfigureAwait(false);
                return new BrowseNextResponseModel
                {
                    References = references,
                    ContinuationToken = continuationToken,
                    ErrorInfo = results[0].ErrorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(T id,
            BrowseStreamRequestModel request, [EnumeratorCancellation] CancellationToken ct)
        {
            var browseStack = new Stack<NodeId>();
            var visited = new HashSet<NodeId>();
            var nodes = 0;
            var references = 0;
            NodeId? typeId = null;
            NodeId[]? nodeIds = null;
            ViewDescription? view = null;
            using var trace = kActivity.StartActivity("Browse");
            _logger.LogDebug("Browsing all nodes in address space ...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            do
            {
                ct.ThrowIfCancellationRequested();
                // Read source node
                var source = await _client.ExecuteServiceAsync(id, async session =>
                {
                    // Lazy initialize to capture session context
                    if (nodeIds == null)
                    {
                        // Initialize
                        nodeIds = request.NodeIds == null ? Array.Empty<NodeId>() : request.NodeIds
                            .Select(n => n.ToNodeId(session.MessageContext))
                            .Where(n => !NodeId.IsNull(n))
                            .ToArray();
                        if (nodeIds.Length == 0)
                        {
                            browseStack.Push(ObjectIds.RootFolder);
                        }
                        else
                        {
                            foreach (var id in nodeIds)
                            {
                                browseStack.Push(id);
                            }
                        }
                    }

                    BrowseStreamChunkModel? chunk = null;
                    var nodeId = PopNode();
                    if (nodeId != null)
                    {
                        var (node, errorInfo) = await session.ReadNodeAsync(
                            request.Header.ToRequestHeader(),
                            nodeId, null, !(request.ReadVariableValues ?? false),
                            null, ct).ConfigureAwait(false);

                        visited.Add(nodeId); // Mark as visited
                        var id = nodeId.AsString(session.MessageContext);
                        if (id != null)
                        {
                            chunk = new BrowseStreamChunkModel
                            {
                                SourceId = id,
                                Attributes = node,
                                Reference = null,
                                ErrorInfo = errorInfo
                            };
                        }
                    }
                    return (chunk, nodeId);
                }, ct: ct).ConfigureAwait(false);

                // Return result and read references
                if (source.nodeId != null)
                {
                    if (source.chunk == null)
                    {
                        continue; // Check whether there is more
                    }
                    nodes++;
                    yield return source.chunk;
                    //  if (source.chunk.ErrorInfo != null) {
                    //      continue; // Check whether there is more
                    //  }

                    // Read first set of references
                    var chunks = await _client.ExecuteServiceAsync(id, async session =>
                    {
                        if (typeId == null)
                        {
                            typeId = request.ReferenceTypeId.ToNodeId(session.MessageContext);
                            if (NodeId.IsNull(typeId))
                            {
                                typeId = ReferenceTypeIds.HierarchicalReferences;
                            }
                        }
                        if (view == null)
                        {
                            view = request.View.ToStackModel(session.MessageContext);
                        }
                        var browseDescriptions = new BrowseDescriptionCollection {
                            new BrowseDescription {
                                BrowseDirection = (request.Direction ?? BrowseDirection.Both)
                                    .ToStackType(),
                                IncludeSubtypes = !(request.NoSubtypes ?? false),
                                NodeClassMask = (uint)request.NodeClassFilter.ToStackMask(),
                                NodeId = source.nodeId,
                                ReferenceTypeId = typeId,
                                ResultMask = (uint)BrowseResultMask.All
                            }
                        };
                        // Browse and read children
                        var response = await session.Services.BrowseAsync(request.Header.ToRequestHeader(),
                            ViewDescription.IsDefault(view) ? null : view, 0,
                            browseDescriptions, ct).ConfigureAwait(false);

                        var results = response.Validate(response.Results, r => r.StatusCode,
                            response.DiagnosticInfos, browseDescriptions);
                        if (results.ErrorInfo != null)
                        {
                            var chunk = new BrowseStreamChunkModel
                            {
                                ErrorInfo = results.ErrorInfo
                            };
                            return (chunk.YieldReturn(), Array.Empty<byte>());
                        }
                        var refs = CollectReferences(session, source.chunk.SourceId,
                            results[0].Result.References, results[0].ErrorInfo);
                        return (refs, results[0].Result.ContinuationPoint ?? Array.Empty<byte>());
                    }, ct: ct).ConfigureAwait(false);
                    foreach (var chunk in chunks.Item1.Where(r => r != null))
                    {
                        references++;
                        yield return chunk;
                    }
                    while (chunks.Item2.Length > 0)
                    {
                        // Get remainder
                        chunks = await _client.ExecuteServiceAsync(id, async session =>
                        {
                            // Browse and read children
                            var continuationPoints = new ByteStringCollection {
                                chunks.Item2
                            };
                            var response = await session.Services.BrowseNextAsync(
                                request.Header.ToRequestHeader(), false, continuationPoints,
                                ct).ConfigureAwait(false);

                            var results = response.Validate(response.Results, r => r.StatusCode,
                                response.DiagnosticInfos, continuationPoints);
                            if (results.ErrorInfo != null)
                            {
                                var chunk = new BrowseStreamChunkModel
                                {
                                    ErrorInfo = results.ErrorInfo
                                };
                                return (chunk.YieldReturn(), Array.Empty<byte>());
                            }
                            var refs = CollectReferences(session, source.chunk.SourceId,
                                results[0].Result.References, results[0].ErrorInfo);
                            return (refs, results[0].Result.ContinuationPoint ?? Array.Empty<byte>());
                        }, ct: ct).ConfigureAwait(false);

                        foreach (var chunk in chunks.Item1.Where(r => r != null))
                        {
                            references++;
                            yield return chunk;
                        }
                    }
                }
            }
            while (browseStack.Count != 0 && (!(request.NoRecurse ?? false)));
            _logger.LogDebug("Browsed {Nodes} nodes and {References} references " +
                "in address space in {Elapsed}...", nodes, references, sw.Elapsed);

            // Helper to push nodes onto the browse stack
            void PushNode(ExpandedNodeId nodeId)
            {
                if ((nodeId?.ServerIndex ?? 1u) != 0)
                {
                    return;
                }
                var local = (NodeId)nodeId;
                if (!NodeId.IsNull(local) && !visited.Contains(local))
                {
                    browseStack.Push(local);
                }
            }

            // Helper to pop nodes from the browse stack
            NodeId? PopNode()
            {
                while (browseStack.TryPop(out var nodeId))
                {
                    if (!NodeId.IsNull(nodeId) && !visited.Contains(nodeId))
                    {
                        return nodeId;
                    }
                }
                return null;
            }

            // Collect references
            IEnumerable<BrowseStreamChunkModel?> CollectReferences(
                ISessionHandle session, string sourceId, ReferenceDescriptionCollection refs,
                ServiceResultModel? errorInfo)
            {
                return refs.Select(r =>
                {
                    PushNode(r.NodeId);
                    PushNode(r.ReferenceTypeId);
                    PushNode(r.TypeDefinition);

                    var id = r.NodeId.AsString(session.MessageContext);
                    if (id == null)
                    {
                        return null;
                    }
                    return new BrowseStreamChunkModel
                    {
                        SourceId = sourceId,
                        ErrorInfo = errorInfo,
                        Reference = new NodeReferenceModel
                        {
                            ReferenceTypeId = r.ReferenceTypeId.AsString(
                                session.MessageContext),
                            Direction = r.IsForward ?
                                BrowseDirection.Forward : BrowseDirection.Backward,
                            Target = new NodeModel
                            {
                                NodeId = id,
                                DisplayName = r.DisplayName?.ToString(),
                                TypeDefinitionId = r.TypeDefinition.AsString(session.MessageContext),
                                BrowseName = r.BrowseName.AsString(session.MessageContext)
                            }
                        }
                    };
                });
            }
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(T id,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null ||
                request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Bad browse path", nameof(request));
            }
            using var trace = kActivity.StartActivity("BrowsePath");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var rootId = request.NodeId.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(rootId))
                {
                    rootId = ObjectIds.RootFolder;
                }
                var targets = new List<NodePathTargetModel>();
                var requests = new BrowsePathCollection(request.BrowsePaths.Select(p =>
                    new BrowsePath
                    {
                        StartingNode = rootId,
                        RelativePath = p.ToRelativePath(session.MessageContext)
                    }));
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    request.Header.ToRequestHeader(), requests, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request.BrowsePaths);
                if (results.ErrorInfo != null)
                {
                    return new BrowsePathResponseModel
                    {
                        ErrorInfo = results.ErrorInfo
                    };
                }
                foreach (var operation in results)
                {
                    await AddTargetsToBrowseResultAsync(session,
                        request.Header.ToRequestHeader(),
                        request.ReadVariableValues ?? false, request.NodeIdsOnly ?? false,
                        targets, operation.Result.Targets,
                        operation.Request.ToArray(), ct).ConfigureAwait(false);
                }
                return new BrowsePathResponseModel
                {
                    Targets = targets,
                    ErrorInfo = results[0].ErrorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            T id, NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Node id or browse path missing", nameof(request));
            }
            using var trace = kActivity.StartActivity("GetMetadata");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath?.Count > 0)
                {
                    nodeId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        nodeId, request.BrowsePath.ToArray(),
                        nameof(request.BrowsePath), ct).ConfigureAwait(false);
                }
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Node id missing", nameof(request));
                }

                var (node, errorInfo) = await session.ReadNodeAsync(
                    request.Header.ToRequestHeader(), nodeId, ct: ct).ConfigureAwait(false);
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
                    (methodMetadata, errorInfo) = await session.GetMethodMetadataAsync(
                        request.Header.ToRequestHeader(), nodeId, ct).ConfigureAwait(false);
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
                    var references = await session.FindTargetOfReferenceAsync(
                        request.Header.ToRequestHeader(), nodeId.YieldReturn(),
                        ReferenceTypeIds.HasTypeDefinition, ct).ConfigureAwait(false);
                    (_, typeId) = references.FirstOrDefault();
                    if (NodeId.IsNull(typeId))
                    {
                        typeId = nodeId;
                    }
                    if (node.NodeClass == NodeClass.Variable)
                    {
                        (variableMetadata, errorInfo) = await session.GetVariableMetadataAsync(
                            request.Header.ToRequestHeader(), nodeId, ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return new NodeMetadataResponseModel { ErrorInfo = errorInfo };
                        }
                    }
                }

                var type = node;
                if (typeId != nodeId)
                {
                    (type, errorInfo) = await session.ReadNodeAsync(
                    request.Header.ToRequestHeader(), typeId, ct: ct).ConfigureAwait(false);
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
                var map = new Dictionary<string, InstanceDeclarationModel>();
                var declarations = new List<InstanceDeclarationModel>();

                var hierarchy = new List<(NodeId, ReferenceDescription)>();
                await session.CollectTypeHierarchyAsync(request.Header.ToRequestHeader(),
                    typeId, hierarchy, ct).ConfigureAwait(false);
                hierarchy.Reverse(); // Start from Root super type
                foreach (var (subType, superType) in hierarchy)
                {
                    errorInfo = await session.CollectInstanceDeclarationsAsync(
                        request.Header.ToRequestHeader(), (NodeId)superType.NodeId,
                        null, declarations, map, ct).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        break;
                    }
                }
                if (errorInfo == null)
                {
                    // collect the fields for the selected type.
                    errorInfo = await session.CollectInstanceDeclarationsAsync(
                        request.Header.ToRequestHeader(), typeId, null,
                        declarations, map, ct).ConfigureAwait(false);
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
                            NodeId = e.Item2.NodeId.AsString(session.MessageContext) ?? string.Empty,
                            DisplayName = e.Item2.DisplayName.Text,
                            BrowseName = e.Item2.BrowseName.AsString(session.MessageContext),
                            NodeClass = e.Item2.NodeClass.ToServiceType()
                        }),
                        NodeType = GetNodeType(hierarchy
                            .Select(r => (NodeId)r.Item2.NodeId)
                            .Append(typeId)
                            .ToList()),
                        Declarations = declarations
                    }
                };
            }, ct: ct).ConfigureAwait(false);

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
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            T id, MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId) &&
                (request.MethodBrowsePath == null || request.MethodBrowsePath.Count == 0))
            {
                throw new ArgumentException("Node id missing", nameof(request));
            }
            using var trace = kActivity.StartActivity("GetMethodMetadata");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var methodId = request.MethodId.ToNodeId(session.MessageContext);
                if (request.MethodBrowsePath?.Count > 0)
                {
                    // Browse from object id to method if possible
                    methodId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        methodId, request.MethodBrowsePath.ToArray(),
                        nameof(request.MethodBrowsePath), ct).ConfigureAwait(false);
                }
                if (NodeId.IsNull(methodId))
                {
                    throw new ArgumentException(nameof(request.MethodId));
                }
                var browseDescriptions = new BrowseDescriptionCollection {
                    new BrowseDescription {
                        BrowseDirection = Opc.Ua.BrowseDirection.Both,
                        IncludeSubtypes = true,
                        NodeClassMask = 0,
                        NodeId = methodId,
                        ReferenceTypeId = ReferenceTypeIds.Aggregates,
                        ResultMask = (uint)BrowseResultMask.All
                    }
                };
                // Get default input arguments and types
                var browse = await session.Services.BrowseAsync(
                    request.Header.ToRequestHeader(), null, 0, browseDescriptions,
                    ct).ConfigureAwait(false);

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
                            result.ObjectId = nodeReference.NodeId.AsString(
                                session.MessageContext);
                        }
                        continue;
                    }
                    var isInput = nodeReference.BrowseName == BrowseNames.InputArguments;
                    if (!isInput && nodeReference.BrowseName != BrowseNames.OutputArguments)
                    {
                        continue;
                    }

                    var node = nodeReference.NodeId.ToNodeId(session.MessageContext.NamespaceUris);
                    var (value, errorInfo) = await session.ReadValueAsync(
                        request.Header.ToRequestHeader(), node, ct).ConfigureAwait(false);
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
                        var (dataTypeIdNode, errorInfo2) = await session.ReadNodeAsync(
                            request.Header.ToRequestHeader(), argument.DataType, null,
                            false, false, false, ct).ConfigureAwait(false);
                        var arg = new MethodMetadataArgumentModel
                        {
                            Name = argument.Name,
                            DefaultValue = argument.Value == null ? VariantValue.Null :
                                session.Codec.Encode(new Variant(argument.Value), out var type),
                            ValueRank = argument.ValueRank == ValueRanks.Scalar ?
                                null : (NodeValueRank)argument.ValueRank,
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
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(T id,
            MethodCallRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ObjectId) &&
                (request.ObjectBrowsePath == null || request.ObjectBrowsePath.Count == 0))
            {
                throw new ArgumentException("Object id missing or bad browse path", nameof(request));
            }
            using var trace = kActivity.StartActivity("MethodCall");
            return await _client.ExecuteServiceAsync(id, async session =>
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
                var objectId = request.ObjectId.ToNodeId(session.MessageContext);
                if (request.ObjectBrowsePath?.Count > 0)
                {
                    objectId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        objectId, request.ObjectBrowsePath.ToArray(),
                        nameof(request.ObjectBrowsePath), ct).ConfigureAwait(false);
                }
                if (NodeId.IsNull(objectId))
                {
                    throw new ArgumentException("Object id missing", nameof(request));
                }

                var methodId = request.MethodId.ToNodeId(session.MessageContext);
                if (request.MethodBrowsePath?.Count > 0)
                {
                    if (NodeId.IsNull(methodId))
                    {
                        // Browse from object id to method if possible
                        methodId = objectId ??
                            throw new ArgumentException("Method id and object id missing",
                                nameof(request));
                    }
                    methodId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        methodId, request.MethodBrowsePath.ToArray(),
                        nameof(request.MethodBrowsePath), ct).ConfigureAwait(false);
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
                var browse = await session.Services.BrowseAsync(
                    request.Header.ToRequestHeader(), null, 0, browseDescriptions,
                    ct).ConfigureAwait(false);

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
                            temp = inputs = new List<(TypeInfo, object)>();
                        }
                        else if (nodeReference.BrowseName == BrowseNames.OutputArguments)
                        {
                            temp = outputs = new List<(TypeInfo, object)>();
                        }
                        else
                        {
                            continue;
                        }
                        var node = nodeReference.NodeId.ToNodeId(session.MessageContext.NamespaceUris);
                        var (value, _) = await session.ReadValueAsync(
                            request.Header.ToRequestHeader(),
                            node, ct).ConfigureAwait(false);
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
                        InputArguments = inputs == null ? new VariantCollection() :
                            new VariantCollection(inputs
                                .Select(arg => arg.Item1.CreateVariant(arg.Item2)))
                    }
                };

                // Update with input arguments provided in request payload
                if ((request.Arguments?.Count ?? 0) != 0)
                {
                    System.Diagnostics.Debug.Assert(request.Arguments != null);
                    System.Diagnostics.Debug.Assert(inputs != null);
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
                            builtinType = TypeInfo.GetBuiltInType(
                                arg.DataType.ToNodeId(session.MessageContext),
                                session.TypeTree);
                        }
                        requests[0].InputArguments[i] = session.Codec.Decode(
                            arg.Value ?? VariantValue.Null, builtinType);
                    }
                }

                // Call method
                var response = await session.Services.CallAsync(
                    request.Header.ToRequestHeader(), requests,
                    ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new MethodCallResponseModel
                    {
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
                    var value = session.Codec.Encode(arg, out var type);
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
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(T id,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Bad node id or browse path missing", nameof(request));
            }
            using var trace = kActivity.StartActivity("ValueRead");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var readNode = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath?.Count > 0)
                {
                    readNode = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        readNode, request.BrowsePath.ToArray(),
                        nameof(request.BrowsePath), ct).ConfigureAwait(false);
                }
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
                var response = await session.Services.ReadAsync(request.Header.ToRequestHeader(),
                    request.MaxAge?.TotalMilliseconds ?? 0,
                    request.TimestampsToReturn.ToStackType(),
                    nodesToRead, ct).ConfigureAwait(false);

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
                    Value = session.Codec.Encode(values[0].Result.WrappedValue, out var type),
                    DataType = type == BuiltInType.Null ? null : type.ToString(),
                    ErrorInfo = values[0].ErrorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(T id,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null)
            {
                throw new ArgumentException("Missing value", nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId) &&
                (request.BrowsePath == null || request.BrowsePath.Count == 0))
            {
                throw new ArgumentException("Bad node id or browse path missing", nameof(request));
            }
            using var trace = kActivity.StartActivity("ValueWrite");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var writeNode = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath?.Count > 0)
                {
                    writeNode = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        writeNode, request.BrowsePath.ToArray(),
                        nameof(request.BrowsePath), ct).ConfigureAwait(false);
                }
                if (NodeId.IsNull(writeNode))
                {
                    throw new ArgumentException("Node id missing", nameof(request));
                }
                var dataTypeId = request.DataType.ToNodeId(session.MessageContext);
                if (NodeId.IsNull(dataTypeId))
                {
                    // Read data type
                    (dataTypeId, _) = await session.ReadAttributeAsync<NodeId?>(
                        request.Header.ToRequestHeader(), writeNode,
                        Attributes.DataType, ct).ConfigureAwait(false);
                    if (NodeId.IsNull(dataTypeId))
                    {
                        throw new ArgumentException("Data type missing", nameof(request));
                    }
                }

                var builtinType = TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);
                var value = session.Codec.Decode(request.Value, builtinType);
                var nodesToWrite = new WriteValueCollection{
                    new WriteValue {
                        NodeId = writeNode,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(value),
                        IndexRange = request.IndexRange
                    }
                };
                var result = new ValueWriteResponseModel();
                var response = await session.Services.WriteAsync(
                    request.Header.ToRequestHeader(), nodesToWrite,
                    ct).ConfigureAwait(false);
                var values = response.Validate(response.Results, s => s,
                    response.DiagnosticInfos, nodesToWrite);
                return new ValueWriteResponseModel
                {
                    ErrorInfo = values.ErrorInfo ?? values[0].ErrorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(T id,
            ReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null)
            {
                throw new ArgumentException("Missing attributes", nameof(request));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId)))
            {
                throw new ArgumentException("Bad attributes", nameof(request));
            }
            using var trace = kActivity.StartActivity("Read");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var requests = new ReadValueIdCollection(request.Attributes
                    .Select(a => new ReadValueId
                    {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId.ToNodeId(session.MessageContext)
                    }));
                var response = await session.Services.ReadAsync(
                    request.Header.ToRequestHeader(), 0, Opc.Ua.TimestampsToReturn.Both,
                    requests, ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new ReadResponseModel
                    {
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
                                Value = session.Codec.Encode(result.Result.WrappedValue,
                                    out var wellKnown),
                                ErrorInfo = result.ErrorInfo
                            };
                        }).ToList()
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(T id,
            WriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null)
            {
                throw new ArgumentException("Missing attributes", nameof(request));
            }
            if (request.Attributes.Any(a => string.IsNullOrEmpty(a.NodeId)))
            {
                throw new ArgumentException("Missing node id in attributes", nameof(request));
            }
            using var trace = kActivity.StartActivity("Write");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var requests = new WriteValueCollection(request.Attributes
                    .Select(a => new WriteValue
                    {
                        AttributeId = (uint)a.Attribute,
                        NodeId = a.NodeId.ToNodeId(session.MessageContext),
                        Value = new DataValue(session.Codec.Decode(a.Value ?? VariantValue.Null,
                            AttributeMap.GetBuiltInType((uint)a.Attribute)))
                    }));
                var response = await session.Services.WriteAsync(
                    request.Header.ToRequestHeader(), requests,
                    ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, s => s,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new WriteResponseModel
                    {
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
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            T id, CancellationToken ct)
        {
            return _client.ExecuteServiceAsync(id,
                async session => await session.GetHistoryCapabilitiesAsync(ct).ConfigureAwait(false), ct: ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            T id, HistoryConfigurationRequestModel request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId))
            {
                throw new ArgumentException("Bad node id missing", nameof(request));
            }
            using var trace = kActivity.StartActivity("HistoryGetConfiguration");
            return await _client.ExecuteServiceAsync(id, async session =>
            {
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
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

                config.Create(session.SystemContext, null,
                    BrowseNames.HAConfiguration, null, false);

                var relativePath = new RelativePath();
                relativePath.Elements.Add(new RelativePathElement
                {
                    ReferenceTypeId = ReferenceTypeIds.HasHistoricalConfiguration,
                    IsInverse = false,
                    IncludeSubtypes = false,
                    TargetName = BrowseNames.HAConfiguration
                });
                var errorInfo = await session.ReadNodeStateAsync(
                    request.Header.ToRequestHeader(), config,
                    nodeId, relativePath, ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return new HistoryConfigurationResponseModel
                    {
                        ErrorInfo = errorInfo
                    };
                }
                var startTime = config.StartOfOnlineArchive.GetValueOrDefault() ?? config.StartOfArchive.GetValueOrDefault();
                if (startTime == null)
                {
                    startTime = await HistoryReadTimestampAsync(
                        session, request.Header, nodeId, true,
                        ct).ConfigureAwait(false);
                }
                DateTime? endTime = null;
                if (endTime == null)
                {
                    endTime = await HistoryReadTimestampAsync(
                        session, request.Header, nodeId, false,
                        ct).ConfigureAwait(false);
                }
                var children = new List<BaseInstanceState>();
                config.AggregateFunctions.GetChildren(session.SystemContext, children);
                var aggregateFunctions = children.OfType<BaseObjectState>().ToDictionary(
                    c => c.BrowseName.AsString(session.MessageContext),
                    c => c.NodeId.AsString(session.MessageContext) ?? string.Empty);
                return new HistoryConfigurationResponseModel
                {
                    Configuration = errorInfo != null ? null : new HistoryConfigurationModel
                    {
                        MinTimeInterval =
                            config.MinTimeInterval.GetValueOrDefault(
                                v => v.HasValue && v.Value != 0 ?
                                TimeSpan.FromMilliseconds(v.Value) : (TimeSpan?)null),
                        MaxTimeInterval =
                            config.MaxTimeInterval.GetValueOrDefault(
                                v => v.HasValue && v.Value != 0 ?
                                TimeSpan.FromMilliseconds(v.Value) : (TimeSpan?)null),
                        ExceptionDeviation =
                            config.ExceptionDeviation.GetValueOrDefault(),
                        ExceptionDeviationType =
                            config.ExceptionDeviationFormat.GetValueOrDefault(
                                v => v.ToExceptionDeviationType()),
                        ServerTimestampSupported =
                            config.ServerTimestampSupported.GetValueOrDefault(),
                        Stepped =
                            config.Stepped.GetValueOrDefault(),
                        Definition =
                            config.Definition.GetValueOrDefault(),
                        AggregateFunctions =
                            aggregateFunctions.Count == 0 ? null : aggregateFunctions,
                        AggregateConfiguration = new AggregateConfigurationModel
                        {
                            PercentDataBad =
                                aggregate.PercentDataBad.GetValueOrDefault(),
                            PercentDataGood =
                                aggregate.PercentDataGood.GetValueOrDefault(),
                            TreatUncertainAsBad =
                                aggregate.TreatUncertainAsBad.GetValueOrDefault(),
                            UseSlopedExtrapolation =
                                aggregate.UseSlopedExtrapolation.GetValueOrDefault()
                        },
                        StartOfOnlineArchive = startTime ??
                            config.StartOfOnlineArchive.GetValueOrDefault(
                                v => v == DateTime.MinValue ? startTime : v),
                        StartOfArchive =
                            config.StartOfArchive.GetValueOrDefault(
                                v => v == DateTime.MinValue ? startTime : v) ?? startTime,
                        EndOfArchive = endTime
                    }
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(T id,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            return HistoryReadAsync(id, request, (details, session) =>
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
            T id, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return HistoryReadNextAsync(id, request,
                (details, session) => session.Codec.Encode(new Variant(details), out _), ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpdateAsync(T id,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            return HistoryUpdateAsync(id, request, (nodeId, details, session) =>
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
            Func<TInput, ISessionHandle, ExtensionObject> decode,
            Func<ExtensionObject, ISessionHandle, TResult> encode,
            CancellationToken ct) where TInput : class where TResult : class
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
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
            using var trace = kActivity.StartActivity("HistoryRead");
            return await _client.ExecuteServiceAsync(connectionId, async session =>
            {
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath?.Count > 0)
                {
                    nodeId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        nodeId, request.BrowsePath.ToArray(),
                        nameof(request.BrowsePath), ct).ConfigureAwait(false);
                }
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Bad node id", nameof(request));
                }
                var readDetails = decode(request.Details, session);
                var historytoread = new HistoryReadValueIdCollection {
                    new HistoryReadValueId {
                        IndexRange = request.IndexRange,
                        NodeId = nodeId,
                        DataEncoding = null // TODO
                    }
                };
                var response = await session.Services.HistoryReadAsync(
                    request.Header.ToRequestHeader(), readDetails,
                    request.TimestampsToReturn.ToStackType(),
                    false, historytoread, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, historytoread);
                if (results.ErrorInfo != null)
                {
                    return new HistoryReadResponseModel<TResult> { ErrorInfo = results.ErrorInfo };
                }

                var history = encode(results[0].Result.HistoryData, session);
                var errorInfo = results[0].ErrorInfo;
                if (errorInfo?.StatusCode == StatusCodes.GoodNoData &&
                    history is Array array && array.Length > 0)
                {
                    errorInfo = null; // There is data, so fix up error
                }
                var continuationToken =
                    results[0].Result.ContinuationPoint == null ||
                    results[0].Result.ContinuationPoint.Length == 0 ? null :
                    Convert.ToBase64String(results[0].Result.ContinuationPoint);
                return new HistoryReadResponseModel<TResult>
                {
                    ContinuationToken = continuationToken,
                    History = history,
                    ErrorInfo = errorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<TResult>> HistoryReadNextAsync<TResult>(
            T connectionId, HistoryReadNextRequestModel request,
            Func<ExtensionObject, ISessionHandle, TResult> encode, CancellationToken ct)
            where TResult : class
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Missing continuation", nameof(request));
            }
            using var trace = kActivity.StartActivity("HistoryReadNext");
            return await _client.ExecuteServiceAsync(connectionId, async session =>
            {
                var historytoread = new HistoryReadValueIdCollection {
                    new HistoryReadValueId {
                        ContinuationPoint = Convert.FromBase64String(request.ContinuationToken),
                        DataEncoding = null // TODO
                    }
                };
                var response = await session.Services.HistoryReadAsync(
                    request.Header.ToRequestHeader(), null, Opc.Ua.TimestampsToReturn.Both,
                    request.Abort ?? false, historytoread, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, historytoread);
                if (results.ErrorInfo != null)
                {
                    return new HistoryReadNextResponseModel<TResult> { ErrorInfo = results.ErrorInfo };
                }
                var history = encode(results[0].Result.HistoryData, session);
                var errorInfo = results[0].ErrorInfo;
                if (errorInfo?.StatusCode == StatusCodes.GoodNoData &&
                    history is Array array && array.Length > 0)
                {
                    errorInfo = null; // There is data, so fix up error
                }
                var continuationToken =
                    results[0].Result.ContinuationPoint == null ||
                    results[0].Result.ContinuationPoint.Length == 0 ? null :
                    Convert.ToBase64String(results[0].Result.ContinuationPoint);
                return new HistoryReadNextResponseModel<TResult>
                {
                    ContinuationToken = continuationToken,
                    History = history,
                    ErrorInfo = errorInfo
                };
            }, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync<TInput>(
            T connectionId, HistoryUpdateRequestModel<TInput> request,
            Func<NodeId, TInput, ISessionHandle, Task<ExtensionObject>> decode,
            CancellationToken ct) where TInput : class
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Missing details", nameof(request));
            }
            using var trace = kActivity.StartActivity("HistoryUpdate");
            return await _client.ExecuteServiceAsync(connectionId, async session =>
            {
                var nodeId = request.NodeId.ToNodeId(session.MessageContext);
                if (request.BrowsePath?.Count > 0)
                {
                    nodeId = await ResolveBrowsePathToNodeAsync(session, request.Header,
                        nodeId, request.BrowsePath.ToArray(),
                        nameof(request.BrowsePath), ct).ConfigureAwait(false);
                }
                // Update the node id to target based on the request
                if (NodeId.IsNull(nodeId))
                {
                    throw new ArgumentException("Missing node id", nameof(request));
                }
                var details = await decode(nodeId, request.Details, session).ConfigureAwait(false);
                if (details == null)
                {
                    throw new ArgumentException("Bad details", nameof(request));
                }
                var updates = new ExtensionObjectCollection { details };
                var response = await session.Services.HistoryUpdateAsync(request.Header.ToRequestHeader(),
                    updates, ct).ConfigureAwait(false);
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
            }, ct: ct).ConfigureAwait(false);
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
        private async Task<string?> AddReferencesToBrowseResultAsync(ISessionHandle session,
            RequestHeader header, bool targetNodesOnly, bool readValues,
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
                    var id = nodeId.AsString(session.MessageContext);
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
                            var response = await session.Services.BrowseAsync(header, null, 1,
                                browseDescriptions, ct).ConfigureAwait(false);
                            System.Diagnostics.Debug.Assert(response != null);
                            var results = response.Validate(response.Results, r => r.StatusCode,
                                response.DiagnosticInfos, browseDescriptions);
                            if (results.Count > 0)
                            {
                                children = results[0].Result.References.Count != 0;
                                if (results[0].Result.ContinuationPoint != null)
                                {
                                    await session.Services.BrowseNextAsync(header, true,
                                        new ByteStringCollection {
                                            response.Results[0].ContinuationPoint
                                        }, ct: ct).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation(ex, "Failed to obtain child information");
                        }
                    }
                    var (model, _) = await session.ReadNodeAsync(header, nodeId,
                        reference.NodeClass, !readValues, rawMode, children,
                        ct).ConfigureAwait(false);
                    if (rawMode)
                    {
                        model = model with
                        {
                            BrowseName = reference.BrowseName.AsString(session.MessageContext),
                            DisplayName = reference.DisplayName?.ToString()
                        };
                    }
                    model = model with
                    {
                        TypeDefinitionId = reference.TypeDefinition.AsString(
                            session.MessageContext)
                    };
                    if (targetNodesOnly)
                    {
                        result.Add(new NodeReferenceModel { Target = model });
                        continue;
                    }
                    result.Add(new NodeReferenceModel
                    {
                        ReferenceTypeId = reference.ReferenceTypeId.AsString(
                            session.MessageContext),
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
        private static async Task AddTargetsToBrowseResultAsync(ISessionHandle session,
            RequestHeader header, bool readValues, bool rawMode,
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
                        var (model, _) = await session.ReadNodeAsync(header,
                            nodeId, null, !readValues, rawMode, false,
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
        /// Resolve provided path to node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="rootId"></param>
        /// <param name="paths"></param>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <exception cref="ResourceConflictException"></exception>
        private static async Task<NodeId> ResolveBrowsePathToNodeAsync(
            ISessionHandle session, RequestHeaderModel? header, NodeId rootId,
            string[] paths, string paramName, CancellationToken ct)
        {
            if (paths == null || paths.Length == 0)
            {
                return rootId;
            }
            if (NodeId.IsNull(rootId))
            {
                rootId = ObjectIds.RootFolder;
            }
            var result = new BrowsePathResponseModel
            {
                Targets = new List<NodePathTargetModel>()
            };
            var browsepaths = new BrowsePathCollection {
                new BrowsePath {
                    StartingNode = rootId,
                    RelativePath = paths.ToRelativePath(session.MessageContext)
                }
            };
            var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                header.ToRequestHeader(), browsepaths,
                ct).ConfigureAwait(false);
            System.Diagnostics.Debug.Assert(response != null);
            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browsepaths);
            var count = results[0].Result.Targets?.Count ?? 0;
            if (count == 0)
            {
                throw new ResourceNotFoundException(
                    $"{paramName} did not resolve to any node.");
            }
            if (count != 1)
            {
                throw new ResourceConflictException(
                    $"{paramName} resolved to {count} nodes.");
            }
            return results[0].Result.Targets[0].TargetId
                .ToNodeId(session.MessageContext.NamespaceUris);
        }

        /// <summary>
        /// Reads the first or last date of the archive
        /// (truncates milliseconds).
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="first"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task<DateTime?> HistoryReadTimestampAsync(
            ISessionHandle session, RequestHeaderModel? header, NodeId nodeId,
            bool first, CancellationToken ct)
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
                EndTime = first ? DateTime.MinValue : DateTime.UtcNow.AddDays(1),
                NumValuesPerNode = 1,
                IsReadModified = false,
                ReturnBounds = false
            });
            var response = await session.Services.HistoryReadAsync(header.ToRequestHeader(),
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
                    nodesToRead = new HistoryReadValueIdCollection {
                        new HistoryReadValueId {
                            NodeId = nodeId,
                            ContinuationPoint = results[0].Result.ContinuationPoint
                        }
                    };
                    await session.Services.HistoryReadAsync(header.ToRequestHeader(),
                        details, Opc.Ua.TimestampsToReturn.Source, true,
                        nodesToRead, ct).ConfigureAwait(false);
                }
            }
        }

        private readonly ILogger _logger;
        private readonly ISessionProvider<T> _client;
        private static readonly System.Diagnostics.ActivitySource kActivity =
            new(typeof(NodeServices<T>).FullName!);
    }
}
