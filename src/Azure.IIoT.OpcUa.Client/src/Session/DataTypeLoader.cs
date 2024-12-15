// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Resolve data types from the node cache of the session.
/// </summary>
internal sealed class DataTypeLoader
{
    /// <inheritdoc/>
    public NamespaceTable NamespaceUris => _context.NamespaceUris;

    /// <summary>
    /// Create type resolver
    /// </summary>
    /// <param name="nodeCache"></param>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public DataTypeLoader(INodeCache nodeCache, IServiceMessageContext context,
        ILogger<DataTypeLoader> logger)
    {
        _context = context;
        _logger = logger;
        _nodeCache = nodeCache;
    }

    /// <summary>
    /// Load the data type system from the server
    /// </summary>
    /// <param name="dataTypeSystem"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    public async ValueTask<Dictionary<NodeId, DataTypeDictionary>> LoadDataTypeSystemAsync(
        NodeId? dataTypeSystem = null, CancellationToken ct = default)
    {
        if (dataTypeSystem == null)
        {
            dataTypeSystem = ObjectIds.OPCBinarySchema_TypeSystem;
        }
        else if (!Utils.IsEqual(dataTypeSystem, ObjectIds.OPCBinarySchema_TypeSystem) &&
                 !Utils.IsEqual(dataTypeSystem, ObjectIds.XmlSchema_TypeSystem))
        {
            throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid,
                $"{nameof(dataTypeSystem)} does not refer to a valid data dictionary.");
        }

        // find the dictionary for the description.
        var references = await _nodeCache.GetReferencesAsync(dataTypeSystem,
            ReferenceTypeIds.HasComponent, false, false, ct).ConfigureAwait(false);
        if (references.Count == 0)
        {
            throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid,
                "Type system does not contain a valid data dictionary.");
        }

        // batch read all encodings and namespaces
        var referenceNodeIds = references
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, NamespaceUris))
            .ToList();

        // find namespace properties
        var namespaceReferences = await _nodeCache.GetReferencesAsync(
            referenceNodeIds, new[] { ReferenceTypeIds.HasProperty },
            false, false, ct).ConfigureAwait(false);
        var namespaceNodes = namespaceReferences
            .Where(n => n.BrowseName == BrowseNames.NamespaceUri)
            .ToList();
        var namespaceNodeIds = namespaceNodes
            .ConvertAll(n => ExpandedNodeId.ToNodeId(n.NodeId, NamespaceUris));

        // read all schema definitions
        var referenceExpandedNodeIds = references
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, NamespaceUris))
            .Where(n => n.NamespaceIndex != 0).ToList();
        var schemas = await DataTypeDictionary.ReadDictionariesAsync(
            _nodeCache, referenceExpandedNodeIds, ct).ConfigureAwait(false);

        // read namespace property values
        var namespaces = new Dictionary<NodeId, string>();
        var nameSpaceValues = await _nodeCache.GetValuesAsync(
            namespaceNodeIds, ct).ConfigureAwait(false);

        // build the namespace dictionary
        for (var i = 0; i < nameSpaceValues.Count; i++)
        {
            // servers may optimize space by not returning a dictionary
            if (StatusCode.IsNotBad(nameSpaceValues[i].StatusCode) &&
                nameSpaceValues[i].Value is string ns)
            {
                namespaces[referenceNodeIds[i]] = ns;
            }
            else
            {
                _logger.LogWarning("Failed to load namespace {Ns}: {Error}",
                    namespaceNodeIds[i], nameSpaceValues[i].StatusCode);
            }
        }

        // build the namespace/schema import dictionary
        var imports = new Dictionary<string, byte[]>();
        foreach (var r in references)
        {
            var nodeId = ExpandedNodeId.ToNodeId(r.NodeId, NamespaceUris);
            if (schemas.TryGetValue(nodeId, out var schema) &&
                namespaces.TryGetValue(nodeId, out var ns))
            {
                imports[ns] = schema;
            }
        }

        // read all type dictionaries in the type system
        var result = new Dictionary<NodeId, DataTypeDictionary>();
        foreach (var r in references)
        {
            var dictionaryId =
                ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris);
            if (dictionaryId.NamespaceIndex == 0)
            {
                // Skip the namespace 0 dictionaries which we already have
                continue;
            }
            if (_cache.TryGetValue(dictionaryId, out var dictionaryToLoad))
            {
                result.Add(dictionaryId, dictionaryToLoad);
                continue;
            }
            // Load the dictionary and add to the cache
            try
            {
                if (schemas.TryGetValue(dictionaryId, out var schema))
                {
                    dictionaryToLoad = await DataTypeDictionary.LoadAsync(_nodeCache,
                        dictionaryId, dictionaryId.ToString(), _context, schema, imports,
                        ct).ConfigureAwait(false);
                }
                else
                {
                    dictionaryToLoad = await DataTypeDictionary.LoadAsync(_nodeCache,
                        dictionaryId, dictionaryId.ToString(), _context,
                        ct: ct).ConfigureAwait(false);
                }
                _cache.TryAdd(dictionaryId, dictionaryToLoad);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Dictionary load error for Dictionary {NodeId} : {Message}",
                    r.NodeId, ex.Message);
                continue;
            }
            result.Add(dictionaryId, dictionaryToLoad);
        }
        return result;
    }

    /// <summary>
    /// Get the encodings
    /// </summary>
    /// <param name="nodeIds"></param>
    /// <param name="supportedEncodings"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<IReadOnlyList<NodeId>> BrowseForEncodingsAsync(
        IReadOnlyList<ExpandedNodeId> nodeIds, string[] supportedEncodings,
        CancellationToken ct)
    {
        // cache type encodings
        var source = nodeIds
            .Select(nodeId => ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris))
            .ToList();
        var encodings = await _nodeCache.GetReferencesAsync(
            source, new[] { ReferenceTypeIds.HasEncoding },
            false, false, ct).ConfigureAwait(false);

        // cache dictionary descriptions
        source = encodings
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
            .ToList();
        var descriptions = await _nodeCache.GetReferencesAsync(
            source, new[] { ReferenceTypeIds.HasDescription },
            false, false, ct).ConfigureAwait(false);
        return encodings
            .Where(r => supportedEncodings.Contains(r.BrowseName.Name))
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris)!)
            .Where(n => !NodeId.IsNull(n))
            .ToList();
    }

    /// <summary>
    /// Get the encodings
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="supportedEncodings"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<(
        IReadOnlyList<NodeId> encodings,
        ExpandedNodeId binaryEncodingId,
        ExpandedNodeId xmlEncodingId
        )> BrowseForEncodingsAsync(ExpandedNodeId nodeId,
            string[] supportedEncodings, CancellationToken ct = default)
    {
        var source = ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris);
        var references = await _nodeCache.GetReferencesAsync(source,
            ReferenceTypeIds.HasEncoding, false, false, ct).ConfigureAwait(false);

        var binaryEncodingId = references
            .FirstOrDefault(r => r.BrowseName.Name == BrowseNames.DefaultBinary)?
            .NodeId ?? ExpandedNodeId.Null;
        binaryEncodingId = NormalizeExpandedNodeId(binaryEncodingId);
        var xmlEncodingId = references
            .FirstOrDefault(r => r.BrowseName.Name == BrowseNames.DefaultXml)?
            .NodeId ?? ExpandedNodeId.Null;
        xmlEncodingId = NormalizeExpandedNodeId(xmlEncodingId);
        return (references
            .Where(r => supportedEncodings.Contains(r.BrowseName.Name))
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
            .ToList(),
            binaryEncodingId, xmlEncodingId);
    }

    /// <summary>
    /// Get the type id for the dictionary component.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<(
        ExpandedNodeId typeId,
        ExpandedNodeId encodingId,
        DataTypeNode? dataTypeNode
        )> BrowseTypeIdsForDictionaryComponentAsync(
            NodeId nodeId, CancellationToken ct = default)
    {
        var references = await _nodeCache.GetReferencesAsync(
            nodeId, ReferenceTypeIds.HasDescription, true, false,
            ct).ConfigureAwait(false);
        if (references.Count == 1 && !NodeId.IsNull(references[0].NodeId))
        {
            var encodingId = ExpandedNodeId.ToNodeId(references[0].NodeId,
                _context.NamespaceUris);

            references = await _nodeCache.GetReferencesAsync(
                encodingId, ReferenceTypeIds.HasEncoding, true, false,
                ct).ConfigureAwait(false);
            if (references.Count == 1 && !NodeId.IsNull(references[0].NodeId))
            {
                var typeId = ExpandedNodeId.ToNodeId(references[0].NodeId,
                    _context.NamespaceUris);
                var dataTypeNode = await _nodeCache.GetNodeAsync(typeId,
                    ct).ConfigureAwait(false);
                return (typeId, encodingId, dataTypeNode as DataTypeNode);
            }
        }
        return (ExpandedNodeId.Null, ExpandedNodeId.Null, null);
    }

    /// <summary>
    /// Load the data nodes for a base data type (structure or enumeration).
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="nestedSubTypes"></param>
    /// <param name="addRootNode"></param>
    /// <param name="filterUATypes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    public async ValueTask<IReadOnlyList<INode>> LoadDataTypesAsync(
        ExpandedNodeId dataType, bool nestedSubTypes = false,
        bool addRootNode = false, bool filterUATypes = true,
        CancellationToken ct = default)
    {
        var result = new List<INode>();
        var nodesToBrowse = new NodeIdCollection
            {
                ExpandedNodeId.ToNodeId(dataType, _context.NamespaceUris)
            };
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif
        if (addRootNode)
        {
            var rootNode = await _nodeCache.GetNodeAsync(nodesToBrowse[0],
                ct).ConfigureAwait(false);
            if (rootNode is not DataTypeNode)
            {
                throw new ServiceResultException(
                    "Root Node is not a DataType node.");
            }
            result.Add(rootNode);
        }

        while (nodesToBrowse.Count > 0)
        {
            var response = await _nodeCache.GetReferencesAsync(nodesToBrowse,
                new[]
                {
                        ReferenceTypeIds.HasSubtype
                },
                false, false, ct).ConfigureAwait(false);

            var nextNodesToBrowse = new NodeIdCollection();
            if (nestedSubTypes)
            {
                nextNodesToBrowse.AddRange(response
                    .Select(r => ExpandedNodeId.ToNodeId(r.NodeId,
                        _context.NamespaceUris)));
            }
            if (filterUATypes)
            {
                // filter out default namespace
                result.AddRange(response
                    .Where(rd => rd.NodeId.NamespaceIndex != 0));
            }
            else
            {
                result.AddRange(response);
            }
            nodesToBrowse = nextNodesToBrowse;
        }
#if DEBUG
        stopwatch.Stop();
        _logger.LogInformation("LoadDataTypes returns {Count} nodes in {Duration}ms",
            result.Count, stopwatch.ElapsedMilliseconds);
#endif
        return result;
    }

    /// <inheritdoc/>
    public ValueTask<INode> FindAsync(ExpandedNodeId nodeId, CancellationToken ct)
    {
        return _nodeCache.GetNodeAsync(
            ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris), ct);
    }

    /// <inheritdoc/>
    public async ValueTask<object?> GetEnumTypeArrayAsync(ExpandedNodeId nodeId,
        CancellationToken ct = default)
    {
        // find the property reference for the enum type
        var references = await _nodeCache.GetReferencesAsync(
            ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris),
            ReferenceTypeIds.HasProperty, false, false, ct).ConfigureAwait(false);
        if (references.Count > 0)
        {
            // read the enum type array
            var value = await _nodeCache.GetValueAsync(
                ExpandedNodeId.ToNodeId(references[0].NodeId, NamespaceUris),
                ct).ConfigureAwait(false);
            return value?.Value;
        }
        return null;
    }

    /// <inheritdoc/>
    public ValueTask<NodeId> FindSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
    {
        return _nodeCache.GetSuperTypeAsync(typeId, ct);
    }

    /// <summary>
    /// Helper to ensure the expanded nodeId contains a valid namespaceUri.
    /// </summary>
    /// <param name="expandedNodeId">The expanded nodeId.</param>
    /// <returns>The normalized expanded nodeId.</returns>
    private ExpandedNodeId NormalizeExpandedNodeId(ExpandedNodeId expandedNodeId)
    {
        var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, _context.NamespaceUris);
        return NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris) ?? ExpandedNodeId.Null;
    }

    private readonly ConcurrentDictionary<NodeId, DataTypeDictionary> _cache = new();
    private readonly INodeCache _nodeCache;
    private readonly IServiceMessageContext _context;
    private readonly ILogger _logger;
}

