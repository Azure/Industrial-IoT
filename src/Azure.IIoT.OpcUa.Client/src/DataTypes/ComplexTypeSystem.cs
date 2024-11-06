/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes
{
    using static Opc.Ua.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Extensions.Logging;
    using static System.Collections.Specialized.BitVector32;

    /// <summary>
    /// Manages the custom types of a server for a client session.
    /// Loads the custom types into the type factory
    /// of a client session, to allow for decoding and encoding
    /// of custom enumeration types and structured types.
    /// </summary>
    /// <remarks>
    /// Support for V1.03 dictionaries and all V1.04 data type definitions
    /// with the following known restrictions:
    /// - Support only for V1.03 structured types which can be mapped to the V1.04
    /// structured type definition. Unsupported V1.03 types are ignored.
    /// - V1.04 OptionSet does not create the enumeration flags.
    /// </remarks>
    public class ComplexTypeSystem
    {
        // an internal limit to prevent the retry
        // datatype loader mechanism to loop forever
        private const int MaxLoopCount = 100;

        /// <summary>
        /// Initializes the type system with a session to load the custom types.
        /// </summary>
        /// <param name="session"></param>
        public ComplexTypeSystem(ISession session)
        {
            m_logger = session.LoggerFactory.CreateLogger<ComplexTypeSystem>();
            m_complexTypeResolver = new NodeCacheResolver(session);
            m_complexTypeBuilderFactory = new ComplexTypeBuilderFactory();
        }

        /// <summary>
        /// Load all custom types from a server into the session system type factory.
        /// </summary>
        /// <param name="onlyEnumTypes"></param>
        /// <param name="throwOnError"></param>
        /// <param name="ct"></param>
        /// <remarks>
        /// The loader follows the following strategy:
        /// - Load all DataType nodes of the Enumeration subtypes.
        /// - Load all DataType nodes of the Structure subtypes.
        /// - Create all enumerated custom types using the DataTypeDefinion attribute, if available.
        /// - Create all remaining enumerated custom types using the EnumValues or EnumStrings property, if available.
        /// - Create all structured types using the DataTypeDefinion attribute, if available.
        /// if there are type definitions remaining
        /// - Load the binary schema dictionaries with type definitions.
        /// - Create all remaining enumerated custom types using the dictionaries.
        /// - Convert all structured types in the dictionaries to the DataTypeDefinion attribute, if possible.
        /// - Create all structured types from the dictionaries using the converted DataTypeDefinion attribute.
        /// </remarks>
        /// <returns>true if all DataTypes were loaded.</returns>
        public async Task<bool> Load(bool onlyEnumTypes = false, bool throwOnError = false, CancellationToken ct = default)
        {
            try
            {
                // load server types in cache
                await m_complexTypeResolver.LoadDataTypesAsync(DataTypeIds.BaseDataType, true, ct: ct).ConfigureAwait(false);
                var serverEnumTypes = await m_complexTypeResolver.LoadDataTypesAsync(DataTypeIds.Enumeration, ct: ct).ConfigureAwait(false);
                var serverStructTypes = onlyEnumTypes ? new List<INode>() :
                    await m_complexTypeResolver.LoadDataTypesAsync(DataTypeIds.Structure, true, ct: ct).ConfigureAwait(false);
                if (DisableDataTypeDefinition || !await LoadBaseDataTypesAsync(serverEnumTypes, serverStructTypes, ct).ConfigureAwait(false))
                {
                    if (DisableDataTypeDictionary)
                    {
                        return false;
                    }
                    return await LoadDictionaryDataTypes(serverEnumTypes, serverStructTypes, true, ct).ConfigureAwait(false);
                }
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to load the custom types.");
                if (throwOnError)
                {
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Get the data type definition and dependent definitions for a data type node id.
        /// Recursive through the cache to find all dependent types for strutures fields
        /// contained in the cache.
        /// </summary>
        /// <param name="dataTypeId"></param>
        public NodeIdDictionary<DataTypeDefinition> GetDataTypeDefinitionsForDataType(ExpandedNodeId dataTypeId)
        {
            var dataTypeDefinitions = new NodeIdDictionary<DataTypeDefinition>();

            var dataTypeNodeId = ExpandedNodeId.ToNodeId(dataTypeId, m_complexTypeResolver.NamespaceUris);
            if (!NodeId.IsNull(dataTypeNodeId))
            {
                CollectAllDataTypeDefinitions(dataTypeNodeId, dataTypeDefinitions);
            }

            return dataTypeDefinitions;

            void CollectAllDataTypeDefinitions(NodeId nodeId, NodeIdDictionary<DataTypeDefinition> collect)
            {
                if (NodeId.IsNull(nodeId))
                {
                    return;
                }

                if (m_dataTypeDefinitionCache.TryGetValue(nodeId, out var dataTypeDefinition))
                {
                    collect[nodeId] = dataTypeDefinition;

                    if (dataTypeDefinition is StructureDefinition structureDefinition)
                    {
                        foreach (var field in structureDefinition.Fields)
                        {
                            if (!IsRecursiveDataType(nodeId, field.DataType) &&
                                !collect.ContainsKey(nodeId))
                            {
                                CollectAllDataTypeDefinitions(field.DataType, collect);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Disable the use of DataTypeDefinition to create the complex type definition.
        /// </summary>
        internal bool DisableDataTypeDefinition { get; set; }

        /// <summary>
        /// Disable the use of DataType Dictionaries to create the complex type definition.
        /// </summary>
        internal bool DisableDataTypeDictionary { get; set; }

        /// <summary>
        /// Load listed custom types from dictionaries
        /// into the sessions system type factory.
        /// </summary>
        /// <param name="serverEnumTypes"></param>
        /// <param name="serverStructTypes"></param>
        /// <param name="fullTypeList"></param>
        /// <param name="ct"></param>
        /// <remarks>
        /// Loads all custom types at this time to avoid
        /// complexity when resolving type dependencies.
        /// </remarks>
        private async Task<bool> LoadDictionaryDataTypes(
            IList<INode> serverEnumTypes,
            IList<INode> serverStructTypes,
            bool fullTypeList,
            CancellationToken ct = default
            )
        {
            // build a type dictionary with all known new types
            var allEnumTypes = fullTypeList ? serverEnumTypes : await m_complexTypeResolver.LoadDataTypesAsync(DataTypeIds.Enumeration, ct: ct).ConfigureAwait(false);
            var typeDictionary = new Dictionary<XmlQualifiedName, NodeId>();

            // strip known types from list
            serverEnumTypes = RemoveKnownTypes(allEnumTypes);

            // load the binary schema dictionaries from the server
            var typeSystem = await m_complexTypeResolver.LoadDataTypeSystem(ct: ct).ConfigureAwait(false);

            // sort dictionaries with import dependencies to the end of the list
            var sortedTypeSystem = typeSystem.OrderBy(t => t.Value.TypeDictionary?.Import?.Length).ToList();

            var allTypesLoaded = true;

            // create custom types for all dictionaries
            foreach (var dictionaryId in sortedTypeSystem)
            {
                try
                {
                    var dictionary = dictionaryId.Value;
                    if (dictionary.TypeDictionary == null ||
                        dictionary.TypeDictionary.Items == null)
                    {
                        continue;
                    }
                    var targetDictionaryNamespace = dictionary.TypeDictionary.TargetNamespace;
                    var targetNamespaceIndex = m_complexTypeResolver.NamespaceUris.GetIndex(targetDictionaryNamespace);
                    var structureList = new List<Schema.Binary.TypeDescription>();
                    var enumList = new List<Schema.Binary.TypeDescription>();

                    // split into enumeration and structure types and sort
                    // types with dependencies to the end of the list.
                    SplitAndSortDictionary(dictionary, structureList, enumList);

                    // create assembly for all types in the same module
                    var complexTypeBuilder = m_complexTypeBuilderFactory.Create(
                        targetDictionaryNamespace,
                        targetNamespaceIndex,
                        dictionary.Name);

                    // Add all unknown enumeration types in dictionary
                    await AddEnumTypesAsync(complexTypeBuilder, typeDictionary, enumList, allEnumTypes, serverEnumTypes, ct).ConfigureAwait(false);

                    // handle structures
                    var loopCounter = 0;
                    var lastStructureCount = 0;
                    while (structureList.Count > 0 &&
                        structureList.Count != lastStructureCount &&
                        loopCounter < MaxLoopCount)
                    {
                        loopCounter++;
                        lastStructureCount = structureList.Count;
                        var retryStructureList = new List<Schema.Binary.TypeDescription>();
                        // build structured types
                        foreach (var item in structureList)
                        {
                            if (item is Schema.Binary.StructuredType structuredObject)
                            {
                                var nodeId = dictionary.DataTypes.FirstOrDefault(d => d.Value.Name == item.Name).Key;
                                if (nodeId == null)
                                {
                                    m_logger.LogError(TraceMasks.Error, "Skip the type definition of {Type} because the data type node was not found.", item.Name);
                                    continue;
                                }

                                // find the data type node and the binary encoding id
                                (var typeId, var binaryEncodingId, var dataTypeNode) =
                                    await m_complexTypeResolver.BrowseTypeIdsForDictionaryComponentAsync(nodeId, ct).ConfigureAwait(false);

                                if (dataTypeNode == null)
                                {
                                    m_logger.LogError(TraceMasks.Error, "Skip the type definition of {Type} because the data type node was not found.", item.Name);
                                    continue;
                                }

                                if (GetSystemType(typeId) != null)
                                {
                                    var qName = structuredObject.QName ?? new XmlQualifiedName(structuredObject.Name, targetDictionaryNamespace);
                                    typeDictionary[qName] = ExpandedNodeId.ToNodeId(typeId, m_complexTypeResolver.NamespaceUris);
                                    m_logger.LogInformation("Skip the type definition of {Type} because the type already exists.", item.Name);
                                    continue;
                                }

                                // Use DataTypeDefinition attribute, if available (>=V1.04)
                                StructureDefinition structureDefinition = null;
                                if (!DisableDataTypeDefinition)
                                {
                                    structureDefinition = GetStructureDefinition(dataTypeNode);
                                }
                                if (structureDefinition == null)
                                {
                                    try
                                    {
                                        // convert the binary schema description to a StructureDefinition
                                        structureDefinition = structuredObject.ToStructureDefinition(
                                            binaryEncodingId,
                                            typeDictionary,
                                            m_complexTypeResolver.NamespaceUris,
                                            dataTypeNode.NodeId);
                                    }
                                    catch (DataTypeNotSupportedException)
                                    {
                                        m_logger.LogError("Skipped the type definition of {Type} because it is not supported.", item.Name);
                                        continue;
                                    }
                                    catch (ServiceResultException sre)
                                    {
                                        m_logger.LogError(sre, "Skip the type definition of {Type}.", item.Name);
                                        continue;
                                    }
                                }

                                ExpandedNodeIdCollection missingTypeIds = null;
                                Type complexType = null;
                                if (structureDefinition != null)
                                {
                                    IList<NodeId> encodingIds;
                                    ExpandedNodeId xmlEncodingId;
                                    (encodingIds, binaryEncodingId, xmlEncodingId) = await m_complexTypeResolver.BrowseForEncodingsAsync(
                                        typeId, m_supportedEncodings, ct).ConfigureAwait(false);
                                    try
                                    {
                                        // build the actual .NET structured type in assembly
                                        (complexType, missingTypeIds) = await AddStructuredTypeAsync(
                                            complexTypeBuilder,
                                            structureDefinition,
                                            dataTypeNode.BrowseName,
                                            typeId,
                                            binaryEncodingId,
                                            xmlEncodingId,
                                            ct
                                            ).ConfigureAwait(false);
                                    }
                                    catch (DataTypeNotSupportedException typeNotSupportedException)
                                    {
                                        m_logger.LogInformation(typeNotSupportedException,
                                            "Skipped the type definition of {Type} because it is not supported.", item.Name);
                                        continue;
                                    }

                                    // Add new type to factory
                                    if (complexType != null)
                                    {
                                        // match namespace and add new type to type factory
                                        foreach (var encodingId in encodingIds)
                                        {
                                            AddEncodeableType(encodingId, complexType);
                                        }
                                        AddEncodeableType(typeId, complexType);
                                        var qName = structuredObject.QName ?? new XmlQualifiedName(structuredObject.Name, targetDictionaryNamespace);
                                        typeDictionary[qName] = ExpandedNodeId.ToNodeId(typeId, m_complexTypeResolver.NamespaceUris);
                                    }
                                }

                                if (complexType == null)
                                {
                                    retryStructureList.Add(item);
                                    m_logger.LogTrace("Skipped the type definition of {Type}, missing {Missing}. Retry in next round.", item.Name, missingTypeIds?.ToString() ?? string.Empty);
                                }
                            }
                        }
                        structureList = retryStructureList;
                    }
                    allTypesLoaded = allTypesLoaded && structureList.Count == 0;
                }
                catch (ServiceResultException sre)
                {
                    m_logger.LogError(sre, "Unexpected error processing {Entry}.", dictionaryId.Value.Name);
                }
            }
            return allTypesLoaded;
        }

        /// <summary>
        /// Load all custom types with DataTypeDefinition into the type factory.
        /// </summary>
        /// <param name="serverEnumTypes"></param>
        /// <param name="serverStructTypes"></param>
        /// <param name="ct"></param>
        /// <returns>true if all types were loaded, false otherwise</returns>
        private async Task<bool> LoadBaseDataTypesAsync(
            IList<INode> serverEnumTypes,
            IList<INode> serverStructTypes,
            CancellationToken ct = default
            )
        {
            IList<INode> enumTypesToDoList = new List<INode>();
            IList<INode> structTypesToDoList = new List<INode>();

            bool repeatDataTypeLoad;
            do
            {
                // strip known types
                serverEnumTypes = RemoveKnownTypes(serverEnumTypes);
                serverStructTypes = RemoveKnownTypes(serverStructTypes);

                repeatDataTypeLoad = false;
                try
                {
                    enumTypesToDoList = await LoadBaseEnumDataTypesAsync(serverEnumTypes, ct).ConfigureAwait(false);
                    structTypesToDoList = await LoadBaseStructureDataTypesAsync(serverStructTypes, ct).ConfigureAwait(false);
                }
                catch (DataTypeNotFoundException dtnfex)
                {
                    m_logger.LogWarning("{Message}", dtnfex.Message);
                    foreach (var nodeId in dtnfex.NodeIds)
                    {
                        // add missing types to list
                        var dataTypeNode = await m_complexTypeResolver.FindAsync(nodeId, ct).ConfigureAwait(false);
                        if (dataTypeNode != null)
                        {
                            await AddEnumerationOrStructureTypeAsync(dataTypeNode, serverEnumTypes, serverStructTypes, ct).ConfigureAwait(false);
                            repeatDataTypeLoad = true;
                        }
                        else
                        {
                            m_logger.LogWarning("Datatype {Type} was not found.", nodeId);
                        }
                    }
                }
            } while (repeatDataTypeLoad);

            // all types loaded
            return enumTypesToDoList.Count == 0 && structTypesToDoList.Count == 0;
        }

        /// <summary>
        /// Load all custom types with DataTypeDefinition into the type factory.
        /// </summary>
        /// <param name="serverEnumTypes"></param>
        /// <param name="ct"></param>
        /// <returns>true if all types were loaded, false otherwise</returns>
        private async Task<IList<INode>> LoadBaseEnumDataTypesAsync(
            IList<INode> serverEnumTypes,
            CancellationToken ct = default
            )
        {
            // strip known types
            serverEnumTypes = RemoveKnownTypes(serverEnumTypes);

            // add new enum Types for all namespaces
            var enumTypesToDoList = new List<INode>();
            var namespaceCount = m_complexTypeResolver.NamespaceUris.Count;

            // create enumeration types for all namespaces
            for (uint i = 0; i < namespaceCount; i++)
            {
                IComplexTypeBuilder complexTypeBuilder = null;
                var enumTypes = serverEnumTypes.Where(node => node.NodeId.NamespaceIndex == i).ToList();
                if (enumTypes.Count != 0)
                {
                    if (complexTypeBuilder == null)
                    {
                        var targetNamespace = m_complexTypeResolver.NamespaceUris.GetString(i);
                        complexTypeBuilder = m_complexTypeBuilderFactory.Create(
                            targetNamespace,
                            (int)i);
                    }
                    foreach (var enumType in enumTypes)
                    {
                        var newType = await AddEnumTypeAsync(complexTypeBuilder, enumType as DataTypeNode, ct).ConfigureAwait(false);
                        if (newType != null)
                        {
                            // match namespace and add to type factory
                            AddEncodeableType(enumType.NodeId, newType);
                        }
                        else
                        {
                            enumTypesToDoList.Add(enumType);
                        }
                    }
                }
            }

            // all types loaded, return remaining
            return enumTypesToDoList;
        }

        /// <summary>
        /// Load all structure custom types with DataTypeDefinition into the type factory.
        /// </summary>
        /// <param name="serverStructTypes"></param>
        /// <param name="ct"></param>
        /// <returns>true if all types were loaded, false otherwise</returns>
        private async Task<IList<INode>> LoadBaseStructureDataTypesAsync(
            IList<INode> serverStructTypes,
            CancellationToken ct = default
            )
        {
            // strip known types
            serverStructTypes = RemoveKnownTypes(serverStructTypes);

            // add new enum Types for all namespaces
            var namespaceCount = m_complexTypeResolver.NamespaceUris.Count;

            bool retryAddStructType;
            var structTypesToDoList = new List<INode>();
            var structTypesWorkList = serverStructTypes;

            // allow the loader to cache the encodings
            IList<ExpandedNodeId> nodeIds = serverStructTypes.Select(n => n.NodeId).ToList();
            _ = await m_complexTypeResolver.BrowseForEncodingsAsync(nodeIds, m_supportedEncodings, ct).ConfigureAwait(false);

            // create structured types for all namespaces
            var loopCounter = 0;
            do
            {
                loopCounter++;
                retryAddStructType = false;
                for (uint i = 0; i < namespaceCount; i++)
                {
                    IComplexTypeBuilder complexTypeBuilder = null;
                    var structTypes = structTypesWorkList.Where(node => node.NodeId.NamespaceIndex == i).ToList();
                    if (structTypes.Count != 0)
                    {
                        if (complexTypeBuilder == null)
                        {
                            var targetNamespace = m_complexTypeResolver.NamespaceUris.GetString(i);
                            complexTypeBuilder = m_complexTypeBuilderFactory.Create(
                                targetNamespace,
                                (int)i);
                        }
                        foreach (var structType in structTypes)
                        {
                            Type newType = null;
                            if (!(structType is DataTypeNode dataTypeNode) ||
                                dataTypeNode.IsAbstract)
                            {
                                continue;
                            }

                            var structureDefinition = GetStructureDefinition(dataTypeNode);
                            if (structureDefinition != null)
                            {
                                (var encodingIds, var binaryEncodingId, var xmlEncodingId)
                                    = await m_complexTypeResolver.BrowseForEncodingsAsync(structType.NodeId, m_supportedEncodings, ct).ConfigureAwait(false);
                                try
                                {
                                    var typeId = NormalizeExpandedNodeId(structType.NodeId);
                                    ExpandedNodeIdCollection missingTypeIds;
                                    (newType, missingTypeIds) = await AddStructuredTypeAsync(
                                        complexTypeBuilder,
                                        structureDefinition,
                                        dataTypeNode.BrowseName,
                                        typeId,
                                        binaryEncodingId,
                                        xmlEncodingId,
                                        ct
                                        ).ConfigureAwait(false);

                                    if (missingTypeIds?.Count > 0)
                                    {
                                        var missingTypeIdsFromWorkList = new ExpandedNodeIdCollection();
                                        foreach (var missingTypeId in missingTypeIds)
                                        {
                                            var typeMatch = structTypesWorkList.FirstOrDefault(n => n.NodeId == missingTypeId);
                                            if (typeMatch == null)
                                            {
                                                missingTypeIdsFromWorkList.Add(missingTypeId);
                                            }
                                        }
                                        foreach (var id in missingTypeIdsFromWorkList)
                                        {
                                            if (!structTypesToDoList.Where(n => n.NodeId == id).Any())
                                            {
                                                structTypesToDoList.Add(await m_complexTypeResolver.FindAsync(id, ct).ConfigureAwait(false));
                                            }
                                            retryAddStructType = true;
                                        }
                                    }
                                }
                                catch (DataTypeNotSupportedException)
                                {
                                    m_logger.LogError("Skipped the type definition of {Type} because it is not supported.", dataTypeNode.BrowseName.Name);
                                }
                                catch
                                {
                                    // creating the new type failed, likely a missing dependency, retry later
                                    retryAddStructType = true;
                                }

                                if (newType != null)
                                {
                                    foreach (var encodingId in encodingIds)
                                    {
                                        AddEncodeableType(encodingId, newType);
                                    }
                                    AddEncodeableType(structType.NodeId, newType);
                                }
                            }

                            if (newType == null)
                            {
                                structTypesToDoList.Add(structType);
                                if (structureDefinition != null)
                                {
                                    retryAddStructType = true;
                                }
                            }
                        }
                    }
                }
                // due to type dependencies, retry missing types until there is no more progress
                if (retryAddStructType &&
                    structTypesWorkList.Count != structTypesToDoList.Count &&
                    loopCounter < MaxLoopCount)
                {
                    structTypesWorkList = structTypesToDoList;
                    structTypesToDoList = new List<INode>();
                }
                else
                {
                    break;
                }
            } while (retryAddStructType);

            // all types loaded
            return structTypesToDoList;
        }

        /// <summary>
        /// Return the structure definition from a DataTypeDefinition
        /// </summary>
        /// <param name="dataTypeNode"></param>
        private static StructureDefinition GetStructureDefinition(DataTypeNode dataTypeNode)
        {
            if (dataTypeNode.DataTypeDefinition?.Body is StructureDefinition structureDefinition)
            {
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
                    if (!(field.ValueRank == ValueRanks.Scalar ||
                        field.ValueRank >= ValueRanks.OneDimension))
                    {
                        return null;
                    }
                }
                return structureDefinition;
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
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, m_complexTypeResolver.NamespaceUris);
            return NodeId.ToExpandedNodeId(nodeId, m_complexTypeResolver.NamespaceUris);
        }

        /// <summary>
        /// Add data type to enumeration or structure base type list depending on supertype.
        /// </summary>
        /// <param name="dataTypeNode"></param>
        /// <param name="serverEnumTypes"></param>
        /// <param name="serverStructTypes"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task AddEnumerationOrStructureTypeAsync(INode dataTypeNode, IList<INode> serverEnumTypes, IList<INode> serverStructTypes, CancellationToken ct = default)
        {
            var superType = ExpandedNodeId.ToNodeId(dataTypeNode.NodeId, m_complexTypeResolver.NamespaceUris);
            while (true)
            {
                superType = await m_complexTypeResolver.FindSuperTypeAsync(superType, ct).ConfigureAwait(false);
                if (superType.IsNullNodeId)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdInvalid, $"SuperType for {dataTypeNode.NodeId} not found.");
                }
                if (superType == DataTypeIds.Enumeration)
                {
                    serverEnumTypes.Insert(0, dataTypeNode);
                    break;
                }
                else if (superType == DataTypeIds.Structure)
                {
                    serverStructTypes.Insert(0, dataTypeNode);
                    break;
                }
                else if (TypeInfo.GetBuiltInType(superType) != BuiltInType.Null)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Remove all known types in the type factory from a list of DataType nodes.
        /// </summary>
        /// <param name="nodeList"></param>
        private List<INode> RemoveKnownTypes(IList<INode> nodeList)
        {
            return nodeList.Where(
                node => GetSystemType(node.NodeId) == null).Distinct().ToList();
        }

        /// <summary>
        /// Get the factory system type for an expanded node id.
        /// </summary>
        /// <param name="nodeId"></param>
        private Type GetSystemType(ExpandedNodeId nodeId)
        {
            if (!nodeId.IsAbsolute)
            {
                nodeId = NormalizeExpandedNodeId(nodeId);
            }
            return m_complexTypeResolver.Factory.GetSystemType(nodeId);
        }

        /// <summary>
        /// Add an enum type defined in a binary schema dictionary.
        /// </summary>
        /// <param name="complexTypeBuilder"></param>
        /// <param name="typeDictionary"></param>
        /// <param name="enumList"></param>
        /// <param name="allEnumerationTypes"></param>
        /// <param name="enumerationTypes"></param>
        /// <param name="ct"></param>
        private async Task AddEnumTypesAsync(
            IComplexTypeBuilder complexTypeBuilder,
            Dictionary<XmlQualifiedName, NodeId> typeDictionary,
            IList<Schema.Binary.TypeDescription> enumList,
            IList<INode> allEnumerationTypes,
            IList<INode> enumerationTypes,
            CancellationToken ct = default)
        {
            foreach (var item in enumList)
            {
                Type newType = null;
                DataTypeNode enumDescription = null;
                var enumType = enumerationTypes.FirstOrDefault(node =>
                    node.BrowseName.Name == item.Name &&
                    (node.BrowseName.NamespaceIndex == complexTypeBuilder.TargetNamespaceIndex ||
                    complexTypeBuilder.TargetNamespaceIndex == -1))
                    as DataTypeNode;
                if (enumType != null)
                {
                    enumDescription = enumType;

                    // try dictionary enum definition
                    if (item is Schema.Binary.EnumeratedType enumeratedObject)
                    {
                        // 1. use Dictionary entry
                        var enumDefinition = enumeratedObject.ToEnumDefinition();

                        // Add EnumDefinition to cache
                        m_dataTypeDefinitionCache[enumType.NodeId] = enumDefinition;

                        newType = complexTypeBuilder.AddEnumType(enumeratedObject.Name, enumDefinition);
                    }
                    if (newType == null)
                    {
                        // 2. use node cache
                        var dataTypeNode = await m_complexTypeResolver.FindAsync(enumType.NodeId, ct).ConfigureAwait(false) as DataTypeNode;
                        newType = await AddEnumTypeAsync(complexTypeBuilder, dataTypeNode, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    enumDescription = allEnumerationTypes.FirstOrDefault(node =>
                        node.BrowseName.Name == item.Name &&
                        (node.BrowseName.NamespaceIndex == complexTypeBuilder.TargetNamespaceIndex ||
                        complexTypeBuilder.TargetNamespaceIndex == -1))
                        as DataTypeNode;
                }
                if (enumDescription != null)
                {
                    var qName = new XmlQualifiedName(item.Name, complexTypeBuilder.TargetNamespace);
                    typeDictionary[qName] = enumDescription.NodeId;
                }
                if (newType != null)
                {
                    // match namespace and add to type factory
                    AddEncodeableType(enumType.NodeId, newType);
                }
            }
        }

        /// <summary>
        /// Helper to add new type with absolute ExpandedNodeId.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="type"></param>
        private void AddEncodeableType(ExpandedNodeId nodeId, Type type)
        {
            if (NodeId.IsNull(nodeId) || type == null)
            {
                return;
            }
            var internalNodeId = NormalizeExpandedNodeId(nodeId);
            m_logger.LogDebug("Adding Type {Type} as: {TypeId}", type.FullName, internalNodeId);
            m_complexTypeResolver.Factory.AddEncodeableType(internalNodeId, type);
        }

        /// <summary>
        /// Add an enum type defined in a DataType node.
        /// </summary>
        /// <param name="complexTypeBuilder"></param>
        /// <param name="enumTypeNode"></param>
        /// <param name="ct"></param>
        private async Task<Type> AddEnumTypeAsync(
            IComplexTypeBuilder complexTypeBuilder,
            DataTypeNode enumTypeNode,
            CancellationToken ct = default
            )
        {
            Type newType = null;
            if (enumTypeNode != null)
            {
                var name = enumTypeNode.BrowseName;

                // 1. use DataTypeDefinition
                if (DisableDataTypeDefinition ||
                    !(enumTypeNode.DataTypeDefinition?.Body is EnumDefinition enumDefinition))
                {
                    // browse for EnumFields or EnumStrings property
                    var enumTypeArray = await m_complexTypeResolver.GetEnumTypeArrayAsync(enumTypeNode.NodeId, ct).ConfigureAwait(false);
                    if (enumTypeArray is ExtensionObject[] extensionObject)
                    {
                        // 2. use EnumValues
                        enumDefinition = extensionObject.ToEnumDefinition();
                    }
                    else if (enumTypeArray is LocalizedText[] localizedText)
                    {
                        // 3. use EnumStrings
                        enumDefinition = localizedText.ToEnumDefinition();
                    }
                    else
                    {
                        // 4. Give up
                        enumDefinition = null;
                    }
                }

                if (enumDefinition != null)
                {
                    // Add EnumDefinition to cache
                    m_dataTypeDefinitionCache[enumTypeNode.NodeId] = enumDefinition;

                    newType = complexTypeBuilder.AddEnumType(name, enumDefinition);
                }
            }
            return newType;
        }

        /// <summary>
        /// Add structured type to assembly with StructureDefinition.
        /// </summary>
        /// <param name="complexTypeBuilder"></param>
        /// <param name="structureDefinition"></param>
        /// <param name="typeName"></param>
        /// <param name="complexTypeId"></param>
        /// <param name="binaryEncodingId"></param>
        /// <param name="xmlEncodingId"></param>
        /// <param name="ct"></param>
        private async Task<(Type structureType, ExpandedNodeIdCollection missingTypes)> AddStructuredTypeAsync(
            IComplexTypeBuilder complexTypeBuilder,
            StructureDefinition structureDefinition,
            QualifiedName typeName,
            ExpandedNodeId complexTypeId,
            ExpandedNodeId binaryEncodingId,
            ExpandedNodeId xmlEncodingId,
            CancellationToken ct = default
            )
        {
            // init missing type list
            ExpandedNodeIdCollection missingTypes = null;

            var localDataTypeId = ExpandedNodeId.ToNodeId(complexTypeId, m_complexTypeResolver.NamespaceUris);
            var allowSubTypes = IsAllowSubTypes(structureDefinition);

            // check all types
            var typeList = new List<Type>();
            foreach (var field in structureDefinition.Fields)
            {
                var newType = await GetFieldTypeAsync(field, allowSubTypes, ct).ConfigureAwait(false);
                if (newType == null &&
                    !IsRecursiveDataType(localDataTypeId, field.DataType))
                {
                    if (missingTypes == null)
                    {
                        missingTypes = new ExpandedNodeIdCollection() { field.DataType };
                    }
                    else if (!missingTypes.Contains(field.DataType))
                    {
                        missingTypes.Add(field.DataType);
                    }
                }
                else
                {
                    typeList.Add(newType);
                }
            }

            if (missingTypes != null)
            {
                return (null, missingTypes);
            }

            // Add StructureDefinition to cache
            m_dataTypeDefinitionCache[localDataTypeId] = structureDefinition;

            var fieldBuilder = complexTypeBuilder.AddStructuredType(
                typeName,
                structureDefinition
                );

            fieldBuilder.AddTypeIdAttribute(complexTypeId, binaryEncodingId, xmlEncodingId);

            var order = 1;
            var typeListEnumerator = typeList.GetEnumerator();
            foreach (var field in structureDefinition.Fields)
            {
                typeListEnumerator.MoveNext();

                // check for recursive data type:
                //    field has the same data type as the parent structure
                var nodeId = ExpandedNodeId.ToNodeId(complexTypeId, m_complexTypeResolver.NamespaceUris);
                var isRecursiveDataType = IsRecursiveDataType(nodeId, field.DataType);
                if (isRecursiveDataType)
                {
                    fieldBuilder.AddField(field, fieldBuilder.GetStructureType(field.ValueRank), order);
                }
                else
                {
                    fieldBuilder.AddField(field, typeListEnumerator.Current, order);
                }
                order++;
            }

            return (fieldBuilder.CreateType(), missingTypes);
        }

        static bool IsAllowSubTypes(StructureDefinition structureDefinition)
        {
            switch (structureDefinition.StructureType)
            {
                case StructureType.UnionWithSubtypedValues:
                case StructureType.StructureWithSubtypedValues:
                    return true;
            }
            return false;
        }

        private async Task<bool> IsAbstractTypeAsync(NodeId fieldDataType, CancellationToken ct = default)
        {
            var dataTypeNode = await m_complexTypeResolver.FindAsync(fieldDataType, ct).ConfigureAwait(false) as DataTypeNode;
            return dataTypeNode?.IsAbstract == true;
        }

        private static bool IsRecursiveDataType(NodeId structureDataType, NodeId fieldDataType)
            => fieldDataType.Equals(structureDataType);

        /// <summary>
        /// Determine the type of a field in a StructureField definition.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="allowSubTypes"></param>
        /// <param name="ct"></param>
        /// <exception cref="DataTypeNotSupportedException"></exception>
        private async Task<Type> GetFieldTypeAsync(StructureField field, bool allowSubTypes, CancellationToken ct = default)
        {
            if (field.ValueRank != ValueRanks.Scalar &&
                field.ValueRank < ValueRanks.OneDimension)
            {
                throw new DataTypeNotSupportedException(field.DataType, $"The ValueRank {field.ValueRank} is not supported.");
            }

            var fieldType = field.DataType.NamespaceIndex == 0 ?
                TypeInfo.GetSystemType(field.DataType, m_complexTypeResolver.Factory) :
                GetSystemType(field.DataType);

            if (fieldType == null)
            {
                var superType = await GetBuiltInSuperTypeAsync(field.DataType, allowSubTypes, field.IsOptional, ct).ConfigureAwait(false);
                if (superType?.IsNullNodeId == false)
                {
                    field.DataType = superType;
                    return await GetFieldTypeAsync(field, allowSubTypes, ct).ConfigureAwait(false);
                }
                return null;
            }

            if (field.ValueRank == ValueRanks.OneDimension)
            {
                fieldType = fieldType.MakeArrayType();
            }
            else if (field.ValueRank >= ValueRanks.TwoDimensions)
            {
                fieldType = fieldType.MakeArrayType(field.ValueRank);
            }

            return fieldType;
        }

        /// <summary>
        /// Find superType for a datatype.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="allowSubTypes"></param>
        /// <param name="isOptional"></param>
        /// <param name="ct"></param>
        /// <exception cref="DataTypeNotSupportedException"></exception>
        private async Task<NodeId> GetBuiltInSuperTypeAsync(NodeId dataType, bool allowSubTypes, bool isOptional, CancellationToken ct = default)
        {
            const int MaxSuperTypes = 100;

            var iterations = 0;
            var superType = dataType;
            while (iterations++ < MaxSuperTypes)
            {
                superType = await m_complexTypeResolver.FindSuperTypeAsync(superType, ct).ConfigureAwait(false);
                if (superType?.IsNullNodeId != false)
                {
                    return null;
                }
                if (superType.NamespaceIndex == 0)
                {
                    if (superType == DataTypeIds.Enumeration &&
                        dataType.NamespaceIndex == 0)
                    {
                        // enumerations of namespace 0 in a structure
                        // which are not in the type system are encoded as UInt32
                        return new NodeId((uint)BuiltInType.UInt32);
                    }
                    if (superType == DataTypeIds.Enumeration)
                    {
                        return null;
                    }
                    else if (superType == DataTypeIds.Structure)
                    {
                        // throw on invalid combinations of allowSubTypes, isOptional and abstract types
                        // in such case the encoding as ExtensionObject is undetermined and not specified
                        if ((dataType != DataTypeIds.Structure) &&
                            ((allowSubTypes && !isOptional) || !allowSubTypes) &&
                            await IsAbstractTypeAsync(dataType, ct).ConfigureAwait(false))
                        {
                            throw new DataTypeNotSupportedException("Invalid definition of a abstract subtype of a structure.");
                        }

                        if (allowSubTypes && isOptional)
                        {
                            return superType;
                        }
                        return null;
                    }
                    // end search if a valid BuiltInType is found. Treat type as opaque.
                    else if (superType.IdType == IdType.Numeric &&
                        (uint)superType.Identifier >= (uint)BuiltInType.Boolean &&
                        (uint)superType.Identifier <= (uint)BuiltInType.DiagnosticInfo)
                    {
                        return superType;
                    }
                    // no valid supertype found
                    else if (superType == DataTypeIds.BaseDataType)
                    {
                        break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Split the dictionary types into a list of structures and enumerations.
        /// Sort the structures by dependencies, with structures with dependent
        /// types at the end of the list, so they can be added to the factory in order.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="structureList"></param>
        /// <param name="enumList"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static void SplitAndSortDictionary(
            DataDictionary dictionary,
            List<Schema.Binary.TypeDescription> structureList,
            List<Schema.Binary.TypeDescription> enumList
            )
        {
            foreach (var item in dictionary.TypeDictionary.Items)
            {
                if (item is Schema.Binary.StructuredType structuredObject)
                {
                    var dependentFields = structuredObject.Field.Where(f => f.TypeName.Namespace == dictionary.TypeDictionary.TargetNamespace);
                    if (!dependentFields.Any())
                    {
                        structureList.Insert(0, structuredObject);
                    }
                    else
                    {
                        structureList.Add(structuredObject);
                    }
                }
                else if (item is Schema.Binary.EnumeratedType)
                {
                    enumList.Add(item);
                }
                else if (item is Schema.Binary.OpaqueType)
                {
                    // no need to handle Opaque types
                }
                else
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Unexpected Type in binary schema: {0}.", item.GetType().Name);
                }
            }
        }

        private NodeCacheResolver m_complexTypeResolver;
        private ComplexTypeBuilderFactory m_complexTypeBuilderFactory;
        private readonly NodeIdDictionary<DataTypeDefinition> m_dataTypeDefinitionCache = new();
        private static readonly string[] m_supportedEncodings = new string[] { BrowseNames.DefaultBinary, BrowseNames.DefaultXml, BrowseNames.DefaultJson };
        private readonly ILogger m_logger;
    }
}
