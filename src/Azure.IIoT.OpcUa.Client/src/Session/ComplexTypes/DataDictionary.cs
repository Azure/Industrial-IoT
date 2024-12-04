// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A class that holds the configuration for a UA service.
/// </summary>
internal sealed class DataDictionary
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
    private DataDictionary(NodeId dictionaryId, string name,
        NodeId typeSystemId, string typeSystemName,
        Schema.Binary.TypeDictionary? typeDictionary,
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
        IComplexTypeContext session, IReadOnlyList<NodeId> dictionaryIds,
        CancellationToken ct = default)
    {
        var result = new Dictionary<NodeId, byte[]>();
        if (dictionaryIds.Count == 0)
        {
            return result;
        }
        var (values, errors) = await session.FetchValuesAsync(null,
            dictionaryIds, ct).ConfigureAwait(false);

        Debug.Assert(dictionaryIds.Count == values.Count);
        Debug.Assert(dictionaryIds.Count == values.Count);
        for (var index = 0; index < dictionaryIds.Count; index++)
        {
            var nodeId = dictionaryIds[index];
            // check for error.
            if (StatusCode.IsBad(errors[index].StatusCode))
            {
                throw new ServiceResultException(errors[index]);
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
    /// <param name="context"></param>
    /// <param name="dictionaryId"></param>
    /// <param name="name"></param>
    /// <param name="schema"></param>
    /// <param name="imports"></param>
    /// <param name="ct"></param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="dictionaryId"/> is <c>null</c>.</exception>
    /// <exception cref="ServiceResultException"></exception>
    internal static async Task<DataDictionary> LoadAsync(IComplexTypeContext context,
        NodeId dictionaryId, string name, byte[]? schema = null,
        IDictionary<string, byte[]>? imports = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dictionaryId);

        var (typeSystemId, typeSystemName) = await GetTypeSystemAsync(
            context, dictionaryId, ct).ConfigureAwait(false);
        if (schema == null || schema.Length == 0)
        {
            schema = await ReadDictionaryAsync(context, dictionaryId,
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
        await ReadDataTypesAsync(context, dictionaryId, dataTypes, ct).ConfigureAwait(false);
        return new DataDictionary(dictionaryId, name, typeSystemId, typeSystemName,
            typeDictionary, dataTypes);

        static async Task<(NodeId, string)> GetTypeSystemAsync(
            IComplexTypeContext context, NodeId dictionaryId, CancellationToken ct)
        {
            var references = await context.NodeCache.GetReferencesAsync(dictionaryId,
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
    /// <param name="context"></param>
    /// <param name="dictionaryId"></param>
    /// <param name="dataTypes"></param>
    /// <param name="ct"></param>
    /// <remarks>
    /// In order to allow for fast Linq matching of dictionary
    /// QNames with the data type nodes, the BrowseName of
    /// the DataType node is replaced with Value string.
    /// </remarks>
    /// <exception cref="ServiceResultException"></exception>
    private static async Task ReadDataTypesAsync(IComplexTypeContext context,
        NodeId dictionaryId, Dictionary<NodeId, QualifiedName> dataTypes,
        CancellationToken ct)
    {
        var references = await context.NodeCache.GetReferencesAsync(dictionaryId,
            ReferenceTypeIds.HasComponent, false, false, ct).ConfigureAwait(false);
        var nodeIdCollection = references
            .Select(node => ExpandedNodeId.ToNodeId(node.NodeId, context.NamespaceUris))
            .ToList();

        // read the value to get the names that are used in the dictionary
        var (values, errors) = await context.FetchValuesAsync(null, nodeIdCollection,
            ct).ConfigureAwait(false);
        for (var index = 0; index < references.Count; index++)
        {
            var reference = references[index];
            var datatypeId = ExpandedNodeId.ToNodeId(reference.NodeId,
                context.NamespaceUris);
            if (datatypeId != null && ServiceResult.IsGood(errors[index]) &&
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
    private static async Task<byte[]> ReadDictionaryAsync(IComplexTypeContext context,
        NodeId dictionaryId, CancellationToken ct)
    {
        var data = await context.FetchValueAsync(null, dictionaryId, ct).ConfigureAwait(false);
        // return as a byte array.
        if (data.Value is not byte[] dictionary || dictionary.Length == 0)
        {
            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                "Found empty data dictionary.");
        }
        return dictionary;
    }
}
