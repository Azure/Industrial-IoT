// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        _typeResolver = new DataTypeLoader(nodeCache, context,
            loggerFactory.CreateLogger<DataTypeLoader>());
    }

    /// <summary>
    /// Clone constructor
    /// </summary>
    /// <param name="complexTypeSystem"></param>
    private DataTypeSystem(DataTypeSystem complexTypeSystem)
    {
        _logger = complexTypeSystem._logger;
        _context = complexTypeSystem._context;
        _factory = complexTypeSystem._factory;
        _typeResolver = complexTypeSystem._typeResolver;
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
            await _typeResolver.LoadDataTypesAsync(
                DataTypeIds.BaseDataType, true, ct: ct).ConfigureAwait(false);
            var serverEnumTypes = await _typeResolver.LoadDataTypesAsync(
                DataTypeIds.Enumeration, ct: ct).ConfigureAwait(false);
            var serverStructTypes = await _typeResolver.LoadDataTypesAsync(
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
                // Load the rest from the dictionaries (<= 1.03)
                return await LoadDictionaryDataTypesAsync(true,
                    serverEnumTypes, ct).ConfigureAwait(false);
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
    /// <param name="jsonEncodingId"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    internal void Add(ExpandedNodeId typeId, StructureDefinition definition,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
        ExpandedNodeId jsonEncodingId, XmlQualifiedName xmlName, bool isAbstract)
    {
        _structures[typeId] = StructureDescription.Create(this, typeId, definition,
            xmlName, binaryEncodingId, xmlEncodingId, jsonEncodingId, isAbstract);
    }

    /// <summary>
    /// Add type information
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="enumDefinition"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    /// <returns></returns>
    internal void Add(ExpandedNodeId typeId, EnumDefinition enumDefinition,
        XmlQualifiedName xmlName, bool isAbstract)
    {
        _enums[typeId] = new EnumDescription(typeId, enumDefinition,
            xmlName, isAbstract);
    }

    /// <summary>
    /// Load listed custom types from dictionaries into cache. Loads all at once
    /// to avoid complexity when resolving type dependencies and given we have
    /// the dictionaries already open.
    /// </summary>
    /// <param name="fullTypeList"></param>
    /// <param name="serverEnumTypes"></param>
    /// <param name="ct"></param>
    /// <exception cref="ServiceResultException"></exception>
    private async ValueTask<bool> LoadDictionaryDataTypesAsync(bool fullTypeList,
        IReadOnlyList<INode> serverEnumTypes, CancellationToken ct)
    {
        // build a type dictionary with all known new types
        var allEnumTypes = fullTypeList ? serverEnumTypes :
            await _typeResolver.LoadDataTypesAsync(DataTypeIds.Enumeration,
                ct: ct).ConfigureAwait(false);
        var typeDictionary = new Dictionary<XmlQualifiedName, NodeId>();
        // strip known types from list
        serverEnumTypes = RemoveKnownTypes(allEnumTypes);

        // load the binary schema dictionaries from the server
        var typeSystem = await _typeResolver.LoadDataTypeSystemAsync(
            ct: ct).ConfigureAwait(false);

        // sort dictionaries with import dependencies to the end of the list
        var sortedTypeSystem = typeSystem
            .OrderBy(t => t.Value.TypeDictionary?.Import?.Length)
            .ToList();

        var allTypesLoaded = true;
        // create custom types for all dictionaries
        foreach (var dictionaryId in sortedTypeSystem)
        {
            try
            {
                var dictionary = dictionaryId.Value;
                if (dictionary.TypeDictionary?.Items == null)
                {
                    continue;
                }

                // Add all unknown enumeration and structure types in dictionary
                var targetNamespace = dictionary.TypeDictionary.TargetNamespace;
                AddEnumTypesFromDictionary(typeDictionary, targetNamespace,
                    dictionary.GetEnumTypes(), allEnumTypes);
                await AddStructureTypesFromDictionaryAsync(typeDictionary, dictionary,
                    targetNamespace, dictionary.GetStructureTypes(),
                    ct).ConfigureAwait(false);
            }
            catch (ServiceResultException sre)
            {
                _logger.LogError(sre, "Unexpected error processing {Entry}.",
                    dictionaryId.Value.Name);
                allTypesLoaded = false;
            }
        }
        return allTypesLoaded;
    }

    /// <summary>
    /// Add all enum types defined in a binary schema dictionary.
    /// </summary>
    /// <param name="typeDictionary"></param>
    /// <param name="targetNamespace"></param>
    /// <param name="enumTypes"></param>
    /// <param name="allEnumNodes"></param>
    private void AddEnumTypesFromDictionary(
        Dictionary<XmlQualifiedName, NodeId> typeDictionary, string targetNamespace,
        IReadOnlyList<Schema.Binary.EnumeratedType> enumTypes, IReadOnlyList<INode> allEnumNodes)
    {
        var targetNamespaceIndex = _typeResolver.NamespaceUris.GetIndex(targetNamespace);
        foreach (var item in enumTypes)
        {
            // Find the type already exists
            var enumDataTypeNode = allEnumNodes
                .FirstOrDefault(node => node.BrowseName.Name == item.Name &&
                    node.BrowseName.NamespaceIndex == targetNamespaceIndex)
                as DataTypeNode;

            var enumDefinition = item.ToEnumDefinition();
            if (enumDataTypeNode != null)
            {
                Add(enumDataTypeNode.NodeId, enumDefinition, item.QName,
                    enumDataTypeNode.IsAbstract);
                var qName = new XmlQualifiedName(item.Name, targetNamespace);
                typeDictionary[qName] = enumDataTypeNode.NodeId;
            }
        }
    }

    /// <summary>
    /// Add structure types from data dictionary
    /// </summary>
    /// <param name="typeDictionary"></param>
    /// <param name="dictionary"></param>
    /// <param name="targetNamespace"></param>
    /// <param name="structureList"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask AddStructureTypesFromDictionaryAsync(
        Dictionary<XmlQualifiedName, NodeId> typeDictionary, DataTypeDictionary dictionary,
        string targetNamespace, IReadOnlyList<Schema.Binary.StructuredType> structureList,
        CancellationToken ct)
    {
        // build structured types
        foreach (var item in structureList)
        {
            if (item is not Schema.Binary.StructuredType structuredObject)
            {
                continue;
            }
            var nodeId = dictionary.DataTypes
                .FirstOrDefault(d => d.Value.Name == item.Name).Key;
            if (NodeId.IsNull(nodeId))
            {
                _logger.LogError("Skip the type definition of {Type} because " +
                    "the data type node was not found.", item.Name);
                continue;
            }

            // find the data type node and the binary encoding id
            (var typeId, var binaryEncodingId, var dataTypeNode) =
                await _typeResolver.BrowseTypeIdsForDictionaryComponentAsync(
                    nodeId, ct).ConfigureAwait(false);

            if (dataTypeNode == null)
            {
                _logger.LogError("Skip the type definition of {Type} because" +
                    " the data type node was not found.", item.Name);
                continue;
            }

            if (IsKnownType(typeId))
            {
                var qName = structuredObject.QName ?? new XmlQualifiedName(
                    structuredObject.Name, targetNamespace);
                typeDictionary[qName] =
                    ExpandedNodeId.ToNodeId(typeId, _typeResolver.NamespaceUris)
                        ?? NodeId.Null;
                _logger.LogInformation("Skip the type definition of " +
                    "{Type} because the type already exists.", item.Name);
                continue;
            }

            // Use DataTypeDefinition attribute, if available (>=V1.04)
            StructureDefinition? structureDefinition = null;
            if (!DisableDataTypeDefinition)
            {
                structureDefinition = GetStructureDefinition(dataTypeNode);
            }
            if (structureDefinition == null)
            {
                try
                {
                    // convert the binary schema to a StructureDefinition
                    structureDefinition = structuredObject.ToStructureDefinition(
                        binaryEncodingId, typeDictionary,
                        _typeResolver.NamespaceUris, dataTypeNode.NodeId);
                }
                catch (ServiceResultException sre)
                {
                    _logger.LogError(sre, "Skip the type definition of {Type}.",
                        item.Name);
                    continue;
                }
            }
            if (structureDefinition != null)
            {
                // Add structure definition
                (var encodingIds, binaryEncodingId, var xmlEncodingId) =
                    await _typeResolver.BrowseForEncodingsAsync(typeId, kSupportedEncodings,
                    ct).ConfigureAwait(false);
                Add(typeId, structureDefinition, binaryEncodingId, xmlEncodingId,
                    ExpandedNodeId.Null, GetXmlNameFromBrowseName(dataTypeNode.BrowseName),
                    dataTypeNode.IsAbstract);

                var qName = structuredObject.QName ?? new XmlQualifiedName(
                    structuredObject.Name, targetNamespace);
                typeDictionary[qName] =
                    ExpandedNodeId.ToNodeId(typeId, _typeResolver.NamespaceUris)
                        ?? NodeId.Null;
            }
        }
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
        await _typeResolver.BrowseForEncodingsAsync(dataTypeNodes
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
            = await _typeResolver.BrowseForEncodingsAsync(dataTypeNode.NodeId,
                kSupportedEncodings, ct).ConfigureAwait(false);
        var typeId = NormalizeExpandedNodeId(dataTypeNode.NodeId);
        Add(typeId, structureDefinition, binaryEncodingId, xmlEncodingId,
            ExpandedNodeId.Null, GetXmlNameFromBrowseName(dataTypeNode.BrowseName),
            dataTypeNode.IsAbstract);
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
            var enumTypeArray = await _typeResolver.GetEnumTypeArrayAsync(
                dataTypeNode.NodeId, ct).ConfigureAwait(false);
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
    /// Helper to ensure the expanded nodeId contains a valid namespaceUri.
    /// </summary>
    /// <param name="expandedNodeId">The expanded nodeId.</param>
    /// <returns>The normalized expanded nodeId.</returns>
    private ExpandedNodeId NormalizeExpandedNodeId(ExpandedNodeId expandedNodeId)
    {
        var nodeId = ExpandedNodeId.ToNodeId(
            expandedNodeId, _typeResolver.NamespaceUris);
        return NodeId.ToExpandedNodeId(nodeId, _typeResolver.NamespaceUris)
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
        _typeResolver.NamespaceUris.GetString(name.NamespaceIndex));
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
    private readonly IServiceMessageContext _context;
    private readonly IEncodeableFactory _factory;
    private readonly DataTypeLoader _typeResolver;
    private readonly ConcurrentDictionary<ExpandedNodeId, StructureDescription> _structures = [];
    private readonly ConcurrentDictionary<ExpandedNodeId, EnumDescription> _enums = [];
}
