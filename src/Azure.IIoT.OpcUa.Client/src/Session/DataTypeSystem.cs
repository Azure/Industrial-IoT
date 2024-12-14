// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Logging;
using Opc.Ua.Client.Dynamic;
using Opc.Ua.Schema.Binary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// Manages the custom types of a server for a client session. Loads the
/// custom types into the type factory of a client session, to allow for
/// decoding and encoding of custom enumeration types and structured types.
/// </summary>
/// <remarks>
/// Support for V1.03 dictionaries and all V1.04 data type definitions
/// with the following known restrictions:
/// - Support only for V1.03 structured types which can be mapped to the
///   V1.04 structured type definition. Unsupported V1.03 types are ignored.
/// - V1.04 OptionSet does not create the enumeration flags.
/// </remarks>
internal class DataTypeSystem : IEncodeableFactory
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<ExpandedNodeId, Type> EncodeableTypes
        => _context.Factory.EncodeableTypes;

    /// <inheritdoc/>
    public int InstanceId => _context.Factory.InstanceId;

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
        return _context.Factory.GetSystemType(typeId);
    }

    /// <inheritdoc/>
    public object Clone()
    {
        return new DataTypeSystem(this);
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

    /// <summary>
    /// Get the data type definition and dependent definitions for a data type node id.
    /// Recursive through the cache to find all dependent types for strutures fields
    /// contained in the cache.
    /// </summary>
    /// <param name="dataTypeId"></param>
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
    /// <param name="onlyEnumTypes"></param>
    /// <param name="throwOnError"></param>
    /// <param name="ct"></param>
    /// <returns>true if all DataTypes were loaded.</returns>
    public async ValueTask<bool> LoadAllDataTypesAsync(bool onlyEnumTypes = false,
        bool throwOnError = false, CancellationToken ct = default)
    {
        try
        {
            // load server types in cache
            await _typeResolver.LoadDataTypesAsync(DataTypeIds.BaseDataType, true,
                ct: ct).ConfigureAwait(false);

            var serverEnumTypes = await _typeResolver.LoadDataTypesAsync(
                DataTypeIds.Enumeration, ct: ct).ConfigureAwait(false);
            var serverStructTypes = onlyEnumTypes ?
                new List<INode>() : await _typeResolver.LoadDataTypesAsync(
                DataTypeIds.Structure, true, ct: ct).ConfigureAwait(false);

            var allTypesLoaded = false;
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
                return true;
            }
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
            if (throwOnError)
            {
                throw;
            }
            return false;
        }
    }

    /// <summary>
    /// Get information for the structure type
    /// </summary>
    /// <param name="typeId"></param>
    /// <returns></returns>
    internal StructureDescription? GetStructureDescription(ExpandedNodeId typeId)
    {
        if (!_structures.TryGetValue(typeId, out var info))
        {
            return null;
        }
        return info;
    }

    /// <summary>
    /// Get the information about the enum type
    /// </summary>
    /// <param name="typeId"></param>
    /// <returns></returns>
    internal EnumDescription? GetEnumDescription(ExpandedNodeId typeId)
    {
        if (!_enums.TryGetValue(typeId, out var info))
        {
            return null;
        }
        return info;
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
    internal void Add(ExpandedNodeId typeId, StructureDefinition definition,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
        ExpandedNodeId jsonEncodingId, XmlQualifiedName xmlName)
    {
        _structures[typeId] = StructureDescription.Create(this, typeId, definition,
            xmlName, binaryEncodingId, xmlEncodingId, jsonEncodingId);
    }

    /// <summary>
    /// Add type information
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="enumDefinition"></param>
    /// <param name="xmlName"></param>
    /// <returns></returns>
    internal void Add(ExpandedNodeId typeId,
        EnumDefinition enumDefinition, XmlQualifiedName xmlName)
    {
        _enums[typeId] = new EnumDescription(typeId, enumDefinition, xmlName);
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
                await AddEnumTypesFromDictionaryAsync(typeDictionary, targetNamespace,
                    dictionary.GetEnumTypes(), allEnumTypes, serverEnumTypes,
                    ct).ConfigureAwait(false);
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
    /// <param name="enumList"></param>
    /// <param name="allEnumerationTypes"></param>
    /// <param name="enumerationTypes"></param>
    /// <param name="ct"></param>
    private async ValueTask AddEnumTypesFromDictionaryAsync(
        Dictionary<XmlQualifiedName, NodeId> typeDictionary, string targetNamespace,
        IReadOnlyList<Schema.Binary.TypeDescription> enumList,
        IReadOnlyList<INode> allEnumerationTypes, IReadOnlyList<INode> enumerationTypes,
        CancellationToken ct)
    {
        var targetNamespaceIndex = _typeResolver.NamespaceUris.GetIndex(targetNamespace);
        foreach (var item in enumList)
        {
            // Find the type already exists
            var enumDescription = enumerationTypes
                .FirstOrDefault(node => node.BrowseName.Name == item.Name &&
                    node.BrowseName.NamespaceIndex == targetNamespaceIndex)
                as DataTypeNode;

            if (enumDescription != null)
            {
                // try dictionary enum definition
                switch (item)
                {
                    case Schema.Binary.EnumeratedType enumeratedObject:
                        // 1. use Dictionary entry
                        var enumDefinition = enumeratedObject.ToEnumDefinition();
                        Add(enumDescription.NodeId, enumDefinition,
                            enumeratedObject.QName);
                        break;
                    default:
                        // 2. use node cache
                        if (await _typeResolver.FindAsync(enumDescription.NodeId,
                            ct).ConfigureAwait(false) is not DataTypeNode dataTypeNode)
                        {
                            // Not found, give up
                            break;
                        }
                        await AddEnumTypeAsync(dataTypeNode, ct).ConfigureAwait(false);
                        break;
                }
            }
            else
            {
                enumDescription = allEnumerationTypes
                    .FirstOrDefault(node => node.BrowseName.Name == item.Name &&
                        node.BrowseName.NamespaceIndex == targetNamespaceIndex)
                    as DataTypeNode;
            }
            if (enumDescription != null)
            {
                var qName = new XmlQualifiedName(item.Name, targetNamespace);
                typeDictionary[qName] = enumDescription.NodeId;
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
        Dictionary<XmlQualifiedName, NodeId> typeDictionary, DataDictionary dictionary,
        string targetNamespace, IReadOnlyList<TypeDescription> structureList,
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
                Add(typeId, structureDefinition, binaryEncodingId,
                    xmlEncodingId, ExpandedNodeId.Null,
                    GetXmlNameFromBrowseName(dataTypeNode.BrowseName));

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
        if (dataTypeNode.IsAbstract)
        {
            // Abstract types we do not cache (yet) as they are not needed
            return true;
        }
        var structureDefinition = GetStructureDefinition(dataTypeNode);
        if (structureDefinition == null)
        {
            return false;
        }
        var (encodingIds, binaryEncodingId, xmlEncodingId)
            = await _typeResolver.BrowseForEncodingsAsync(dataTypeNode.NodeId,
                kSupportedEncodings, ct).ConfigureAwait(false);
        var typeId = NormalizeExpandedNodeId(dataTypeNode.NodeId);
        Add(typeId, structureDefinition,
            binaryEncodingId, xmlEncodingId, ExpandedNodeId.Null,
            GetXmlNameFromBrowseName(dataTypeNode.BrowseName));
        return true;
    }

    /// <summary>
    /// Add an enum type defined in a DataType node to the type cache.
    /// </summary>
    /// <param name="enumTypeNode"></param>
    /// <param name="ct"></param>
    private async ValueTask<bool> AddEnumTypeAsync(DataTypeNode enumTypeNode,
        CancellationToken ct)
    {
        var name = enumTypeNode.BrowseName;

        // 1. use DataTypeDefinition
        var enumDefinition = enumTypeNode.DataTypeDefinition?.Body as EnumDefinition;
        if (DisableDataTypeDefinition || enumDefinition == null)
        {
            // browse for EnumFields or EnumStrings property
            var enumTypeArray = await _typeResolver.GetEnumTypeArrayAsync(
                enumTypeNode.NodeId, ct).ConfigureAwait(false);
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
        Add(enumTypeNode.NodeId, enumDefinition,
            GetXmlNameFromBrowseName(enumTypeNode.BrowseName));
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
        public async ValueTask<Dictionary<NodeId, DataDictionary>> LoadDataTypeSystemAsync(
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
            var schemas = await DataDictionary.ReadDictionariesAsync(
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
            var result = new Dictionary<NodeId, DataDictionary>();
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
                        dictionaryToLoad = await DataDictionary.LoadAsync(_nodeCache,
                            dictionaryId, dictionaryId.ToString(), _context, schema, imports,
                            ct).ConfigureAwait(false);
                    }
                    else
                    {
                        dictionaryToLoad = await DataDictionary.LoadAsync(_nodeCache,
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

        private readonly ConcurrentDictionary<NodeId, DataDictionary> _cache = new();
        private readonly INodeCache _nodeCache;
        private readonly IServiceMessageContext _context;
        private readonly ILogger _logger;
    }

    /// <summary>
    /// A class that holds the configuration for a UA service.
    /// </summary>
    internal sealed record class DataDictionary
    {
        /// <summary>
        /// The node id for the dictionary.
        /// </summary>
        public NodeId DictionaryId { get; }

        /// <summary>
        /// The display name for the dictionary.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The node id for the type system.
        /// </summary>
        public NodeId TypeSystemId { get; }

        /// <summary>
        /// The display name for the type system.
        /// </summary>
        public string TypeSystemName { get; }

        /// <summary>
        /// The type dictionary.
        /// </summary>
        public Schema.Binary.TypeDictionary? TypeDictionary { get; }

        /// <summary>
        /// The data type dictionary DataTypes
        /// </summary>
        public Dictionary<NodeId, QualifiedName> DataTypes { get; }

        /// <summary>
        /// Create dictionary
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <param name="name"></param>
        /// <param name="typeSystemId"></param>
        /// <param name="typeSystemName"></param>
        /// <param name="typeDictionary"></param>
        /// <param name="dataTypes"></param>
        internal DataDictionary(NodeId dictionaryId, string name, NodeId typeSystemId,
            string typeSystemName, Schema.Binary.TypeDictionary? typeDictionary,
            Dictionary<NodeId, QualifiedName> dataTypes)
        {
            DataTypes = dataTypes;
            TypeDictionary = typeDictionary;
            TypeSystemId = typeSystemId;
            TypeSystemName = typeSystemName;
            DictionaryId = dictionaryId;
            Name = name;
        }

        /// <summary>
        /// Reads the contents of multiple data dictionaries.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="dictionaryIds"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal static async Task<IDictionary<NodeId, byte[]>> ReadDictionariesAsync(
            INodeCache session, IReadOnlyList<NodeId> dictionaryIds,
            CancellationToken ct = default)
        {
            var result = new Dictionary<NodeId, byte[]>();
            if (dictionaryIds.Count == 0)
            {
                return result;
            }
            var values = await session.GetValuesAsync(
                dictionaryIds, ct).ConfigureAwait(false);

            Debug.Assert(dictionaryIds.Count == values.Count);
            Debug.Assert(dictionaryIds.Count == values.Count);
            for (var index = 0; index < dictionaryIds.Count; index++)
            {
                var nodeId = dictionaryIds[index];
                // check for error.
                if (StatusCode.IsBad(values[index].StatusCode))
                {
                    throw new ServiceResultException(values[index].StatusCode);
                }
                if (values[index].Value is byte[] buffer &&
                    !result.TryAdd(nodeId, buffer))
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Trying to add duplicate dictionary.");
                }
            }
            return result;
        }

        /// <summary>
        /// Loads the dictionary identified by the node id.
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="dictionaryId"></param>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <param name="schema"></param>
        /// <param name="imports"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionaryId"/> is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        internal static async Task<DataDictionary> LoadAsync(INodeCache nodeCache,
            NodeId dictionaryId, string name, IServiceMessageContext context,
            byte[]? schema = null, IDictionary<string, byte[]>? imports = null,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dictionaryId);

            var (typeSystemId, typeSystemName) = await GetTypeSystemAsync(
                nodeCache, dictionaryId, context, ct).ConfigureAwait(false);
            if (schema == null || schema.Length == 0)
            {
                schema = await ReadDictionaryAsync(nodeCache, dictionaryId,
                    ct).ConfigureAwait(false);
            }

            var zeroTerminator = Array.IndexOf<byte>(schema, 0);
            if (zeroTerminator >= 0)
            {
                Array.Resize(ref schema, zeroTerminator);
            }

            Schema.Binary.TypeDictionary? typeDictionary = null;
            var istrm = new MemoryStream(schema);
            await using (var _ = istrm.ConfigureAwait(false))
            {
                if (typeSystemId == Objects.XmlSchema_TypeSystem)
                {
                    var validator = new Schema.Xml.XmlSchemaValidator(imports);
                    validator.Validate(istrm);
                }

                if (typeSystemId == Objects.OPCBinarySchema_TypeSystem)
                {
                    var validator = new Schema.Binary.BinarySchemaValidator(imports);
                    validator.Validate(istrm);
                    typeDictionary = validator.Dictionary;
                }
            }

            var dataTypes = new Dictionary<NodeId, QualifiedName>();
            await ReadDataTypesAsync(nodeCache, dictionaryId, dataTypes, context,
                ct).ConfigureAwait(false);
            return new DataDictionary(dictionaryId, name, typeSystemId, typeSystemName,
                typeDictionary, dataTypes);

            static async Task<(NodeId, string)> GetTypeSystemAsync(INodeCache nodeCache,
                NodeId dictionaryId, IServiceMessageContext context, CancellationToken ct)
            {
                var references = await nodeCache.GetReferencesAsync(dictionaryId,
                    ReferenceTypeIds.HasComponent, true, false, ct).ConfigureAwait(false);
                return references.Count > 0
                    ? (ExpandedNodeId.ToNodeId(references[0].NodeId, context.NamespaceUris),
                        references[0].ToString()!)
                    : throw ServiceResultException.Create(StatusCodes.BadNotFound,
                        "Failed to get type system dictionary.");
            }
        }

        /// <summary>
        /// Retrieves the data types in the dictionary.
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="dictionaryId"></param>
        /// <param name="dataTypes"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <remarks>
        /// In order to allow for fast Linq matching of dictionary
        /// QNames with the data type nodes, the BrowseName of
        /// the DataType node is replaced with Value string.
        /// </remarks>
        /// <exception cref="ServiceResultException"></exception>
        private static async Task ReadDataTypesAsync(INodeCache nodeCache,
            NodeId dictionaryId, Dictionary<NodeId, QualifiedName> dataTypes,
            IServiceMessageContext context, CancellationToken ct)
        {
            var references = await nodeCache.GetReferencesAsync(dictionaryId,
                ReferenceTypeIds.HasComponent, false, false, ct).ConfigureAwait(false);
            var nodeIdCollection = references
                .Select(node => ExpandedNodeId.ToNodeId(node.NodeId, context.NamespaceUris))
                .ToList();

            // read the value to get the names that are used in the dictionary
            var values = await nodeCache.GetValuesAsync(nodeIdCollection,
                ct).ConfigureAwait(false);
            for (var index = 0; index < references.Count; index++)
            {
                var reference = references[index];
                var datatypeId = ExpandedNodeId.ToNodeId(reference.NodeId,
                    context.NamespaceUris);
                if (datatypeId != null && ServiceResult.IsGood(values[index].StatusCode) &&
                    !dataTypes.TryAdd(datatypeId,
                        new QualifiedName((string)values[index].Value,
                            datatypeId.NamespaceIndex)))
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Trying to add duplicate data type.");
                }
            }
        }

        /// <summary>
        /// Reads the contents of a data dictionary.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dictionaryId"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static async Task<byte[]> ReadDictionaryAsync(INodeCache context,
            NodeId dictionaryId, CancellationToken ct)
        {
            var data = await context.GetValueAsync(dictionaryId, ct).ConfigureAwait(false);
            // return as a byte array.
            if (data.Value is not byte[] dictionary || dictionary.Length == 0)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Found empty data dictionary.");
            }
            return dictionary;
        }

        /// <summary>
        /// Get structure types
        /// </summary>
        /// <returns></returns>
        public List<Schema.Binary.StructuredType> GetStructureTypes()
        {
            if (TypeDictionary?.Items == null)
            {
                return [];
            }
            var structureList = new List<Schema.Binary.StructuredType>();
            foreach (var item in TypeDictionary.Items)
            {
                if (item is Schema.Binary.StructuredType structuredObject)
                {
                    var dependentFields = structuredObject.Field
                        .Where(f => f.TypeName.Namespace ==
                            TypeDictionary.TargetNamespace);
                    if (!dependentFields.Any())
                    {
                        structureList.Insert(0, structuredObject);
                    }
                    else
                    {
                        structureList.Add(structuredObject);
                    }
                }
            }
            return structureList;
        }

        /// <summary>
        /// Get enums from the dictionary.
        /// </summary>
        /// <returns></returns>
        public List<Schema.Binary.EnumeratedType> GetEnumTypes()
        {
            if (TypeDictionary?.Items == null)
            {
                return [];
            }
            return TypeDictionary.Items
                .OfType<Schema.Binary.EnumeratedType>()
                .ToList();
        }
    }

    private static readonly string[] kSupportedEncodings =
    [
        BrowseNames.DefaultBinary,
        BrowseNames.DefaultXml,
        BrowseNames.DefaultJson
    ];

    private readonly ILogger _logger;
    private readonly IServiceMessageContext _context;
    private readonly DataTypeLoader _typeResolver;
    private readonly ConcurrentDictionary<ExpandedNodeId, StructureDescription> _structures = [];
    private readonly ConcurrentDictionary<ExpandedNodeId, EnumDescription> _enums = [];
}
