// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Logging;
using Opc.Ua.Schema.Binary;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// A dictionary data type
/// </summary>
/// <param name="Definition"></param>
/// <param name="XmlName"></param>
/// <param name="Node"></param>
/// <param name="Dictionary"></param>
internal sealed record class DictionaryDataType(DataTypeDefinition Definition,
    XmlQualifiedName XmlName, DataTypeNode Node, DataTypeDictionary Dictionary)
{
    public ExpandedNodeId BinaryEncodingId { get; internal set; }
    public ExpandedNodeId XmlEncodingId { get; internal set; }
}

/// <summary>
/// A class that holds the configuration for a UA service.
/// </summary>
internal sealed record class DataTypeDictionary(NodeId DictionaryId,
    string Name, NodeId TypeSystemId, string TypeSystemName,
    TypeDictionary? TypeDictionary,
    Dictionary<NodeId, QualifiedName> DataTypes);

/// <summary>
/// Resolve data types from the node cache of the session.
/// </summary>
internal sealed class DataTypeDictionaries
{
    /// <summary>
    /// Create type resolver
    /// </summary>
    /// <param name="nodeCache"></param>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public DataTypeDictionaries(INodeCache nodeCache, IServiceMessageContext context,
        ILogger<DataTypeDictionaries> logger)
    {
        _context = context;
        _logger = logger;
        _nodeCache = nodeCache;
    }

    /// <summary>
    /// Load listed custom types from dictionaries into cache. Loads all at once
    /// to avoid complexity when resolving type dependencies and given we have
    /// the dictionaries already open.
    /// </summary>
    /// <param name="serverEnumTypes"></param>
    /// <param name="ct"></param>
    /// <exception cref="ServiceResultException"></exception>
    public async IAsyncEnumerable<DictionaryDataType> GetDictionaryDataTypesAsync(
        IReadOnlyList<INode> serverEnumTypes, [EnumeratorCancellation] CancellationToken ct)
    {
        // build a type dictionary with all known new types
        var typeDictionary = new Dictionary<XmlQualifiedName, NodeId>();

        // load the binary schema dictionaries from the server
        var typeSystem = await LoadDataTypeSystemAsync(ct: ct).ConfigureAwait(false);

        // sort dictionaries with import dependencies to the end of the list
        var sortedTypeSystem = typeSystem
            .OrderBy(t => t.Value.TypeDictionary?.Import?.Length)
            .ToList();

        // create custom types for all dictionaries
        foreach (var dictionaryId in sortedTypeSystem)
        {
            var dictionary = dictionaryId.Value;
            if (dictionary.TypeDictionary?.Items == null)
            {
                continue;
            }

            // Add all unknown enumeration and structure types in dictionary
            var targetNamespace = dictionary.TypeDictionary.TargetNamespace;
            var targetNamespaceIndex = _context.NamespaceUris.GetIndex(targetNamespace);

            foreach (var item in dictionary.TypeDictionary.Items)
            {
                var qName = item.QName ?? new XmlQualifiedName(item.Name, targetNamespace);
                switch (item)
                {
                    case Schema.Binary.EnumeratedType enumeratedObject:
                        // Find the type already exists
                        var enumDataTypeNode = serverEnumTypes
                            .FirstOrDefault(node => node.BrowseName.Name == item.Name &&
                                node.BrowseName.NamespaceIndex == targetNamespaceIndex)
                            as DataTypeNode;
                        var enumDefinition = enumeratedObject.ToEnumDefinition();
                        if (enumDataTypeNode != null)
                        {
                            yield return new DictionaryDataType(enumDefinition,
                                qName, enumDataTypeNode, dictionary);
                            typeDictionary[qName] = enumDataTypeNode.NodeId;
                        }
                        break;
                    case Schema.Binary.StructuredType structuredObject:
                        var nodeId = dictionary.DataTypes
                            .FirstOrDefault(d => d.Value.Name == item.Name).Key;
                        if (NodeId.IsNull(nodeId))
                        {
                            _logger.LogError("Skip the type definition of {Type} because " +
                                "the data type node was not found.", item.Name);
                            break;
                        }

                        // find the data type node and the binary encoding id
                        (var typeId, var binaryEncodingId, var dataTypeNode) =
                            await BrowseTypeIdsForDictionaryComponentAsync(
                                nodeId, ct).ConfigureAwait(false);

                        if (dataTypeNode == null)
                        {
                            _logger.LogError("Skip the type definition of {Type} because" +
                                " the data type node was not found.", item.Name);
                            break;
                        }

                        // convert the binary schema to a StructureDefinition
                        var structureDefinition = structuredObject.ToStructureDefinition(
                            binaryEncodingId, typeDictionary,
                            _context.NamespaceUris, dataTypeNode.NodeId);
                        if (structureDefinition != null)
                        {
                            // Add structure definition
                            yield return new DictionaryDataType(structureDefinition, qName,
                                dataTypeNode, dictionary);
                            typeDictionary[qName] =
                                ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris)
                                    ?? NodeId.Null;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
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
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
            .ToList();

        // find namespace properties
        var namespaceReferences = await _nodeCache.GetReferencesAsync(
            referenceNodeIds, new[] { ReferenceTypeIds.HasProperty },
            false, false, ct).ConfigureAwait(false);
        var namespaceNodes = namespaceReferences
            .Where(n => n.BrowseName == BrowseNames.NamespaceUri)
            .ToList();
        var namespaceNodeIds = namespaceNodes
            .ConvertAll(n => ExpandedNodeId.ToNodeId(n.NodeId, _context.NamespaceUris));

        // read all schema definitions
        var referenceExpandedNodeIds = references
            .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
            .Where(n => n.NamespaceIndex != 0).ToList();
        var schemas = await ReadDictionariesAsync(
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
            var nodeId = ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris);
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
                    dictionaryToLoad = await LoadAsync(_nodeCache,
                        dictionaryId, dictionaryId.ToString(), _context, schema, imports,
                        ct).ConfigureAwait(false);
                }
                else
                {
                    dictionaryToLoad = await LoadAsync(_nodeCache,
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
    /// Get the type id for the dictionary component.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask<(ExpandedNodeId typeId, ExpandedNodeId encodingId,
        DataTypeNode? dataTypeNode)> BrowseTypeIdsForDictionaryComponentAsync(
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
            var values = await session.GetValuesAsync(dictionaryIds, ct).ConfigureAwait(false);
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
        internal static async Task<DataTypeDictionary> LoadAsync(INodeCache nodeCache,
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
            return new DataTypeDictionary(dictionaryId, name, typeSystemId, typeSystemName,
                typeDictionary, dataTypes);

            // Get the data types referenced by the type system
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
        public List<Schema.Binary.StructuredType> GetStructureTypes(DataTypeDictionary dictionary)
        {
            if (dictionary.TypeDictionary?.Items == null)
            {
                return [];
            }
            var structureList = new List<Schema.Binary.StructuredType>();
            foreach (var item in dictionary.TypeDictionary.Items)
            {
                if (item is Schema.Binary.StructuredType structuredObject)
                {
                    var dependentFields = structuredObject.Field
                        .Where(f => f.TypeName.Namespace ==
                            dictionary.TypeDictionary.TargetNamespace);
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
        public List<Schema.Binary.EnumeratedType> GetEnumTypes(DataTypeDictionary dictionary)
    {
            if (dictionary.TypeDictionary?.Items == null)
            {
                return [];
            }
            return dictionary.TypeDictionary.Items
                .OfType<Schema.Binary.EnumeratedType>()
                .ToList();
        }

    private readonly ConcurrentDictionary<NodeId, DataTypeDictionary> _cache = new();
    private readonly INodeCache _nodeCache;
    private readonly IServiceMessageContext _context;
    private readonly ILogger _logger;
}

