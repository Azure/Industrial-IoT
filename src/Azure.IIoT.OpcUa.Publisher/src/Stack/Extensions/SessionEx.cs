// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using NodeClass = Publisher.Models.NodeClass;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session Handle extensions
    /// </summary>
    public static class SessionEx
    {
        /// <summary>
        /// Read attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeIds"></param>
        /// <param name="attributeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(T?, ServiceResultModel?)> ReadAttributeAsync<T>(
            this IOpcUaSession session, RequestHeader header, NodeId nodeIds,
            uint attributeId, CancellationToken ct = default)
        {
            var attributes = await session.ReadAttributeAsync<T>(header,
                nodeIds.YieldReturn(), attributeId, ct).ConfigureAwait(false);
            return attributes.SingleOrDefault();
        }

        /// <summary>
        /// Read attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeIds"></param>
        /// <param name="attributeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<IEnumerable<(T?, ServiceResultModel?)>> ReadAttributeAsync<T>(
            this IOpcUaSession session, RequestHeader header, IEnumerable<NodeId> nodeIds,
            uint attributeId, CancellationToken ct = default)
        {
            var itemsToRead = new ReadValueIdCollection(nodeIds.Select(nodeId => new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = attributeId
            }));
            if (itemsToRead.Count == 0)
            {
                return Enumerable.Empty<(T?, ServiceResultModel?)>();
            }
            var response = await session.Services.ReadAsync(header,
                0, Opc.Ua.TimestampsToReturn.Neither, itemsToRead, ct).ConfigureAwait(false);
            var results = response.Validate(response.Results,
                s => s.StatusCode, response.DiagnosticInfos, itemsToRead);

            if (results.ErrorInfo != null)
            {
                return (default(T), (ServiceResultModel?)results.ErrorInfo).YieldReturn();
            }
            return results.Select(result =>
            {
                var errorInfo = result.ErrorInfo;
                var value = result.ErrorInfo != null ? default :
                    result.Result.GetValue<T?>(default);
                return (value, errorInfo);
            });
        }

        /// <summary>
        /// Read attributes grouped by node id
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeIds"></param>
        /// <param name="attributeIds"></param>
        /// <param name="results"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> ReadAttributesAsync(
            this IOpcUaSession session, RequestHeader header,
            IEnumerable<NodeId> nodeIds, IEnumerable<uint> attributeIds,
            Dictionary<NodeId, Dictionary<uint, DataValue>> results,
            CancellationToken ct = default)
        {
            var itemsToRead = new ReadValueIdCollection(nodeIds
                .SelectMany(nodeId => attributeIds
                    .Select(attributeId =>
                        new ReadValueId
                        {
                            NodeId = nodeId,
                            AttributeId = attributeId
                        })));
            if (itemsToRead.Count == 0)
            {
                return null;
            }
            var response = await session.Services.ReadAsync(header,
                0, Opc.Ua.TimestampsToReturn.Neither, itemsToRead,
                ct).ConfigureAwait(false);
            var readresults = response.Validate(response.Results,
                s => s.StatusCode, response.DiagnosticInfos, itemsToRead);
            if (readresults.ErrorInfo != null)
            {
                return readresults.ErrorInfo;
            }
            foreach (var group in readresults.GroupBy(g => g.Request.NodeId))
            {
                results.AddOrUpdate(group.Key,
                    group.ToDictionary(r => r.Request.AttributeId, r => r.Result));
            }
            return null;
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public static async Task<(DataValue?, ServiceResultModel?)> ReadValueAsync(
            this IOpcUaSession session, RequestHeader header,
            NodeId nodeId, CancellationToken ct = default)
        {
            var itemsToRead = new ReadValueIdCollection {
                new ReadValueId {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                }
            };
            var response = await session.Services.ReadAsync(header,
                0, Opc.Ua.TimestampsToReturn.Both, itemsToRead,
                ct).ConfigureAwait(false);
            var results = response.Validate(response.Results,
                s => s.StatusCode, response.DiagnosticInfos, itemsToRead);

            var errorInfo = results.ErrorInfo ?? results[0].ErrorInfo;
            var value = results.ErrorInfo != null ? null : results[0].Result;
            return (value, errorInfo);
        }

        /// <summary>
        /// Path result returned by browse path resolver.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="ErrorInfo"></param>
        internal record class PathResult(RelativePath Path, ServiceResultModel? ErrorInfo);

        /// <summary>
        /// Get all browse paths for the nodes provided from the root folder object that
        /// organizes the address space.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<IReadOnlyList<PathResult>> GetBrowsePathsFromRootAsync(
            this IOpcUaSession session, RequestHeader requestHeader, IEnumerable<NodeId> nodes,
            CancellationToken ct = default)
        {
            //
            // The semantic of HierarchicalReferences is to denote that References of
            // HierarchicalReferences span a hierarchy. It means that it may be useful
            // to present Nodes related with References of this type in a hierarchical-like
            // way. HierarchicalReferences does not forbid loops. For example, starting
            // from Node A and following HierarchicalReferences it may be possible to
            // browse to Node A, again. Technically we only care about HasChild references
            // as well as Organizes, but we try and follow all paths to the root path
            // and backtrack if we do not get to it.
            //
            var browse = nodes.Select(nodeId => new BrowseDescription
            {
                BrowseDirection = Opc.Ua.BrowseDirection.Inverse,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeId = nodeId,
                Handle = new PathResult(new RelativePath(), null),
                NodeClassMask = 0u,
                ResultMask =
                      (uint)BrowseResultMask.BrowseName
                    | (uint)BrowseResultMask.ReferenceTypeId
            }).ToList();

            if (browse.Count == 0)
            {
                return Array.Empty<PathResult>();
            }

            // Here we keep track of the paths we are exploring to allow us to backtrack
            var searchContext = browse.ToDictionary(b => b,
                _ => new Stack<(Queue<ReferenceDescription> Next, HashSet<ExpandedNodeId> Seen)>());

            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
            foreach (var batch in searchContext.Keys.Batch(limits.GetMaxNodesPerRead()))
            {
                var response = await session.Services.ReadAsync(requestHeader,
                    0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                        batch.Select(b => new ReadValueId
                        {
                            NodeId = b.NodeId,
                            AttributeId = (uint)NodeAttribute.BrowseName
                        })), ct).ConfigureAwait(false);
                var readResults = response.Validate(response.Results,
                    s => s.StatusCode, response.DiagnosticInfos, batch);

                // Fail all
                if (readResults.ErrorInfo != null)
                {
                    return searchContext.Keys.Select(b => ((PathResult)b.Handle) with
                    {
                        ErrorInfo = readResults.ErrorInfo
                    }).ToList();
                }
                foreach (var result in readResults)
                {
                    var path = (PathResult)result.Request.Handle;
                    path.Path.Elements.Add(new RelativePathElement
                    {
                        IsInverse = false,
                        IncludeSubtypes = false,
                        TargetName = result.Result.Value as QualifiedName
                    });
                }
            }

            while (searchContext.Count != 0)
            {
                var results = session.BrowseAsync(requestHeader, null,
                    new BrowseDescriptionCollection(searchContext.Keys), ct).ConfigureAwait(false);
                await foreach (var result in results)
                {
                    if (result.Description == null)
                    {
                        // Fail all
                        return browse.ConvertAll(b => ((PathResult)b.Handle) with
                        {
                            ErrorInfo = result.ErrorInfo ?? new ServiceResultModel
                            {
                                StatusCode = StatusCodes.BadNotFound
                            }
                        });
                    }

                    var pathsFromNode = searchContext[result.Description];
                    var path = (PathResult)result.Description.Handle;

                    ReferenceDescription? reference = null;
                    if (result.References != null)
                    {
                        if (result.References.Any(r => r.NodeId == ObjectIds.RootFolder))
                        {
                            //
                            // we reached the root folder and are now done. There could be
                            // alternative paths to the root but we do not care about those.
                            //
                            searchContext.Remove(result.Description);
                            continue;
                        }

                        //
                        // Filter any nodes we have already seen on our journey and then
                        // filter any nodes that are external. Then we do some weighing,
                        // e.g., prefer Organizes to HasChild (HasProperty, HasComponent)
                        // to HasEventSource (HasNotifier)
                        //
                        var references = result.References
                            .Where(r => r.NodeId.ServerIndex == 0 &&
                                !pathsFromNode.Any(p => p.Seen.Contains(r.NodeId)))
                            .OrderBy(r => r.ReferenceTypeId)
                            ;

                        var queue = new Queue<ReferenceDescription>(references);
                        if (queue.Count > 0)
                        {
                            pathsFromNode.Push((queue, new HashSet<ExpandedNodeId>()));

                            reference = pathsFromNode.Peek().Next.Dequeue();
                            pathsFromNode.Peek().Seen.Add(reference.NodeId);
                        }
                    }

                    if (reference == null)
                    {
                        //
                        // Wrong path taken see if there are alternatives to get to root
                        // Backtrack the path elements and try to find a new route
                        //
                        path.Path.Elements.RemoveAt(0);
                        while (pathsFromNode.Count > 0)
                        {
                            var alternativeReferences = pathsFromNode.Peek().Next;
                            if (alternativeReferences.Count != 0)
                            {
                                reference = alternativeReferences.Dequeue();
                                pathsFromNode.Peek().Seen.Add(reference.NodeId);
                                break;
                            }

                            // All paths at this level exhausted - backtrack a level.
                            path.Path.Elements.RemoveAt(0);
                            path.Path.Elements[0].ReferenceTypeId = null;
                            pathsFromNode.Pop();
                        }

                        if (reference == null)
                        {
                            // No way to get to root.  This should not happen.
                            searchContext.Remove(result.Description);
                            result.Description.Handle = path with
                            {
                                ErrorInfo = result.ErrorInfo ?? new ServiceResultModel
                                {
                                    StatusCode = StatusCodes.BadNotFound
                                }
                            };
                            continue;
                        }
                    }

                    path.Path.Elements[0].ReferenceTypeId = reference.ReferenceTypeId;
                    path.Path.Elements.Insert(0, new RelativePathElement
                    {
                        IsInverse = false,
                        IncludeSubtypes = false,
                        TargetName = reference.BrowseName
                    });
                    result.Description.NodeId = reference.NodeId.ToNodeId(
                        session.MessageContext.NamespaceUris);
                }
            }
            return browse.ConvertAll(b => (PathResult)b.Handle);
        }

        /// <summary>
        /// Read all attributes from node
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="skipValueRead"></param>
        /// <param name="nodeClass"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResponse<uint, DataValue>> ReadNodeAttributesAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId nodeId,
            bool skipValueRead = false, Opc.Ua.NodeClass? nodeClass = null,
            CancellationToken ct = default)
        {
            if (nodeClass == null || nodeClass.Value == Opc.Ua.NodeClass.Unspecified)
            {
                // First read node class
                var nodeClassRead = new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = nodeId,
                        AttributeId = Attributes.NodeClass
                    }
                };
                var response = await session.Services.ReadAsync(requestHeader, 0,
                    Opc.Ua.TimestampsToReturn.Both, nodeClassRead,
                    ct).ConfigureAwait(false);

                var readResults = response.Validate(response.Results,
                    s => s.StatusCode, response.DiagnosticInfos, nodeClassRead);
                nodeClass = readResults.ErrorInfo != null ? Opc.Ua.NodeClass.Unspecified :
                    readResults[0].Result.GetValueOrDefault<Opc.Ua.NodeClass>();
            }
            if (nodeClass == Opc.Ua.NodeClass.VariableType)
            {
                skipValueRead = false; // read default values
            }
            var attributes = TypeMaps.Attributes.Value.Identifiers
                .Where(a => !skipValueRead || a != Attributes.Value);
            var readValueCollection = new ReadValueIdCollection(attributes
                .Select(a => new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = a
                }));
            var readResponse = await session.Services.ReadAsync(requestHeader, 0,
                Opc.Ua.TimestampsToReturn.Both, readValueCollection,
                ct).ConfigureAwait(false);

            var results = readResponse.Validate(readResponse.Results, s => s.StatusCode,
                readResponse.DiagnosticInfos, attributes);
            var errorInfo = results.ErrorInfo ?? results[0].ErrorInfo;
            if (errorInfo != null)
            {
                return results;
            }
            // Fix up responses based on node class
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].ErrorInfo?.StatusCode ==
                        StatusCodes.BadAttributeIdInvalid)
                {
                    // Update result with default and set status to good.
                    readResponse.Results[i].Value = AttributeMap.GetDefaultValue(
                        nodeClass.Value, results[i].Request, true);
                    readResponse.Results[i].StatusCode = StatusCodes.Good;
                }
            }
            return readResponse.Validate(readResponse.Results, s => s.StatusCode,
                readResponse.DiagnosticInfos, attributes);
        }

        /// <summary>
        /// Read node state
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeState"></param>
        /// <param name="rootId"></param>
        /// <param name="relativePath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> ReadNodeStateAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeState nodeState,
            NodeId rootId, RelativePath? relativePath = null, CancellationToken ct = default)
        {
            var valuesToRead = new List<ReadValueId>();
            var objectsToBrowse = new List<BrowseDescription>();
            var resolveBrowsePaths = session.GetBrowsePathFromNodeState(rootId,
                nodeState, relativePath);
            if (resolveBrowsePaths.Count == 0)
            {
                // Nothing to do
                return ((StatusCode)StatusCodes.GoodNoData).CreateResultModel();
            }
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
            var resolveBrowsePathsBatches = resolveBrowsePaths
                .Batch(limits.GetMaxNodesPerTranslatePathsToNodeIds());
            foreach (var batch in resolveBrowsePathsBatches)
            {
                // translate browse paths.
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    requestHeader, new BrowsePathCollection(batch), ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, batch);
                if (results.ErrorInfo != null)
                {
                    return results.ErrorInfo;
                }
                foreach (var result in results)
                {
                    if (result.Request.Handle is not NodeState node)
                    {
                        continue;
                    }
                    if (StatusCode.IsBad(result.StatusCode))
                    {
                        if (result.StatusCode.Code is StatusCodes.BadNodeIdUnknown or
                            StatusCodes.BadUnexpectedError)
                        {
                            return result.ErrorInfo;
                        }
                        if (node is BaseVariableState v)
                        {
                            // Initialize the variable
                            v.Value = null;
                            v.StatusCode = result.StatusCode;
                        }
                        continue;
                    }
                    if (result.Result.Targets.Count == 1 &&
                        result.Result.Targets[0].RemainingPathIndex == uint.MaxValue &&
                        !result.Result.Targets[0].TargetId.IsAbsolute)
                    {
                        node.NodeId = (NodeId)result.Result.Targets[0].TargetId;
                    }
                    else
                    {
                        if (node is BaseVariableState v)
                        {
                            // Initialize the variable
                            v.Value = null;
                            v.StatusCode = StatusCodes.BadNotSupported;
                        }
                        continue;
                    }
                    switch (node)
                    {
                        case BaseVariableState variable:
                            // Initialize the variable
                            variable.Value = null;
                            variable.StatusCode = StatusCodes.BadNotSupported;
                            valuesToRead.Add(new ReadValueId
                            {
                                NodeId = node.NodeId,
                                AttributeId = Attributes.Value,
                                Handle = node
                            });
                            break;
                        case FolderState folder:
                            // Save for browsing
                            objectsToBrowse.Add(new BrowseDescription
                            {
                                BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                                Handle = folder,
                                IncludeSubtypes = true,
                                ReferenceTypeId = ReferenceTypeIds.Organizes,
                                NodeClassMask =
                                    (uint)Opc.Ua.NodeClass.Variable |
                                    (uint)Opc.Ua.NodeClass.Object,
                                NodeId = node.NodeId,
                                ResultMask = (uint)BrowseResultMask.All
                            });
                            break;
                    }
                }
            }

            if (objectsToBrowse.Count > 0)
            {
                foreach (var batch in objectsToBrowse.Batch(limits.GetMaxNodesPerBrowse()))
                {
                    // Browse folders with objects and variables in it
                    var browseResults = session.BrowseAsync(requestHeader, null,
                        new BrowseDescriptionCollection(batch), ct).ConfigureAwait(false);
                    await foreach (var (description, references, errorInfo) in browseResults)
                    {
                        var obj = (BaseObjectState?)description?.Handle;
                        if (obj == null || references == null)
                        {
                            continue;
                        }
                        foreach (var reference in references)
                        {
                            switch (reference.NodeClass)
                            {
                                case Opc.Ua.NodeClass.Variable:
                                    // Add variable to the folder and set it to be read.
                                    var variable = new BaseDataVariableState(obj)
                                    {
                                        NodeId = (NodeId)reference.NodeId,
                                        BrowseName = reference.BrowseName,
                                        DisplayName = reference.DisplayName,
                                        StatusCode = StatusCodes.BadNotSupported,
                                        ReferenceTypeId = reference.ReferenceTypeId,
                                        Value = null
                                    };
                                    obj.AddChild(variable);
                                    valuesToRead.Add(new ReadValueId
                                    {
                                        NodeId = variable.NodeId,
                                        AttributeId = Attributes.Value,
                                        Handle = variable
                                    });
                                    break;
                                case Opc.Ua.NodeClass.Object:
                                    // Add object
#pragma warning disable CA2000 // Dispose objects before losing scope
                                    var child = new BaseObjectState(obj)
                                    {
                                        NodeId = (NodeId)reference.NodeId,
                                        BrowseName = reference.BrowseName,
                                        DisplayName = reference.DisplayName,
                                        ReferenceTypeId = reference.ReferenceTypeId
                                    };
#pragma warning restore CA2000 // Dispose objects before losing scope
                                    obj.AddChild(child);
                                    break;
                            }
                        }
                    }
                }
            }

            if (valuesToRead.Count > 0)
            {
                foreach (var batch in valuesToRead.Batch(limits.GetMaxNodesPerRead()))
                {
                    // read the values.
                    var readResponse = await session.Services.ReadAsync(
                        requestHeader, 0, Opc.Ua.TimestampsToReturn.Neither,
                        new ReadValueIdCollection(batch), ct).ConfigureAwait(false);
                    var readResults = readResponse.Validate(readResponse.Results,
                        s => s.StatusCode, readResponse.DiagnosticInfos,
                        batch);
                    if (readResults.ErrorInfo != null)
                    {
                        return readResults.ErrorInfo;
                    }
                    foreach (var readResult in readResults)
                    {
                        var variable = (BaseVariableState)readResult.Request.Handle;
                        variable.WrappedValue = readResult.Result.WrappedValue;
                        variable.DataType = TypeInfo.GetDataTypeId(readResult.Result.Value);
                        variable.StatusCode = readResult.Result.StatusCode;
                    }
                }
                return null;
            }
            return ((StatusCode)StatusCodes.BadUnexpectedError).CreateResultModel();
        }

        /// <summary>
        /// Browses the address space and returns all of the
        /// supertypes of the specified type node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="typeId"></param>
        /// <param name="hierarchy"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task CollectTypeHierarchyAsync(this IOpcUaSession session,
            RequestHeader header, NodeId typeId, IList<(NodeId, ReferenceDescription)> hierarchy,
            CancellationToken ct = default)
        {
            // find all of the children of the field.
            var nodeToBrowse = new BrowseDescriptionCollection {
                new BrowseDescription {
                    NodeId = typeId,
                    BrowseDirection = Opc.Ua.BrowseDirection.Inverse,
                    ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                    IncludeSubtypes = false,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.All
                }
            };
            while (true)
            {
                var response = await session.Services.BrowseAsync(header, null, 0,
                    nodeToBrowse, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, nodeToBrowse);
                if (results.ErrorInfo != null)
                {
                    break;
                }
                if (results[0].Result.References == null ||
                    results[0].Result.References.Count == 0)
                {
                    // should never be more than one supertype.
                    break;
                }
                var reference = results[0].Result.References[0];
                if (reference.NodeId.IsAbsolute)
                {
                    break;
                }
                hierarchy.Add((nodeToBrowse[0].NodeId, reference));
                nodeToBrowse[0].NodeId = (NodeId)reference.NodeId;
            }
        }

        /// <summary>
        /// Collects the fields for the instance node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="typeId"></param>
        /// <param name="parent"></param>
        /// <param name="instances"></param>
        /// <param name="map"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="ct"></param>
        internal static async Task<ServiceResultModel?> CollectInstanceDeclarationsAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId typeId,
            InstanceDeclarationModel? parent, List<InstanceDeclarationModel> instances,
            IDictionary<ImmutableRelativePath, InstanceDeclarationModel> map,
            NamespaceFormat namespaceFormat, Opc.Ua.NodeClass? nodeClassMask = null,
            CancellationToken ct = default)
        {
            // find the children of the type.
            var nodeToBrowse = new BrowseDescriptionCollection
            {
                new BrowseDescription
                {
                    NodeId = parent == null ? typeId : parent.NodeId.ToNodeId(session.MessageContext),
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HasChild,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)Opc.Ua.NodeClass.Object |
                        (((uint?)nodeClassMask)
                            ?? (uint)Opc.Ua.NodeClass.Variable | (uint)Opc.Ua.NodeClass.Method),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };
            var browseresults = session.BrowseAsync(requestHeader, null, nodeToBrowse, ct);
            await foreach (var result in browseresults.ConfigureAwait(false))
            {
                if (result.ErrorInfo != null)
                {
                    return result.ErrorInfo;
                }
                if (result.References == null)
                {
                    continue;
                }
                var references = result.References
                    .Where(r => !r.NodeId.IsAbsolute)
                    .ToList();
                if (references.Count == 0)
                {
                    continue;
                }

                // find the modelling rules.
                var (targets, errorInfo2) = await session.FindAsync(requestHeader,
                    references.Select(r => (NodeId)r.NodeId),
                    ReferenceTypeIds.HasModellingRule, maxResults: 1, ct: ct).ConfigureAwait(false);
                if (errorInfo2 != null)
                {
                    return errorInfo2;
                }
                var referencesWithRules = targets
                    .Zip(references)
                    .ToList();

                // Get the children.
                var children = new Dictionary<NodeId, InstanceDeclarationModel>();
                foreach (var (modellingRule, reference) in referencesWithRules)
                {
                    var browseName = reference.BrowseName.AsString(session.MessageContext,
                        namespaceFormat);
                    var relativePath = ImmutableRelativePath.Create(parent?.BrowsePath,
                        "/" + browseName);
                    var nodeClass = reference.NodeClass.ToServiceType();
                    if (NodeId.IsNull(modellingRule.Node) || nodeClass == null)
                    {
                        // if the modelling rule is null then the instance is not part
                        // of the type declaration.
                        map.Remove(relativePath);
                        continue;
                    }
                    // create a new declaration.
                    map.TryGetValue(relativePath, out var overriden);

                    var displayName =
                        LocalizedText.IsNullOrEmpty(reference.DisplayName?.Text) ?
                            reference.BrowseName.Name : reference.DisplayName.AsString();
                    var child = new InstanceDeclarationModel
                    {
                        RootTypeId = typeId.AsString(session.MessageContext,
                            namespaceFormat),
                        NodeId = reference.NodeId.AsString(session.MessageContext,
                            namespaceFormat),
                        BrowseName = reference.BrowseName.AsString(session.MessageContext,
                            namespaceFormat),
                        NodeClass = nodeClass.Value,
                        DisplayPath = parent == null ?
                            displayName : $"{parent.DisplayPath}/{displayName}",
                        DisplayName = displayName,
                        BrowsePath = relativePath.Path,
                        ModellingRule = modellingRule.Name.AsString(session.MessageContext,
                            namespaceFormat),
                        ModellingRuleId = modellingRule.Node.AsString(session.MessageContext,
                            namespaceFormat),
                        OverriddenDeclaration = overriden
                    };

                    map[relativePath] = child;

                    // add to list.
                    children.Add((NodeId)reference.NodeId, child);
                }
                // check if nothing more to do.
                if (children.Count == 0)
                {
                    return null;
                }

                // Add variable metadata
                var variables = children
                    .Where(v => v.Value.NodeClass == NodeClass.Variable)
                    .Select(v => v.Key);
                var variableMetadata = new List<VariableMetadataModel>();
                var errorInfo = await session.CollectVariableMetadataAsync(requestHeader,
                    variables, variableMetadata, namespaceFormat, ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return errorInfo;
                }
                foreach (var (nodeId, variable) in variables.Zip(variableMetadata))
                {
                    children[nodeId] = children[nodeId] with { VariableMetadata = variable };
                }

                // Add method metadata
                var methods = children
                    .Where(v => v.Value.NodeClass == NodeClass.Method)
                    .Select(v => v.Key);
                var methodMetadata = new List<MethodMetadataModel>();
                errorInfo = await session.CollectMethodMetadataAsync(requestHeader,
                    variables, methodMetadata, namespaceFormat, ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return errorInfo;
                }
                foreach (var (nodeId, method) in methods.Zip(methodMetadata))
                {
                    children[nodeId] = children[nodeId] with { MethodMetadata = method };
                }

                // Add descriptions
                var attributes = await session.ReadAttributeAsync<LocalizedText>(requestHeader,
                    children.Keys, Attributes.Description, ct).ConfigureAwait(false);
                // TODO: Check attribute error info
                foreach (var (nodeId, description) in children.Keys.Zip(attributes))
                {
                    children[nodeId] = children[nodeId] with
                    {
                        Description = description.Item1.AsString()
                    };
                }

                // recusively collect instance declarations for the tree below.
                foreach (var child in children.Values)
                {
                    instances.Add(child);
                    await session.CollectInstanceDeclarationsAsync(requestHeader,
                        typeId, child, instances, map, namespaceFormat, ct: ct).ConfigureAwait(false);
                }
            }
            return null;
        }

        /// <summary>
        /// Get method metadata
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(VariableMetadataModel?, ServiceResultModel?)> GetVariableMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId nodeId,
            NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            var results = new List<VariableMetadataModel>();
            var errorInfo = await session.CollectVariableMetadataAsync(requestHeader,
                nodeId.YieldReturn(), results, namespaceFormat, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return (null, errorInfo);
            }
            return (results.Single(), null);
        }

        /// <summary>
        /// Get method metadata
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeIds"></param>
        /// <param name="metadata"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> CollectVariableMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, IEnumerable<NodeId> nodeIds,
            List<VariableMetadataModel> metadata, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            if (!nodeIds.Any())
            {
                return null;
            }
            var attributeIds = new uint[] {
                Attributes.DataType,
                Attributes.ArrayDimensions,
                Attributes.ValueRank
            };
            var attributes = new Dictionary<NodeId, Dictionary<uint, DataValue>>();
            var errorInfo = await session.ReadAttributesAsync(requestHeader,
                nodeIds, attributeIds, attributes, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return errorInfo;
            }
            metadata.AddRange(attributes.Select(node => new VariableMetadataModel
            {
                ArrayDimensions = node.Value[Attributes.ArrayDimensions]
                    .GetValueOrDefault<uint[]>()?.ToList(),
                DataType = new DataTypeMetadataModel
                {
                    DataType = node.Value[Attributes.DataType].GetValueOrDefault<NodeId>()?
                        .AsString(session.MessageContext, namespaceFormat)
                },
                ValueRank = (NodeValueRank?)node.Value[Attributes.ValueRank]
                    .GetValueOrDefault<int?>(v => v == ValueRanks.Any ? null : v)
            }));
            return null;
        }

        /// <summary>
        /// Get method metadata
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(MethodMetadataModel?, ServiceResultModel?)> GetMethodMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId nodeId,
            NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            var results = new List<MethodMetadataModel>();
            var errorInfo = await session.CollectMethodMetadataAsync(requestHeader,
                nodeId.YieldReturn(), results, namespaceFormat, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return (null, errorInfo);
            }
            return (results.Single(), null);
        }

        /// <summary>
        /// Get method metadata
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeIds"></param>
        /// <param name="metadata"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> CollectMethodMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, IEnumerable<NodeId> nodeIds,
            List<MethodMetadataModel> metadata, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            if (!nodeIds.Any())
            {
                return null;
            }
            var browseDescriptions = new BrowseDescriptionCollection(nodeIds.Select(nodeId =>
                new BrowseDescription
                {
                    BrowseDirection = Opc.Ua.BrowseDirection.Both,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    NodeId = nodeId,
                    ReferenceTypeId = ReferenceTypeIds.Aggregates,
                    ResultMask = (uint)BrowseResultMask.All
                }
            ));
            var response = await session.Services.BrowseAsync(requestHeader,
                null, 0, browseDescriptions, ct).ConfigureAwait(false);

            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browseDescriptions);
            if (results.ErrorInfo != null)
            {
                return results.ErrorInfo;
            }
            foreach (var result in results)
            {
                if (result.ErrorInfo != null)
                {
                    return result.ErrorInfo;
                }
                var continuationPoint = result.Result.ContinuationPoint;
                var references = result.Result.References;
                IReadOnlyList<MethodMetadataArgumentModel>? outputArguments = null;
                IReadOnlyList<MethodMetadataArgumentModel>? inputArguments = null;
                string? objectId = null;
                foreach (var nodeReference in references)
                {
                    if (outputArguments != null &&
                        inputArguments != null &&
                        !string.IsNullOrEmpty(objectId))
                    {
                        break;
                    }
                    if (!nodeReference.IsForward)
                    {
                        if (nodeReference.ReferenceTypeId == ReferenceTypeIds.HasComponent)
                        {
                            objectId = nodeReference.NodeId.AsString(session.MessageContext, namespaceFormat);
                        }
                        continue;
                    }
                    var isInput = nodeReference.BrowseName == BrowseNames.InputArguments;
                    if (!isInput && nodeReference.BrowseName != BrowseNames.OutputArguments)
                    {
                        continue;
                    }

                    var node = nodeReference.NodeId.ToNodeId(session.MessageContext.NamespaceUris);
                    var (value, _) = await session.ReadValueAsync(requestHeader, node,
                        ct).ConfigureAwait(false);
                    if (value?.Value is not ExtensionObject[] argumentsList)
                    {
                        continue;
                    }

                    var argList = new List<MethodMetadataArgumentModel>();
                    foreach (var argument in argumentsList.Select(a => (Argument)a.Body))
                    {
                        var (dataTypeIdNode, _) = await session.ReadNodeAsync(requestHeader,
                            argument.DataType, null, false, false, namespaceFormat, false, ct).ConfigureAwait(false);
                        var arg = new MethodMetadataArgumentModel
                        {
                            Name = argument.Name,
                            DefaultValue = argument.Value == null ? VariantValue.Null :
                                session.Codec.Encode(new Variant(argument.Value), out var tmp),
                            ValueRank = argument.ValueRank == ValueRanks.Scalar ?
                                null : (NodeValueRank)argument.ValueRank,
                            ArrayDimensions = argument.ArrayDimensions?.ToList(),
                            Description = argument.Description?.ToString(),
                            Type = dataTypeIdNode
                        };
                        argList.Add(arg);
                    }
                    if (isInput)
                    {
                        inputArguments = argList;
                    }
                    else
                    {
                        outputArguments = argList;
                    }
                }
                metadata.Add(new MethodMetadataModel
                {
                    InputArguments = inputArguments,
                    OutputArguments = outputArguments,
                    ObjectId = objectId
                });
            }
            return null;
        }

        /// <summary>
        /// Read node properties as node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="nodeClass"></param>
        /// <param name="skipValue"></param>
        /// <param name="rawMode"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="children"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(NodeModel, ServiceResultModel?)> ReadNodeAsync(
            this IOpcUaSession session, RequestHeader header, NodeId nodeId,
            Opc.Ua.NodeClass? nodeClass, bool skipValue, bool rawMode, NamespaceFormat namespaceFormat,
            bool? children = null, CancellationToken ct = default)
        {
            if (rawMode)
            {
                var id = nodeId.AsString(session.MessageContext, namespaceFormat);
                System.Diagnostics.Debug.Assert(id != null);
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(id));
                return (new NodeModel
                {
                    NodeId = id,
                    NodeClass = nodeClass?.ToServiceType()
                }, null);
            }
            return await session.ReadNodeAsync(header, nodeId, namespaceFormat,
                nodeClass, skipValue, children, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="nodeClass"></param>
        /// <param name="skipValue"></param>
        /// <param name="children"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(NodeModel, ServiceResultModel?)> ReadNodeAsync(
            this IOpcUaSession session, RequestHeader header, NodeId nodeId, NamespaceFormat namespaceFormat,
            Opc.Ua.NodeClass? nodeClass = null, bool skipValue = true, bool? children = null,
            CancellationToken ct = default)
        {
            var results = await session.ReadNodeAttributesAsync(header, nodeId, skipValue,
                nodeClass, ct).ConfigureAwait(false);
            var lookup = results.AsLookupTable();

            var id = nodeId.AsString(session.MessageContext, namespaceFormat);
            System.Diagnostics.Debug.Assert(id != null);
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(id));
            lookup.TryGetValue(Attributes.Value, out var value);
            var nodeModel = new NodeModel
            {
                Children = children,
                NodeId = id,
                Value = value.Item1 == null ? null :
                    session.Codec.Encode(
                        value.Item1.WrappedValue, out var type),
                SourceTimestamp =
                    value.Item1?.SourceTimestamp,
                SourcePicoseconds =
                    value.Item1?.SourcePicoseconds,
                ServerTimestamp =
                    value.Item1?.ServerTimestamp,
                ServerPicoseconds =
                    value.Item1?.ServerPicoseconds,
                ErrorInfo =
                    value.Item2,
                BrowseName =
                    lookup[Attributes.BrowseName].Item1?
                        .GetValueOrDefault<QualifiedName>()?
                        .AsString(session.MessageContext, namespaceFormat),
                DisplayName =
                    lookup[Attributes.DisplayName].Item1?
                        .GetValueOrDefault<LocalizedText>()?
                        .ToString(),
                Description =
                    lookup[Attributes.Description].Item1?
                        .GetValueOrDefault<LocalizedText>()?
                        .ToString(),
                NodeClass =
                    lookup[Attributes.NodeClass].Item1?
                        .GetValueOrDefault<Opc.Ua.NodeClass>()
                        .ToServiceType(),
                AccessRestrictions = (NodeAccessRestrictions?)
                    lookup[Attributes.AccessRestrictions].Item1?
                        .GetValueOrDefault<ushort?>(v => v == 0 ? null : v),
                UserWriteMask =
                    lookup[Attributes.UserWriteMask].Item1?
                        .GetValueOrDefault<uint?>(),
                WriteMask =
                    lookup[Attributes.WriteMask].Item1?
                        .GetValueOrDefault<uint?>(),
                DataType =
                    lookup[Attributes.DataType].Item1?
                        .GetValueOrDefault<NodeId>()?
                        .AsString(session.MessageContext, namespaceFormat),
                ArrayDimensions =
                    lookup[Attributes.ArrayDimensions].Item1?
                        .GetValueOrDefault<uint[]?>(),
                ValueRank = (NodeValueRank?)
                    lookup[Attributes.ValueRank].Item1?
                        .GetValueOrDefault<int?>(),
                AccessLevel = (NodeAccessLevel?)
                    lookup[Attributes.AccessLevelEx].Item1?
                        .GetValueOrDefault<uint?>(l =>
                        {
                            // Or both if available
                            var v = (l ?? 0) |
                            lookup[Attributes.AccessLevel].Item1?
                                .GetValueOrDefault<byte?>(b => b ?? 0);
                            return v == 0 ? null : v;
                        }),
                UserAccessLevel = (NodeAccessLevel?)
                    lookup[Attributes.UserAccessLevel].Item1?
                        .GetValueOrDefault<byte?>(),
                Historizing =
                    lookup[Attributes.Historizing].Item1?
                        .GetValueOrDefault<bool?>(),
                MinimumSamplingInterval =
                    lookup[Attributes.MinimumSamplingInterval].Item1?
                        .GetValueOrDefault<double?>(),
                IsAbstract =
                    lookup[Attributes.IsAbstract].Item1?
                        .GetValueOrDefault<bool?>(),
                EventNotifier = (NodeEventNotifier?)
                    lookup[Attributes.EventNotifier].Item1?
                        .GetValueOrDefault<byte?>(v => v == 0 ? null : v),
                DataTypeDefinition = session.Codec.Encode(
                    lookup[Attributes.DataTypeDefinition].Item1?
                        .GetValueOrDefault<ExtensionObject>(), out _),
                InverseName =
                    lookup[Attributes.InverseName].Item1?
                        .GetValueOrDefault<LocalizedText>()?
                        .ToString(),
                Symmetric =
                    lookup[Attributes.Symmetric].Item1?
                        .GetValueOrDefault<bool?>(),
                ContainsNoLoops =
                    lookup[Attributes.ContainsNoLoops].Item1?
                        .GetValueOrDefault<bool?>(),
                Executable =
                    lookup[Attributes.Executable].Item1?
                        .GetValueOrDefault<bool?>(),
                UserExecutable =
                    lookup[Attributes.UserExecutable].Item1?
                        .GetValueOrDefault<bool?>(),
                UserRolePermissions =
                    lookup[Attributes.UserRolePermissions].Item1?
                        .GetValueOrDefault<ExtensionObject[]>()?
                        .Select(ex => ex.Body)
                        .OfType<RolePermissionType>()
                        .Select(p => p.ToServiceModel(session.MessageContext, namespaceFormat)).ToList(),
                RolePermissions =
                    lookup[Attributes.RolePermissions].Item1?
                        .GetValueOrDefault<ExtensionObject[]>()?
                        .Select(ex => ex.Body)
                        .OfType<RolePermissionType>()
                        .Select(p => p.ToServiceModel(session.MessageContext, namespaceFormat)).ToList()
            };
            return (nodeModel, results.ErrorInfo ?? lookup[Attributes.NodeClass].Item2);
        }

        /// <summary>
        /// Find results
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Node"></param>
        /// <param name="TypeDefinition"></param>
        /// <param name="ErrorInfo"></param>
        internal record struct FindResult(QualifiedName Name, NodeId Node,
            ExpandedNodeId TypeDefinition, ServiceResultModel? ErrorInfo = null);

        /// <summary>
        /// Finds the targets for the specified reference.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeIds"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubTypes"></param>
        /// <param name="isInverse"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(IReadOnlyList<FindResult>, ServiceResultModel?)> FindAsync(
            this IOpcUaSession session, RequestHeader requestHeader,
            IEnumerable<NodeId> nodeIds, NodeId referenceTypeId, bool includeSubTypes = false,
            bool isInverse = false, uint nodeClassMask = 0, uint? maxResults = null,
            CancellationToken ct = default)
        {
            // construct browse request.
            var nodesToBrowse = new BrowseDescriptionCollection(nodeIds
                .Select(nodeId => new BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = isInverse ?
                        Opc.Ua.BrowseDirection.Inverse : Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeId = referenceTypeId,
                    IncludeSubtypes = includeSubTypes,
                    NodeClassMask = nodeClassMask,
                    ResultMask =
                        (uint)BrowseResultMask.BrowseName |
                        (uint)BrowseResultMask.TypeDefinition
                }));

            var continuationPoints = new ByteStringCollection();
            try
            {
                var response = await session.Services.BrowseAsync(requestHeader, null,
                    maxResults ?? 0u, nodesToBrowse, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, nodesToBrowse);
                var targetIds = new List<FindResult>();
                if (results.ErrorInfo != null)
                {
                    return (targetIds, results.ErrorInfo);
                }

                foreach (var result in results)
                {
                    // check for error.
                    if (result.ErrorInfo != null)
                    {
                        targetIds.Add(new FindResult(QualifiedName.Null, NodeId.Null,
                            ExpandedNodeId.Null, result.ErrorInfo));
                        continue;
                    }
                    // check for continuation point.
                    if (result.Result.ContinuationPoint?.Length > 0)
                    {
                        continuationPoints.Add(result.Result.ContinuationPoint);
                    }
                    if (!Extract(targetIds, result.Result.References))
                    {
                        break;
                    }
                }

                while (continuationPoints.Count > 0 && !maxResults.HasValue)
                {
                    var next = await session.Services.BrowseNextAsync(requestHeader, false,
                        continuationPoints, ct).ConfigureAwait(false);
                    var nextResults = next.Validate(next.Results, s => s.StatusCode,
                                        next.DiagnosticInfos, continuationPoints);
                    if (nextResults.ErrorInfo != null)
                    {
                        return (targetIds, nextResults.ErrorInfo);
                    }

                    continuationPoints = new ByteStringCollection();
                    foreach (var result in nextResults)
                    {
                        // check for error.
                        if (result.ErrorInfo != null)
                        {
                            targetIds.Add(new FindResult(QualifiedName.Null, NodeId.Null,
                                ExpandedNodeId.Null, result.ErrorInfo));
                            continue;
                        }
                        // check for continuation point.
                        if (result.Result.ContinuationPoint?.Length > 0)
                        {
                            continuationPoints.Add(result.Result.ContinuationPoint);
                        }
                        if (!Extract(targetIds, result.Result.References))
                        {
                            break;
                        }
                    }
                }
                return (targetIds, null);
            }
            finally
            {
                // release continuation points.
                if (continuationPoints.Count > 0)
                {
                    try
                    {
                        await session.Services.BrowseNextAsync(requestHeader, true,
                            continuationPoints, ct).ConfigureAwait(false);
                    }
                    catch { }
                }
            }

            static bool Extract(List<FindResult> targetIds, ReferenceDescriptionCollection references)
            {
                // get the node ids.
                foreach (var reference in references)
                {
                    if (NodeId.IsNull(reference.NodeId) ||
                        reference.NodeId.IsAbsolute)
                    {
                        targetIds.Add(new FindResult(QualifiedName.Null, NodeId.Null, ExpandedNodeId.Null,
                            new ServiceResultModel { ErrorMessage = "Target node is null or absolute" }));
                        continue;
                    }
                    targetIds.Add(new FindResult(reference.BrowseName,
                         (NodeId)reference.NodeId,
                         reference.TypeDefinition));
                }
                return true;
            }
        }

        /// <summary>
        /// Helper struct return
        /// </summary>
        /// <param name="Description"></param>
        /// <param name="References"></param>
        /// <param name="ErrorInfo"></param>
        internal record struct BrowseResult(BrowseDescription? Description,
            ReferenceDescriptionCollection? References, ServiceResultModel? ErrorInfo);

        /// <summary>
        /// Enumerates browse results inline
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async IAsyncEnumerable<BrowseResult> BrowseAsync(
            this IOpcUaSession session, RequestHeader requestHeader,
            ViewDescription? view, BrowseDescriptionCollection nodesToBrowse,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
            var maxContinuationPoints = limits.GetMaxBrowseContinuationPoints();
            foreach (var nodesToBrowseBatch in nodesToBrowse.Batch(limits.GetMaxNodesPerBrowse()))
            {
                var browseDescriptions = new BrowseDescriptionCollection(nodesToBrowseBatch);
                var firstResponse = await session.Services.BrowseAsync(requestHeader, view,
                    0, browseDescriptions, ct).ConfigureAwait(false);
                var firstResults = firstResponse.Validate(firstResponse.Results,
                    s => s.StatusCode, firstResponse.DiagnosticInfos, browseDescriptions);
                if (firstResults.ErrorInfo != null)
                {
                    yield return new BrowseResult(null, null, firstResults.ErrorInfo);
                }
                var continuationPoints = firstResults
                    .Where(r => r.Result.ContinuationPoint?.Length > 0)
                    .Select(r => (r.Request, r.Result.ContinuationPoint));
                try
                {
                    foreach (var result in firstResults)
                    {
                        yield return new BrowseResult(result.Request,
                            result.Result.References, result.ErrorInfo);
                    }
                    while (true)
                    {
                        var next = continuationPoints.Take(maxContinuationPoints).ToList();
                        if (next.Count == 0)
                        {
                            break;
                        }

                        var nextResponse = await session.Services.BrowseNextAsync(requestHeader,
                            false, new ByteStringCollection(next.Select(r => r.ContinuationPoint)),
                            ct).ConfigureAwait(false);
                        var nextResults = firstResponse.Validate(nextResponse.Results,
                            s => s.StatusCode, nextResponse.DiagnosticInfos, next);

                        if (nextResults.ErrorInfo != null)
                        {
                            yield return new BrowseResult(null, null, nextResults.ErrorInfo);
                        }
                        foreach (var result in nextResults)
                        {
                            yield return new BrowseResult(
                                result.Request.Request, result.Result.References, result.ErrorInfo);
                        }

                        continuationPoints = continuationPoints.Concat(nextResults
                            .Where(r => r.Result.ContinuationPoint?.Length > 0)
                            .Select(r => (r.Request.Request, r.Result.ContinuationPoint)));
                    }
                }
                finally
                {
                    // Release any dangling continuation points
                    foreach (var batch in continuationPoints
                        .Select(r => r.ContinuationPoint)
                        .Batch(maxContinuationPoints))
                    {
                        await session.Services.BrowseNextAsync(requestHeader,
                            true, new ByteStringCollection(batch), ct).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively collects the variables in a NodeState and
        /// returns a collection of BrowsePaths.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="rootId"></param>
        /// <param name="parent"></param>
        /// <param name="parentPath"></param>
        /// <param name="browsePaths"></param>
        private static List<BrowsePath> GetBrowsePathFromNodeState(
            this IOpcUaSession session, NodeId rootId, NodeState parent,
            RelativePath? parentPath, List<BrowsePath>? browsePaths = null)
        {
            browsePaths ??= new List<BrowsePath>();
            var children = new List<BaseInstanceState>();
            parent.GetChildren(session.SystemContext, children);
            foreach (var child in children)
            {
                var browsePath = new BrowsePath
                {
                    StartingNode = rootId,
                    Handle = child
                };
                if (parentPath != null)
                {
                    browsePath.RelativePath.Elements.AddRange(parentPath.Elements);
                }
                var element = new RelativePathElement
                {
                    ReferenceTypeId = child.ReferenceTypeId,
                    IsInverse = false,
                    IncludeSubtypes = false,
                    TargetName = child.BrowseName
                };
                browsePath.RelativePath.Elements.Add(element);
                browsePaths.Add(browsePath);
                browsePaths = session.GetBrowsePathFromNodeState(rootId, child,
                    browsePath.RelativePath, browsePaths);
            }
            return browsePaths;
        }
    }
}
