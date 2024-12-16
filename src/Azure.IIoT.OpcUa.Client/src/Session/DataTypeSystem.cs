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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// The type system is an encodeable factory that plugs into the encoder
/// and decoder system of the stack and contains all types in the server.
/// The types can be loaded as well as reloaded from the server. The
/// types are dynamically preloaded when they are needed. This happens
/// whereever custom types are required in the API surface, e.g. reading
/// writing and calling as well as creating monitored items. Alternatively
/// the user can choose to load all types when the session is created or
/// recreated with the server.
/// </summary>
/// <remarks>
/// Support for V1.03 dictionaries and all V1.04 data type definitions
/// with the following known restrictions:
/// - Support only for V1.03 structured types which are mapped to the
///   V1.04 structured type definition. Unsupported V1.03 types are ignored.
/// - V1.04 OptionSet does not create the enumeration flags.
/// - When a type is not found and a dictionary must be loaded the whole
///   dictionary is loaded and parsed and all types are added.
/// </remarks>
internal class DataTypeSystem : IEncodeableFactory, IDataTypeSystem
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<ExpandedNodeId, Type> EncodeableTypes
        => _factory.EncodeableTypes;

    /// <inheritdoc/>
    public int InstanceId => _factory.InstanceId;

    /// <summary>
    /// Disable the use of DataTypeDefinition to create the
    /// complex type definition.
    /// </summary>
    public bool DisableDataTypeDefinition { get; set; }

    /// <summary>
    /// Disable the use of DataType Dictionaries to create the
    /// complex type definition.
    /// </summary>
    public bool DisableDataTypeDictionary { get; set; }

    /// <summary>
    /// Initializes the type system with a session and optionally type
    /// factory to load the custom types.
    /// </summary>
    /// <param name="nodeCache"></param>
    /// <param name="context"></param>
    /// <param name="loggerFactory"></param>
    public DataTypeSystem(INodeCache nodeCache, IServiceMessageContext context,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DataTypeSystem>();
        _context = context;
        _factory = context.Factory;
        _nodeCache = nodeCache;
        _dictionaries = new DataTypeDictionaries(nodeCache, context,
            loggerFactory.CreateLogger<DataTypeDictionaries>());
    }

    /// <summary>
    /// Clone constructor
    /// </summary>
    /// <param name="dataTypeSystem"></param>
    private DataTypeSystem(DataTypeSystem dataTypeSystem)
    {
        _logger = dataTypeSystem._logger;
        _context = dataTypeSystem._context;
        _factory = dataTypeSystem._factory;
        _nodeCache = dataTypeSystem._nodeCache;
        _dictionaries = dataTypeSystem._dictionaries;
    }

    /// <inheritdoc/>
    public Type? GetSystemType(ExpandedNodeId typeId)
    {
        if (_structures.ContainsKey(typeId))
        {
            return typeof(StructureValue);
        }
        if (_enums.ContainsKey(typeId))
        {
            return typeof(EnumValue);
        }
        return _factory.GetSystemType(typeId);
    }

    /// <inheritdoc/>
    public object Clone()
    {
        return new DataTypeSystem(this);
    }

    /// <inheritdoc/>
    public void AddEncodeableType(Type systemType)
    {
        _factory.AddEncodeableType(systemType);
    }

    /// <inheritdoc/>
    public void AddEncodeableType(ExpandedNodeId encodingId, Type systemType)
    {
        _factory.AddEncodeableType(encodingId, systemType);
    }

    /// <inheritdoc/>
    public void AddEncodeableTypes(Assembly assembly)
    {
        _factory.AddEncodeableTypes(assembly);
    }

    /// <inheritdoc/>
    public void AddEncodeableTypes(IEnumerable<Type> systemTypes)
    {
        _factory.AddEncodeableTypes(systemTypes);
    }

    /// <inheritdoc/>
    public StructureDescription? GetStructureDescription(ExpandedNodeId dataTypeId)
    {
        if (!_structures.TryGetValue(dataTypeId, out var info))
        {
            return null;
        }
        return info;
    }

    /// <inheritdoc/>
    public EnumDescription? GetEnumDescription(ExpandedNodeId dataTypeId)
    {
        if (!_enums.TryGetValue(dataTypeId, out var info))
        {
            return null;
        }
        return info;
    }

    /// <inheritdoc/>
    public IDictionary<ExpandedNodeId, DataTypeDefinition> GetDataTypeDefinitions(
        ExpandedNodeId dataTypeId)
    {
        var dataTypeDefinitions = new Dictionary<ExpandedNodeId, DataTypeDefinition>();
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
                    if (!IsRecursiveDataType(nodeId, field.DataType) &&
                        !collect.ContainsKey(nodeId))
                    {
                        CollectAllDataTypeDefinitions(field.DataType, collect);
                    }
                }
            }
            else if (_enums.TryGetValue(nodeId, out var enumDescription))
            {
                collect[nodeId] = enumDescription.EnumDefinition;
            }
            static bool IsRecursiveDataType(ExpandedNodeId structureDataType,
                ExpandedNodeId fieldDataType)
            {
                return fieldDataType.Equals(structureDataType);
            }
        }
    }

    /// <summary>
    /// Load all custom types from a server using the session and the type
    /// factory.
    /// </summary>
    /// <remarks>
    /// <para>The loader follows the following strategy:</para>
    /// <para>
    /// - Load all sub types DataType nodes of the Enumeration type.
    /// - Load all sub types DataType nodes of the Structure type.
    /// - Create all enum types using the EnumDefinition if available.
    /// - Create all remaining enum types using the EnumValues or EnumStrings
    ///   property, if available.
    /// - Create all structured types using the DataTypeDefinion, if available.
    /// </para>
    /// <para>
    /// if there are type definitions remaining
    /// - Load the binary schema dictionaries with type definitions.
    /// - Create all remaining enumerated custom types using the dictionaries.
    /// - Convert all structured types in the dictionaries to a
    ///   DataTypeDefinion, if possible.
    /// - Create all structured types from the dictionaries using the converted
    ///   DataTypeDefinion.
    /// </para>
    /// </remarks>
    /// <param name="ct"></param>
    /// <returns>true if all DataTypes were loaded.</returns>
    internal async ValueTask<bool> TryLoadAllDataTypesAsync(CancellationToken ct)
    {
        try
        {
            // load server types into cache
            await LoadDataTypesAsync(
                DataTypeIds.BaseDataType, true, ct: ct).ConfigureAwait(false);
            var serverEnumTypes = await LoadDataTypesAsync(
                DataTypeIds.Enumeration, ct: ct).ConfigureAwait(false);
            var serverStructTypes = await LoadDataTypesAsync(
                DataTypeIds.Structure, true, ct: ct).ConfigureAwait(false);

            var allTypesLoaded = false;
            // 1. Use the new data type definitions if available (>=V1.04)
            if (!DisableDataTypeDefinition)
            {
                var enumTypesToDoList = await LoadEnumDataTypesAsync(
                    RemoveKnownTypes(serverEnumTypes), ct).ConfigureAwait(false);
                var structTypesToDoList = await LoadStructureDataTypesAsync(
                    RemoveKnownTypes(serverStructTypes), ct).ConfigureAwait(false);

                allTypesLoaded =
                    enumTypesToDoList.Count == 0 &&
                    structTypesToDoList.Count == 0;
            }
            if (allTypesLoaded)
            {
                // Done
                return true;
            }
            // 2. Load the rest from the dictionaries (<= 1.03)
            if (!DisableDataTypeDictionary)
            {
                // strip known types from list
                serverEnumTypes = RemoveKnownTypes(serverEnumTypes);

                // Load the rest from the dictionaries (<= 1.03)
                await foreach (var type in _dictionaries.GetDictionaryDataTypesAsync(
                    serverEnumTypes, ct).ConfigureAwait(false))
                {
                    switch (type.Definition)
                    {
                        case StructureDefinition s:
                            Add(type.Node.NodeId, s, type.BinaryEncodingId,
                                type.XmlEncodingId, type.XmlName, type.Node.IsAbstract);
                            break;
                        case EnumDefinition e:
                            Add(type.Node.NodeId, e, type.XmlName, type.Node.IsAbstract);
                            break;
                        default:
                            break;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the custom types.");
            return false;
        }
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
    private void Add(ExpandedNodeId typeId, StructureDefinition definition,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
        XmlQualifiedName xmlName, bool isAbstract)
    {
        _structures[typeId] = StructureDescription.Create(this, typeId, definition,
            xmlName, binaryEncodingId, xmlEncodingId, ExpandedNodeId.Null, isAbstract);
    }

    /// <summary>
    /// Add type information
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="enumDefinition"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    /// <returns></returns>
    private void Add(ExpandedNodeId typeId, EnumDefinition enumDefinition,
        XmlQualifiedName xmlName, bool isAbstract)
    {
        _enums[typeId] = new EnumDescription(typeId, enumDefinition,
            xmlName, isAbstract);
    }

    /// <summary>
    /// Load all custom types with DataTypeDefinition into the type factory.
    /// </summary>
    /// <param name="serverEnumTypes"></param>
    /// <param name="ct"></param>
    /// <returns>true if all types were loaded, false otherwise</returns>
    private async ValueTask<List<INode>> LoadEnumDataTypesAsync(
        IReadOnlyList<INode> serverEnumTypes, CancellationToken ct)
    {
        // strip known types
        serverEnumTypes = RemoveKnownTypes(serverEnumTypes);

        var enumTypesToDoList = new List<INode>();
        foreach (var enumType in serverEnumTypes)
        {
            if (enumType is not DataTypeNode dataTypeNode ||
                !await AddEnumTypeAsync(dataTypeNode, ct).ConfigureAwait(false))
            {
                enumTypesToDoList.Add(enumType);
            }
        }
        // all types loaded, return remaining
        return enumTypesToDoList;
    }

    /// <summary>
    /// Load all structure custom types with DataTypeDefinition into
    /// the type factory.
    /// </summary>
    /// <param name="dataTypeNodes"></param>
    /// <param name="ct"></param>
    /// <returns>true if all types were loaded, false otherwise</returns>
    private async ValueTask<List<INode>> LoadStructureDataTypesAsync(
        IReadOnlyList<INode> dataTypeNodes, CancellationToken ct)
    {
        // strip known types
        dataTypeNodes = RemoveKnownTypes(dataTypeNodes);
        // cache the encodings
        await BrowseForEncodingsAsync(dataTypeNodes
            .Select(n => n.NodeId)
            .ToList(), kSupportedEncodings, ct).ConfigureAwait(false);

        // then add the structure types to the type cache
        var structTypesToDoList = new List<INode>();
        foreach (var structType in dataTypeNodes)
        {
            if (structType is not DataTypeNode dataTypeNode ||
                !await AddStructureTypeAsync(dataTypeNode, ct).ConfigureAwait(false))
            {
                structTypesToDoList.Add(structType);
            }
        }
        // all types loaded, return remaining
        return structTypesToDoList;
    }

    /// <summary>
    /// Add an structure type defined in a DataType node to the type cache.
    /// </summary>
    /// <param name="dataTypeNode"></param>
    /// <param name="ct"></param>
    private async ValueTask<bool> AddStructureTypeAsync(DataTypeNode dataTypeNode,
        CancellationToken ct)
    {
        var structureDefinition = GetStructureDefinition(dataTypeNode);
        if (structureDefinition == null)
        {
            return false;
        }
        var (encodingIds, binaryEncodingId, xmlEncodingId)
            = await BrowseForEncodingsAsync(dataTypeNode.NodeId,
                kSupportedEncodings, ct).ConfigureAwait(false);
        var typeId = NormalizeExpandedNodeId(dataTypeNode.NodeId);
        Add(typeId, structureDefinition, binaryEncodingId, xmlEncodingId,
            GetXmlNameFromBrowseName(dataTypeNode.BrowseName), dataTypeNode.IsAbstract);
        return true;
    }

    /// <summary>
    /// Add an enum type defined in a DataType node to the type cache.
    /// </summary>
    /// <param name="dataTypeNode"></param>
    /// <param name="ct"></param>
    private async ValueTask<bool> AddEnumTypeAsync(DataTypeNode dataTypeNode,
        CancellationToken ct)
    {
        var name = dataTypeNode.BrowseName;

        // 1. use DataTypeDefinition
        var enumDefinition = dataTypeNode.DataTypeDefinition?.Body as EnumDefinition;
        if (DisableDataTypeDefinition || enumDefinition == null)
        {
            // browse for EnumFields or EnumStrings property
            var enumTypeArray = await GetEnumTypeArrayAsync(dataTypeNode.NodeId,
                ct).ConfigureAwait(false);
            switch (enumTypeArray)
            {
                case ExtensionObject[] extensionObject:
                    // 2. use EnumValues
                    enumDefinition = extensionObject.ToEnumDefinition();
                    break;
                case LocalizedText[] localizedText:
                    // 3. use EnumStrings
                    enumDefinition = localizedText.ToEnumDefinition();
                    break;
                default:
                    // 4. Give up
                    enumDefinition = null;
                    return false;
            }
        }
        // Add EnumDefinition to cache
        Add(dataTypeNode.NodeId, enumDefinition,
            GetXmlNameFromBrowseName(dataTypeNode.BrowseName), dataTypeNode.IsAbstract);
        return true;
    }

    /// <summary>
    /// Get the encodings
    /// </summary>
    /// <param name="nodeIds"></param>
    /// <param name="supportedEncodings"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask<IReadOnlyList<NodeId>> BrowseForEncodingsAsync(
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
    private async ValueTask<(
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
    /// Load the data nodes for a base data type (structure or enumeration).
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="nestedSubTypes"></param>
    /// <param name="addRootNode"></param>
    /// <param name="filterUATypes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ServiceResultException"></exception>
    private async ValueTask<IReadOnlyList<INode>> LoadDataTypesAsync(
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

    /// <summary>
    /// Get enum strings
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask<object?> GetEnumTypeArrayAsync(ExpandedNodeId nodeId,
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
                ExpandedNodeId.ToNodeId(references[0].NodeId, _context.NamespaceUris),
                ct).ConfigureAwait(false);
            return value?.Value;
        }
        return null;
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
    /// Remove all known types in the type factory from a list of DataType nodes.
    /// </summary>
    /// <param name="nodeList"></param>
    private List<INode> RemoveKnownTypes(IReadOnlyList<INode> nodeList)
    {
        return nodeList.Where(node => !IsKnownType(node.NodeId))
            .Distinct()
            .ToList();
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
    /// Get an xml name from the browse name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private XmlQualifiedName GetXmlNameFromBrowseName(QualifiedName name)
    {
        return new XmlQualifiedName(name.Name,
        _context.NamespaceUris.GetString(name.NamespaceIndex));
    }

    /// <summary>
    /// Return the structure definition from a DataTypeDefinition
    /// </summary>
    /// <param name="dataTypeNode"></param>
    private static StructureDefinition? GetStructureDefinition(
        DataTypeNode dataTypeNode)
    {
        if (dataTypeNode.DataTypeDefinition?.Body is not
            StructureDefinition structureDefinition)
        {
            return null;
        }
        // Validate the DataTypeDefinition structure,
        // but not if the type is supported
        if (structureDefinition.Fields == null ||
            structureDefinition.BaseDataType.IsNullNodeId ||
            structureDefinition.BinaryEncodingId.IsNull)
        {
            return null;
        }
        // Validate the structure according to Part3, Table 36
        foreach (var field in structureDefinition.Fields)
        {
            // validate if the DataTypeDefinition is correctly
            // filled out, some servers don't do it yet...
            if (field.BinaryEncodingId.IsNull ||
                field.DataType.IsNullNodeId ||
                field.TypeId.IsNull ||
                field.Name == null)
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
    }

    private static readonly string[] kSupportedEncodings =
    [
        BrowseNames.DefaultBinary,
        BrowseNames.DefaultXml,
        BrowseNames.DefaultJson
    ];

    private readonly ILogger _logger;
    private readonly INodeCache _nodeCache;
    private readonly IServiceMessageContext _context;
    private readonly IEncodeableFactory _factory;
    private readonly DataTypeDictionaries _dictionaries;
    private readonly ConcurrentDictionary<ExpandedNodeId, StructureDescription> _structures = [];
    private readonly ConcurrentDictionary<ExpandedNodeId, EnumDescription> _enums = [];
}
