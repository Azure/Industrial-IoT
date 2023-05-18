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
    using NodeClass = OpcUa.Publisher.Models.NodeClass;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session Handle extensions
    /// </summary>
    public static class SessionHandleEx
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
        public static async Task<(T?, ServiceResultModel?)> ReadAttributeAsync<T>(
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
        public static async Task<IEnumerable<(T?, ServiceResultModel?)>> ReadAttributeAsync<T>(
            this IOpcUaSession session, RequestHeader header, IEnumerable<NodeId> nodeIds,
            uint attributeId, CancellationToken ct = default)
        {
            if (!nodeIds.Any())
            {
                return Enumerable.Empty<(T?, ServiceResultModel?)>();
            }
            var itemsToRead = new ReadValueIdCollection(nodeIds
                .Select(nodeId => new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                }));
            var response = await session.Services.ReadAsync(header,
                0, Opc.Ua.TimestampsToReturn.Neither, itemsToRead,
                ct).ConfigureAwait(false);
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
            System.Diagnostics.Debug.Assert(nodeClass != null);
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
                .Batch(Math.Max(1, (int)(limits.MaxNodesPerTranslatePathsToNodeIds ?? 0)));
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
                var objectsToBrowseBatches = objectsToBrowse
                    .Batch(Math.Max(1, (int)(limits.MaxNodesPerBrowse ?? 0)));
                foreach (var batch in objectsToBrowseBatches)
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
                                    var child = new BaseObjectState(obj)
                                    {
                                        NodeId = (NodeId)reference.NodeId,
                                        BrowseName = reference.BrowseName,
                                        DisplayName = reference.DisplayName,
                                        ReferenceTypeId = reference.ReferenceTypeId
                                    };
                                    obj.AddChild(child);
                                    break;
                            }
                        }
                    }
                }
            }

            if (valuesToRead.Count > 0)
            {
                var valuesToReadBatches = valuesToRead
                    .Batch(Math.Max(1, (int)(limits.MaxNodesPerRead ?? 0)));
                foreach (var batch in valuesToReadBatches)
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
        public static async Task CollectTypeHierarchyAsync(this IOpcUaSession session,
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
                var response = await session.Services.BrowseAsync(header, null, 0, nodeToBrowse,
                    ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        internal static async Task<ServiceResultModel?> CollectInstanceDeclarationsAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId typeId,
            InstanceDeclarationModel? parent, List<InstanceDeclarationModel> instances,
            IDictionary<ImmutableRelativePath, InstanceDeclarationModel> map, CancellationToken ct)
        {
            // find the children of the type.
            var nodeToBrowse = new BrowseDescriptionCollection {
                new BrowseDescription {
                    NodeId = parent == null ? typeId : parent.NodeId,
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HasChild,
                    IncludeSubtypes = true,
                    NodeClassMask =
                        (uint)Opc.Ua.NodeClass.Object |
                        (uint)Opc.Ua.NodeClass.Variable |
                        (uint)Opc.Ua.NodeClass.Method,
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

                // find the modelling rules.
                var targets = await session.FindTargetOfReferenceAsync(requestHeader,
                    references.Select(r => (NodeId)r.NodeId),
                    ReferenceTypeIds.HasModellingRule, ct).ConfigureAwait(false);
                var referencesWithRules = targets
                    .Zip(references)
                    .ToList();

                // Get the children.
                var children = new Dictionary<NodeId, InstanceDeclarationModel>();
                foreach (var (modellingRule, reference) in referencesWithRules)
                {
                    var browseName = reference.BrowseName.AsString(session.MessageContext);
                    var relativePath = ImmutableRelativePath.Create(parent?.BrowsePath,
                        "/" + browseName);
                    var nodeClass = reference.NodeClass.ToServiceType();
                    if (NodeId.IsNull(modellingRule.Item2) || nodeClass == null)
                    {
                        // if the modelling rule is null then the instance is not part
                        // of the type declaration.
                        map.Remove(relativePath);
                        continue;
                    }
                    // create a new declaration.
                    map.TryGetValue(relativePath, out var overriden);

                    var displayName =
                        LocalizedText.IsNullOrEmpty(reference.DisplayName) ?
                        reference.BrowseName.Name : reference.DisplayName.Text;
                    var child = new InstanceDeclarationModel
                    {
                        RootTypeId = typeId.AsString(session.MessageContext),
                        NodeId = reference.NodeId.AsString(session.MessageContext),
                        BrowseName = reference.BrowseName.AsString(session.MessageContext),
                        NodeClass = nodeClass.Value,
                        DisplayPath = parent == null ?
                            displayName : $"{parent.DisplayPath}/{displayName}",
                        DisplayName = displayName,
                        BrowsePath = relativePath.Path,
                        ModellingRule = modellingRule.Item1.AsString(session.MessageContext),
                        ModellingRuleId = modellingRule.Item2.AsString(session.MessageContext),
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
                    variables, variableMetadata, ct).ConfigureAwait(false);
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
                    variables, methodMetadata, ct).ConfigureAwait(false);
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
                if (errorInfo != null)
                {
                    return errorInfo;
                }
                foreach (var (nodeId, description) in children.Keys.Zip(attributes))
                {
                    children[nodeId] = children[nodeId] with
                    {
                        Description = description.Item1?.Text
                    };
                }

                // recusively collect instance declarations for the tree below.
                foreach (var child in children.Values)
                {
                    instances.Add(child);
                    await session.CollectInstanceDeclarationsAsync(requestHeader,
                        typeId, child, instances, map, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(VariableMetadataModel?, ServiceResultModel?)> GetVariableMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId nodeId, CancellationToken ct)
        {
            var results = new List<VariableMetadataModel>();
            var errorInfo = await session.CollectVariableMetadataAsync(requestHeader,
                nodeId.YieldReturn(), results, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> CollectVariableMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, IEnumerable<NodeId> nodeIds,
            List<VariableMetadataModel> metadata, CancellationToken ct)
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
                    DataType = node.Value[Attributes.DataType]
                        .GetValueOrDefault<NodeId>()?.AsString(session.MessageContext)
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
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(MethodMetadataModel?, ServiceResultModel?)> GetMethodMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, NodeId nodeId, CancellationToken ct)
        {
            var results = new List<MethodMetadataModel>();
            var errorInfo = await session.CollectMethodMetadataAsync(requestHeader,
                nodeId.YieldReturn(), results, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<ServiceResultModel?> CollectMethodMetadataAsync(
            this IOpcUaSession session, RequestHeader requestHeader, IEnumerable<NodeId> nodeIds,
            List<MethodMetadataModel> metadata, CancellationToken ct)
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
                            objectId = nodeReference.NodeId.AsString(session.MessageContext);
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
                            argument.DataType, null, false, false, false, ct).ConfigureAwait(false);
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
        /// <param name="children"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(NodeModel, ServiceResultModel?)> ReadNodeAsync(
            this IOpcUaSession session, RequestHeader header, NodeId nodeId,
            Opc.Ua.NodeClass? nodeClass, bool skipValue, bool rawMode,
            bool? children = null, CancellationToken ct = default)
        {
            if (rawMode)
            {
                var id = nodeId.AsString(session.MessageContext);
                System.Diagnostics.Debug.Assert(id != null);
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(id));
                return (new NodeModel
                {
                    NodeId = id,
                    NodeClass = nodeClass?.ToServiceType()
                }, null);
            }
            return await session.ReadNodeAsync(header, nodeId,
                nodeClass, skipValue, children, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read node model
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="nodeClass"></param>
        /// <param name="skipValue"></param>
        /// <param name="children"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<(NodeModel, ServiceResultModel?)> ReadNodeAsync(
            this IOpcUaSession session, RequestHeader header, NodeId nodeId,
            Opc.Ua.NodeClass? nodeClass = null, bool skipValue = true, bool? children = null,
            CancellationToken ct = default)
        {
            var results = await session.ReadNodeAttributesAsync(header, nodeId, skipValue,
                nodeClass, ct).ConfigureAwait(false);
            var lookup = results.AsLookupTable();

            var id = nodeId.AsString(session.MessageContext);
            System.Diagnostics.Debug.Assert(id != null);
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(id));
            lookup.TryGetValue(Attributes.Value, out var value);
            var nodeModel = new NodeModel
            {
                Children = children,
                NodeId = id,
                Value = value.Item1 == null ? null :
                    session.Codec.Encode(
                        value.Item1?.WrappedValue, out var type),
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
                        .AsString(session.MessageContext),
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
                        .AsString(session.MessageContext),
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
                        .Select(p => p.ToServiceModel(session.MessageContext)).ToList(),
                RolePermissions =
                    lookup[Attributes.RolePermissions].Item1?
                        .GetValueOrDefault<ExtensionObject[]>()?
                        .Select(ex => ex.Body)
                        .OfType<RolePermissionType>()
                        .Select(p => p.ToServiceModel(session.MessageContext)).ToList()
            };
            return (nodeModel, results.ErrorInfo ?? lookup[Attributes.NodeClass].Item2);
        }

        /// <summary>
        /// Finds the targets for the specified reference.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeIds"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<IEnumerable<(QualifiedName, NodeId)>> FindTargetOfReferenceAsync(
            this IOpcUaSession session, RequestHeader requestHeader,
            IEnumerable<NodeId> nodeIds, NodeId referenceTypeId,
            CancellationToken ct = default)
        {
            // construct browse request.
            var nodesToBrowse = new BrowseDescriptionCollection(nodeIds
                .Select(nodeId => new BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeId = referenceTypeId,
                    IncludeSubtypes = false,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.BrowseName
                }));

            var response = await session.Services.BrowseAsync(requestHeader, null, 1,
                nodesToBrowse, ct).ConfigureAwait(false);
            var results = response.Validate(response.Results, s => s.StatusCode,
                response.DiagnosticInfos, nodesToBrowse);
            var targetIds = new List<(QualifiedName, NodeId)>();
            if (results.ErrorInfo != null)
            {
                return targetIds;
            }

            var continuationPoints = new ByteStringCollection();
            foreach (var result in results)
            {
                // check for error.
                if (result.ErrorInfo != null)
                {
                    targetIds.Add((QualifiedName.Null, NodeId.Null));
                    continue;
                }
                // check for continuation point.
                if (result.Result.ContinuationPoint?.Length > 0)
                {
                    continuationPoints.Add(result.Result.ContinuationPoint);
                }
                // get the node id.
                if (result.Result.References.Count > 0)
                {
                    if (NodeId.IsNull(result.Result.References[0].NodeId) ||
                        result.Result.References[0].NodeId.IsAbsolute)
                    {
                        targetIds.Add((QualifiedName.Null, NodeId.Null));
                        continue;
                    }
                    targetIds.Add(
                        (result.Result.References[0].BrowseName,
                         (NodeId)result.Result.References[0].NodeId));
                }
            }

            // release continuation points.
            if (continuationPoints.Count > 0)
            {
                await session.Services.BrowseNextAsync(requestHeader, true,
                    continuationPoints, ct).ConfigureAwait(false);
            }
            return targetIds;
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
            var firstResponse = await session.Services.BrowseAsync(requestHeader, view,
                0, nodesToBrowse, ct).ConfigureAwait(false);
            var firstResults = firstResponse.Validate(firstResponse.Results,
                s => s.StatusCode, firstResponse.DiagnosticInfos, nodesToBrowse);
            if (firstResults.ErrorInfo != null)
            {
                yield return new BrowseResult(null, null, firstResults.ErrorInfo);
            }
            var continuationPoints = firstResults
                .Where(r =>
                    r.Result.ContinuationPoint?.Length > 0)
                .ToDictionary(r => r.Request, r => r.Result.ContinuationPoint);
            try
            {
                foreach (var result in firstResults)
                {
                    yield return new BrowseResult(result.Request,
                        result.Result.References, result.ErrorInfo);
                }
                while (continuationPoints.Count != 0)
                {
                    var nextResponse = await session.Services.BrowseNextAsync(requestHeader,
                        false, new ByteStringCollection(continuationPoints.Values),
                        ct).ConfigureAwait(false);
                    var nextResults = firstResponse.Validate(nextResponse.Results,
                        s => s.StatusCode, nextResponse.DiagnosticInfos,
                        continuationPoints);
                    if (nextResults.ErrorInfo != null)
                    {
                        yield return new BrowseResult(null, null, nextResults.ErrorInfo);
                    }
                    foreach (var result in nextResults)
                    {
                        yield return new BrowseResult(
                            result.Request.Key, result.Result.References, result.ErrorInfo);
                    }
                    continuationPoints = nextResults
                        .Where(r =>
                            r.Result.ContinuationPoint?.Length > 0)
                        .ToDictionary(r => r.Request.Key, r => r.Result.ContinuationPoint);
                }
            }
            finally
            {
                if (continuationPoints.Count != 0)
                {
                    await session.Services.BrowseNextAsync(requestHeader,
                        true, new ByteStringCollection(continuationPoints.Values),
                        ct).ConfigureAwait(false);
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
        private static List<Opc.Ua.BrowsePath> GetBrowsePathFromNodeState(
            this IOpcUaSession session, NodeId rootId, NodeState parent,
            RelativePath? parentPath, List<Opc.Ua.BrowsePath>? browsePaths = null)
        {
            browsePaths ??= new List<Opc.Ua.BrowsePath>();
            var children = new List<BaseInstanceState>();
            parent.GetChildren(session.SystemContext, children);
            foreach (var child in children)
            {
                var browsePath = new Opc.Ua.BrowsePath
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
