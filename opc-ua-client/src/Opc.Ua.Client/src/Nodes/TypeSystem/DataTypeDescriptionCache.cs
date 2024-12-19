// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Nodes.TypeSystem;

using Microsoft.Extensions.Logging;
using Opc.Ua.Client.Nodes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// The data type cache is an encodeable factory that plugs into the encoder
/// and decoder system of the stack and contains all types in the server.
/// The types can be loaded and reloaded from the server.
/// The types are dynamically preloaded when they are needed. This happens
/// whereever custom types are required in the API surface, e.g. reading
/// writing and calling as well as creating monitored items. Alternatively
/// the user can choose to load all types when the session is created or
/// recreated with the server.
/// </summary>
internal class DataTypeDescriptionCache : IEncodeableFactory, IDataTypeDescriptionCache,
    IDataTypeDescriptionResolver
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<ExpandedNodeId, Type> EncodeableTypes
        => _context.Factory.EncodeableTypes;

    /// <inheritdoc/>
    public int InstanceId => _context.Factory.InstanceId;

    /// <summary>
    /// Disable the use of the data type system cache to use the legacy data
    /// type dictionaries instead of the new data type definitions which are
    /// always used first.
    /// </summary>
    public bool DisableLegacyDataTypeSystem { get; set; }

    /// <summary>
    /// Initializes the type system with a session and optionally type
    /// factory to load the custom types.
    /// </summary>
    /// <param name="nodeCache"></param>
    /// <param name="context"></param>
    /// <param name="loggerFactory"></param>
    public DataTypeDescriptionCache(INodeCache nodeCache,
        IServiceMessageContext context, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DataTypeDescriptionCache>();
        _dataTypeSystems = new DataTypeSystemCache(nodeCache, context,
            loggerFactory);
        _context = context;
        _nodeCache = nodeCache;
    }

    /// <summary>
    /// Clone constructor
    /// </summary>
    /// <param name="dataTypeSystem"></param>
    private DataTypeDescriptionCache(DataTypeDescriptionCache dataTypeSystem)
    {
        _logger = dataTypeSystem._logger;
        _context = dataTypeSystem._context;
        _nodeCache = dataTypeSystem._nodeCache;
        _dataTypeSystems = dataTypeSystem._dataTypeSystems;
    }

    /// <inheritdoc/>
    public Type? GetSystemType(ExpandedNodeId typeOrEncodingId)
    {
        if (_structures.ContainsKey(typeOrEncodingId))
        {
            return typeof(StructureValue);
        }
        if (_enums.ContainsKey(typeOrEncodingId))
        {
            return typeof(EnumValue);
        }
        return _context.Factory.GetSystemType(typeOrEncodingId);
    }

    /// <inheritdoc/>
    public object Clone()
    {
        return new DataTypeDescriptionCache(this);
    }

    /// <inheritdoc/>
    public void AddEncodeableType(Type systemType)
    {
        _context.Factory.AddEncodeableType(systemType);
    }

    /// <inheritdoc/>
    public void AddEncodeableType(ExpandedNodeId encodingId, Type systemType)
    {
        _context.Factory.AddEncodeableType(encodingId, systemType);
    }

    /// <inheritdoc/>
    public void AddEncodeableTypes(Assembly assembly)
    {
        _context.Factory.AddEncodeableTypes(assembly);
    }

    /// <inheritdoc/>
    public void AddEncodeableTypes(IEnumerable<Type> systemTypes)
    {
        _context.Factory.AddEncodeableTypes(systemTypes);
    }

    /// <inheritdoc/>
    public StructureDescription? GetStructureDescription(ExpandedNodeId typeOrEncodingId)
    {
        if (!_structures.TryGetValue(typeOrEncodingId, out var info))
        {
            // sync load the description from the server
            return GetDataTypeDescriptionAsync(typeOrEncodingId, default)
                .AsTask().GetAwaiter().GetResult() as StructureDescription;
        }
        return info;
    }

    /// <inheritdoc/>
    public EnumDescription? GetEnumDescription(ExpandedNodeId typeOrEncodingId)
    {
        if (!_enums.TryGetValue(typeOrEncodingId, out var info))
        {
            // sync load the description from the server
            return GetDataTypeDescriptionAsync(typeOrEncodingId, default)
                .AsTask().GetAwaiter().GetResult() as EnumDescription;
        }
        return info;
    }

    /// <inheritdoc/>
    public async ValueTask<IDictionary<ExpandedNodeId, DataTypeDefinition>> GetDefinitionsAsync(
        ExpandedNodeId dataTypeId, CancellationToken ct)
    {
        var dataTypeDefinitions = new Dictionary<ExpandedNodeId, DataTypeDefinition>();
        await PreloadDataTypeAsync(dataTypeId, true, ct).ConfigureAwait(false);
        CollectAllDataTypeDefinitions(dataTypeId, dataTypeDefinitions);
        return dataTypeDefinitions;

        void CollectAllDataTypeDefinitions(ExpandedNodeId nodeId,
            Dictionary<ExpandedNodeId, DataTypeDefinition> collect)
        {
            if (NodeId.IsNull(nodeId))
            {
                return;
            }
            if (_structures.TryGetValue(nodeId, out var dataTypeDefinition))
            {
                var structureDefinition = dataTypeDefinition.StructureDefinition;
                collect[nodeId] = structureDefinition;

                foreach (var field in structureDefinition.Fields)
                {
                    if (!collect.ContainsKey(field.DataType))
                    {
                        CollectAllDataTypeDefinitions(field.DataType, collect);
                    }
                }
            }
            else if (_enums.TryGetValue(nodeId, out var enumDescription))
            {
                collect[nodeId] = enumDescription.EnumDefinition;
            }
        }
    }

    /// <summary>
    /// Get a data type definition or load it if not already loaded into the cache.
    /// The method returns null if the type cannot be resolved.
    /// </summary>
    /// <param name="typeOrEncodingId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<DataTypeDescription?> GetDataTypeDescriptionAsync(
        ExpandedNodeId typeOrEncodingId, CancellationToken ct)
    {
        var definition = GetDataTypeDescription(typeOrEncodingId);
        if (definition != null)
        {
            return ValueTask.FromResult<DataTypeDescription?>(definition);
        }
        return GetDataTypeDefinitionAsyncCore(typeOrEncodingId, ct);
        async ValueTask<DataTypeDescription?> GetDataTypeDefinitionAsyncCore(
            ExpandedNodeId dataTypeId, CancellationToken ct)
        {
            var dataTypeNode = await GetDataTypeAsync(dataTypeId, ct).ConfigureAwait(false);
            if (dataTypeNode != null &&
                await AddDataTypeAsync(dataTypeNode, ct).ConfigureAwait(false))
            {
                return GetDataTypeDescription(dataTypeId);
            }
            _logger.LogDebug("Failed to get definition for type {DataTypeId}", dataTypeId);
            return null;
        }
    }

    /// <summary>
    /// Load the data type definitions for the data type referenced by the provided
    /// node id. If the node is is not a data type, try to resolve the data type
    /// the user intended to use.
    /// </summary>
    /// <param name="dataTypeId"></param>
    /// <param name="includeSubTypes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    public async ValueTask PreloadDataTypeAsync(ExpandedNodeId dataTypeId,
        bool includeSubTypes, CancellationToken ct)
    {
        var dataTypeNode = await GetDataTypeAsync(dataTypeId, ct).ConfigureAwait(false);
        if (dataTypeNode == null)
        {
            // Could not find data type node
            return;
        }
        if (includeSubTypes)
        {
            // Load all subtypes of this data type
            await foreach (var subType in GetUnknownSubTypesAsync(dataTypeNode.NodeId,
                ct).ConfigureAwait(false))
            {
                await PreloadAsync(subType, ct).ConfigureAwait(false);
            }
        }
        if (IsKnownType(dataTypeNode.NodeId))
        {
            // Type is already known to us
            return;
        }
        await PreloadAsync(dataTypeNode, ct).ConfigureAwait(false);

        async ValueTask PreloadAsync(DataTypeNode dataTypeNode, CancellationToken ct)
        {
            if (!await AddDataTypeAsync(dataTypeNode, ct).ConfigureAwait(false))
            {
                _logger.LogDebug("Preloading type {DataTypeId} failed.",
                    dataTypeNode.NodeId);
                return;
            }
            if (_structures.TryGetValue(dataTypeNode.NodeId, out var description))
            {
                // Preload all field types if needed
                var includeSubtypes = description.FieldsCanHaveSubtypedValues;
                foreach (var field in description.StructureDefinition.Fields)
                {
                    if (!includeSubtypes && IsKnownType(field.DataType))
                    {
                        continue;
                    }
                    await PreloadDataTypeAsync(field.DataType, includeSubtypes,
                        ct).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Load all custom types from a server using the session node cache.
    /// The loader loads all data types first and then all definitions
    /// associated with them.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>true if all DataTypes were loaded.</returns>
    public async ValueTask<bool> PreloadAllDataTypeAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Get all unknown types in the type hierarchy of the base data type
            var allTypesLoaded = true;
            await foreach (var type in GetUnknownSubTypesAsync(
                DataTypeIds.BaseDataType, ct).ConfigureAwait(false))
            {
                if (!await AddDataTypeAsync(type, ct).ConfigureAwait(false))
                {
                    _logger.LogDebug("Preloading type {DataTypeId} failed.",
                        type.NodeId);
                    allTypesLoaded = false;
                }
            }
            _logger.LogInformation("Preloading all types took {Duration}ms.",
                sw.ElapsedMilliseconds);
            return allTypesLoaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the custom types.");
            return false;
        }
    }

    /// <summary>
    /// Get a data type definition if it is already loaded
    /// </summary>
    /// <param name="dataTypeId"></param>
    /// <returns></returns>
    public DataTypeDescription? GetDataTypeDescription(ExpandedNodeId dataTypeId)
    {
        if (_structures.TryGetValue(dataTypeId, out var structureDescription))
        {
            return structureDescription;
        }
        if (_enums.TryGetValue(dataTypeId, out var enumDescription))
        {
            return enumDescription;
        }
        return null;
    }

    /// <summary>
    /// Try to get the data type user wanted. If the data type id is not a data type
    /// node then if it is a variable or variable type, use the data type of it, if
    /// it is a encoding of a type, use the data type that references the encoding.
    /// Otherwise give up and return null.
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async ValueTask<DataTypeNode?> GetDataTypeAsync(ExpandedNodeId typeId,
        CancellationToken ct)
    {
        var nodeId = ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris);
        var node = await _nodeCache.GetNodeAsync(nodeId, ct).ConfigureAwait(false);
        if (node is not DataTypeNode)
        {
            // Load the type definition for the variable or variable type
            switch (node)
            {
                case VariableNode v:
                    // Check if this is a variable of type Encoding
                    // Then we want the inverse HasEncoding reference to it
                    var references = await _nodeCache.GetReferencesAsync(
                        nodeId, ReferenceTypeIds.HasEncoding, true, false, ct)
                        .ConfigureAwait(false);
                    if (references.Count == 1) // It is a encoding of a type
                    {
                        typeId = references[0].NodeId;
                        break;
                    }
                    typeId = v.DataType;
                    break;
                case VariableTypeNode v:
                    typeId = v.DataType;
                    break;
                default:
                    // Nothing to do
                    return null;
            }
            nodeId = ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris);
            node = await _nodeCache.GetNodeAsync(nodeId, ct).ConfigureAwait(false);
        }
        return node as DataTypeNode;
    }

    /// <summary>
    /// Add type information
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="definition"></param>
    /// <param name="binaryEncodingId"></param>
    /// <param name="xmlEncodingId"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    /// <param name="xmlDefinition"></param>
    internal void Add(ExpandedNodeId typeId, DataTypeDefinition definition,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
        XmlQualifiedName xmlName, bool isAbstract,
        DataTypeDefinition? xmlDefinition = null)
    {
        switch (definition)
        {
            case StructureDefinition structureDefinition:
                var structureDescription = StructureDescription.Create(this, typeId,
                    structureDefinition, xmlName, binaryEncodingId, xmlEncodingId,
                    ExpandedNodeId.Null, isAbstract);
                _structures[typeId] = structureDescription;
                if (binaryEncodingId != ExpandedNodeId.Null)
                {
                    _structures[binaryEncodingId] = structureDescription;
                }
                if (xmlEncodingId != ExpandedNodeId.Null)
                {
                    if (xmlDefinition is StructureDefinition xml)
                    {
                        structureDescription = StructureDescription.Create(this,
                            typeId, xml, xmlName, binaryEncodingId, xmlEncodingId,
                            ExpandedNodeId.Null, isAbstract);
                    }
                    _structures[xmlEncodingId] = structureDescription;
                }
                break;
            case EnumDefinition enumDefinition:
                var enumDescription = new EnumDescription(typeId, enumDefinition,
                xmlName, isAbstract);
                _enums[typeId] = enumDescription;
                if (binaryEncodingId != ExpandedNodeId.Null)
                {
                    _enums[binaryEncodingId] = enumDescription;
                }
                if (xmlEncodingId != ExpandedNodeId.Null)
                {
                    if (xmlDefinition is EnumDefinition xml)
                    {
                        enumDescription = new EnumDescription(typeId, xml, xmlName,
                            isAbstract);
                    }
                    _enums[xmlEncodingId] = enumDescription;
                }
                break;
        }
    }

    /// <summary>
    /// Add an structure type defined in a DataType node to the type cache.
    /// </summary>
    /// <param name="dataTypeNode"></param>
    /// <param name="ct"></param>
    internal async ValueTask<bool> AddDataTypeAsync(DataTypeNode dataTypeNode,
        CancellationToken ct)
    {
        // Get encodings
        var lookup = await GetEncodingsAsync(dataTypeNode, ct).ConfigureAwait(false);
        var binaryEncodingId = lookup.TryGetValue(BrowseNames.DefaultBinary,
            out var b) ? b : ExpandedNodeId.Null;
        var xmlEncodingId = lookup.TryGetValue(BrowseNames.DefaultXml,
            out var x) ? x : ExpandedNodeId.Null;

        XmlQualifiedName? name = null;
        var dataTypeId = NormalizeExpandedNodeId(dataTypeNode.NodeId);

        // 1. Use data type definition for all encodings
        var dataTypeDefinition = GetDataTypeDefinition(dataTypeNode);
        if (dataTypeDefinition == null && !DisableLegacyDataTypeSystem)
        {
            // 2. Use legacy type system
            var def = await _dataTypeSystems.GetDataTypeDefinitionAsync(
                BrowseNames.DefaultBinary, dataTypeNode.NodeId,
                ct).ConfigureAwait(false);

            dataTypeDefinition = def?.Definition as StructureDefinition;
            name = def?.XmlName;

            // The xml encoding might be different than the binary encoding
            // This is a special case to handle the 1.03 type system where
            // the xml encoding is defined using xml schema which could be
            // different from the binary schema definition. Therefore we
            // register the xml schema specially.
            if (xmlEncodingId != ExpandedNodeId.Null || dataTypeDefinition == null)
            {
                var xml = await _dataTypeSystems.GetDataTypeDefinitionAsync(
                    BrowseNames.DefaultXml, dataTypeNode.NodeId,
                    ct).ConfigureAwait(false);
                if (xml?.Definition is StructureDefinition xmlStructureDefinition)
                {
                    dataTypeDefinition ??= xmlStructureDefinition;
                    Add(dataTypeId, dataTypeDefinition, binaryEncodingId,
                        xmlEncodingId, xml.XmlName, dataTypeNode.IsAbstract,
                        xmlStructureDefinition);
                    return true;
                }
            }
        }
        if (dataTypeDefinition == null)
        {
            // 3. Give up
            return false;
        }
        if (name == null)
        {
            var typeName = dataTypeNode.BrowseName;
            name = new XmlQualifiedName(typeName.Name,
                _context.NamespaceUris.GetString(typeName.NamespaceIndex));
        }
        Add(dataTypeId, dataTypeDefinition,
            binaryEncodingId, xmlEncodingId, name, dataTypeNode.IsAbstract);
        return true;
    }

    /// <summary>
    /// Fetch all nodes and subtype nodes of a data type.
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    internal async IAsyncEnumerable<DataTypeNode> GetUnknownSubTypesAsync(
        ExpandedNodeId dataType, [EnumeratorCancellation] CancellationToken ct)
    {
        var nodesToBrowse = new NodeIdCollection
        {
            ExpandedNodeId.ToNodeId(dataType, _context.NamespaceUris)
        };
        while (nodesToBrowse.Count > 0)
        {
            var response = await _nodeCache.GetReferencesAsync(nodesToBrowse,
                [ReferenceTypeIds.HasSubtype], false, false, ct).ConfigureAwait(false);
            foreach (var node in response.OfType<DataTypeNode>()
                .Where(n => !IsKnownType(n.NodeId)))
            {
                yield return node;
            }
            nodesToBrowse = new NodeIdCollection(response
                .OfType<DataTypeNode>()
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId,
                    _context.NamespaceUris)));
        }
    }

    /// <summary>
    /// Helper to ensure the expanded nodeId contains a valid namespaceUri.
    /// </summary>
    /// <param name="expandedNodeId">The expanded nodeId.</param>
    /// <returns>The normalized expanded nodeId.</returns>
    private ExpandedNodeId NormalizeExpandedNodeId(ExpandedNodeId expandedNodeId)
    {
        var nodeId = ExpandedNodeId.ToNodeId(
            expandedNodeId, _context.NamespaceUris);
        return NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris)
            ?? ExpandedNodeId.Null;
    }

    /// <summary>
    /// Get encodings of the data type
    /// </summary>
    /// <param name="dataTypeNode"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<Dictionary<QualifiedName, ExpandedNodeId>> GetEncodingsAsync(
        DataTypeNode dataTypeNode, CancellationToken ct)
    {
        var source = ExpandedNodeId.ToNodeId(dataTypeNode.NodeId,
            _context.NamespaceUris);
        var references = await _nodeCache.GetReferencesAsync(source,
            ReferenceTypeIds.HasEncoding, false, false, ct).ConfigureAwait(false);
        return references.ToDictionary(r => r.BrowseName, r =>
            NormalizeExpandedNodeId(r.NodeId));
    }

    /// <summary>
    /// Get the factory system type for an expanded node id.
    /// </summary>
    /// <param name="nodeId"></param>
    private bool IsKnownType(ExpandedNodeId nodeId)
    {
        if (!nodeId.IsAbsolute)
        {
            nodeId = NormalizeExpandedNodeId(nodeId);
        }
        return GetSystemType(nodeId) != null;
    }

    /// <summary>
    /// Get the data type definition from the data type node
    /// </summary>
    /// <param name="dataTypeNode"></param>
    private static DataTypeDefinition? GetDataTypeDefinition(
        DataTypeNode dataTypeNode)
    {
        switch (dataTypeNode.DataTypeDefinition?.Body)
        {
            case EnumDefinition enumDefinition:
                return enumDefinition;
            case StructureDefinition structureDefinition:
                // Validate the DataTypeDefinition structure,
                // but not if the type is supported
                if (structureDefinition.Fields == null ||
                    NodeId.IsNull(structureDefinition.BaseDataType))
                {
                    return null;
                }
                // Validate the structure according to Part3, Table 36
                foreach (var field in structureDefinition.Fields)
                {
                    // validate if the DataTypeDefinition is correctly
                    // filled out, some servers don't do it yet...
                    if (NodeId.IsNull(field.DataType) ||
                        string.IsNullOrWhiteSpace(field.Name))
                    {
                        return null;
                    }
                    if (field.ValueRank is not (ValueRanks.Scalar or
                        >= ValueRanks.OneDimension))
                    {
                        return null;
                    }
                }
                return structureDefinition;
            default:
                return null;
        }
    }

    private readonly ILogger _logger;
    private readonly INodeCache _nodeCache;
    private readonly IServiceMessageContext _context;
    private readonly IDataTypeSystemCache _dataTypeSystems;
    private readonly ConcurrentDictionary<ExpandedNodeId, StructureDescription> _structures = [];
    private readonly ConcurrentDictionary<ExpandedNodeId, EnumDescription> _enums = [];
}
