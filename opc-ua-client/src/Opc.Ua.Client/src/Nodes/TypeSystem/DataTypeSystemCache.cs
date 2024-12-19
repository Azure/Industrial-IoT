// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Nodes.TypeSystem;

using Opc.Ua.Client.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

/// <summary>
/// Caches and parses data type dictionaries in the form supported before 1.04.
/// <see href="https://reference.opcfoundation.org/Core/Part5/v104/docs/D"/> and
/// <see href="https://reference.opcfoundation.org/Core/Part5/v104/docs/E"/> for
/// more information.
/// </summary>
/// <remarks>
/// Support for V1.03 dictionaries with the following known restrictions:
/// - Only Binary and Xml type systems are currently supported.
/// - Structured types are mapped to the V1.04 structured type definition.
/// - Enumerated types are mapped to the V1.04 enum definition.
/// - V1.04 OptionSet are not supported.
/// - When a type is not found and a dictionary must be loaded the whole
///   dictionary is loaded and parsed and all types are added.
/// </remarks>
internal sealed class DataTypeSystemCache : IDataTypeSystemCache
{
    /// <summary>
    /// Create the data type system cache
    /// </summary>
    /// <param name="nodeCache"></param>
    /// <param name="context"></param>
    /// <param name="loggerFactory"></param>
    public DataTypeSystemCache(INodeCache nodeCache,
        IServiceMessageContext context, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        var binary = new Lazy<Task<DataTypeSystem>>(async () =>
        {
            var binary = new OPCBinarySchema(nodeCache, context,
                _loggerFactory.CreateLogger<OPCBinarySchema>());
            await binary.LoadAsync(default).ConfigureAwait(false);
            return binary;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        var xml = new Lazy<Task<DataTypeSystem>>(async () =>
        {
            var xml = new XmlSchema(nodeCache, context,
                _loggerFactory.CreateLogger<XmlSchema>());
            await xml.LoadAsync(default).ConfigureAwait(false);
            return xml;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        _systems.TryAdd(BrowseNames.DefaultBinary, binary);
        _systems.TryAdd(BrowseNames.DefaultXml, xml);
    }

    /// <inheritdoc/>
    public ValueTask<DictionaryDataTypeDefinition?> GetDataTypeDefinitionAsync(
        QualifiedName encoding, ExpandedNodeId dataTypeId, CancellationToken ct)
    {
        if (!_systems.TryGetValue(encoding, out var typeSystem))
        {
            throw new ServiceResultException(StatusCodes.BadEncodingError,
                $"Unsupported encoding {encoding}.");
        }
        if (typeSystem.Value.IsCompletedSuccessfully)
        {
            return typeSystem.Value.Result.GetDataTypeDefinitionAsync(dataTypeId, ct);
        }
        return GetDataTypeDefinitionAsyncCore(typeSystem, dataTypeId, ct);
        async ValueTask<DictionaryDataTypeDefinition?> GetDataTypeDefinitionAsyncCore(
             Lazy<Task<DataTypeSystem>> typeSystem, ExpandedNodeId dataTypeId,
             CancellationToken ct)
        {
            var ts = await typeSystem.Value.ConfigureAwait(false);
            return await ts.GetDataTypeDefinitionAsync(dataTypeId, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Opc binary schema type system
    /// </summary>
    private sealed record class OPCBinarySchema : DataTypeSystem
    {
        /// <inheritdoc/>
        public override NodeId TypeSystemId => Objects.OPCBinarySchema_TypeSystem;
        /// <inheritdoc/>
        public override QualifiedName TypeSystemName => BrowseNames.OPCBinarySchema_TypeSystem;
        /// <inheritdoc/>
        public override QualifiedName EncodingName => BrowseNames.DefaultBinary;

        /// <inheritdoc/>
        public OPCBinarySchema(INodeCache nodeCache, IServiceMessageContext context,
            ILogger<OPCBinarySchema> logger)
            : base(nodeCache, context, logger)
        {
        }

        /// <inheritdoc/>
        protected override DataTypeDictionary Load(NodeId dictionaryId,
            string targetNamespace, byte[] buffer, Dictionary<string, byte[]> imports)
        {
            using var istrm = new MemoryStream(buffer);
            var validator = new Schema.Binary.BinarySchemaValidator(imports);
            validator.Validate(istrm);
            return new DataTypeDictionary(dictionaryId,
                targetNamespace, TypeSystemId, TypeSystemName, validator.Dictionary, null);
        }

        /// <inheritdoc/>
        protected override void LoadDictionaryDataTypeDefinitions(Dictionary<XmlQualifiedName,
            (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
            DataTypeDictionary dictionary, NamespaceTable namespaceUris)
        {
            foreach (var item in dictionary.TypeDictionary!.Items)
            {
                var qName = item.QName ??
                    new XmlQualifiedName(item.Name, dictionary.Namespace);
                if (!typeDictionary.TryGetValue(qName, out var entry))
                {
                    continue;
                }
                switch (item)
                {
                    case Schema.Binary.EnumeratedType enumeratedObject:
                        var enumDefinition = new DictionaryDataTypeDefinition(
                            ToEnumDefinition(enumeratedObject),
                            qName, entry.EncodingId);
                        Add(entry.EncodingId, entry.TypeId, enumDefinition);
                        break;
                    case Schema.Binary.StructuredType structuredObject:
                        var structureDefinition = new DictionaryDataTypeDefinition(
                            ToStructureDefinition(structuredObject, entry.EncodingId,
                            typeDictionary, namespaceUris, entry.TypeId),
                            qName, entry.EncodingId);
                        Add(entry.EncodingId, entry.TypeId, structureDefinition);
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Convert a binary schema type definition to a
        /// StructureDefinition.
        /// </summary>
        /// <param name="structuredType"></param>
        /// <param name="defaultEncodingId"></param>
        /// <param name="typeDictionary"></param>
        /// <param name="namespaceTable"></param>
        /// <param name="dataTypeNodeId"></param>
        /// <remarks>
        /// Support for:
        /// - Structures, structures with optional fields and unions.
        /// - Nested types and typed arrays with length field.
        /// The converter has the following known restrictions:
        /// - Support only for V1.03 structured types which can be mapped to the V1.04
        ///   structured type definition.
        /// The following dictionary tags cause bail out for a structure:
        /// - use of a terminator of length in bytes
        /// - an array length field is not a direct predecessor of the array
        /// - The switch value of a union is not the first field.
        /// - The selector bits of optional fields are not stored in a 32 bit variable
        ///   and do not add up to 32 bits.
        /// </remarks>
        /// <exception cref="ServiceResultException"></exception>
        public static StructureDefinition ToStructureDefinition(
            Schema.Binary.StructuredType structuredType, ExpandedNodeId defaultEncodingId,
            Dictionary<XmlQualifiedName,
                (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
            NamespaceTable namespaceTable, ExpandedNodeId dataTypeNodeId)
        {
            var structureDefinition = new StructureDefinition
            {
                BaseDataType = null,
                DefaultEncodingId =
                    ExpandedNodeId.ToNodeId(defaultEncodingId, namespaceTable),
                Fields = [],
                StructureType = StructureType.Structure
            };

            var hasBitField = false;
            var isUnionType = false;
            foreach (var field in structuredType.Field)
            {
                // check for yet unsupported properties
                if (field.IsLengthInBytes ||
                    field.Terminator != null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                        "The structure definition uses a Terminator or " +
                        "LengthInBytes, which are not supported.");
                }

                if (field.SwitchValue != 0)
                {
                    isUnionType = true;
                }

                if (field.TypeName.Namespace is Namespaces.OpcBinarySchema or
                    Namespaces.OpcUa && field.TypeName.Name == "Bit")
                {
                    hasBitField = true;
                    continue;
                }
                if (field.Length != 0)
                {
                    throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                        "A structure field has a length field which is not supported.");
                }
            }

            if (isUnionType && hasBitField)
            {
                throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                    "The structure definition combines a Union and a bit filed," +
                    " both of which are not supported in a single structure.");
            }

            if (isUnionType)
            {
                structureDefinition.StructureType = StructureType.Union;
            }
            if (hasBitField)
            {
                structureDefinition.StructureType = StructureType.StructureWithOptionalFields;
            }

            byte switchFieldBitPosition = 0;
            var dataTypeFieldPosition = 0;
            var switchFieldBits = new Dictionary<string, byte>();
            // convert fields
            foreach (var field in structuredType.Field)
            {
                // consume optional bits
                if (IsXmlBitType(field.TypeName))
                {
                    var count = structureDefinition.Fields.Count;
                    if (count == 0 && switchFieldBitPosition < 32)
                    {
                        structureDefinition.StructureType = StructureType.StructureWithOptionalFields;
                        var fieldLength = (byte)((field.Length == 0) ? 1u : field.Length);
                        switchFieldBits[field.Name] = switchFieldBitPosition;
                        switchFieldBitPosition += fieldLength;
                    }
                    else
                    {
                        throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                            "Options for bit selectors must be 32 bit in size, use " +
                            "the Int32 datatype and must be the first element in the structure.");
                    }
                    continue;
                }

                static bool IsXmlBitType(XmlQualifiedName typeName)
                {
                    if (typeName.Namespace is Namespaces.OpcBinarySchema or
                        Namespaces.OpcUa && typeName.Name == "Bit")
                    {
                        return true;
                    }
                    return false;
                }

                if (switchFieldBitPosition is not 0 and not 32)
                {
                    throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                        "Bitwise option selectors must have 32 bits.");
                }
                var fieldDataTypeNodeId = ExpandedNodeId.ToNodeId(
                    field.TypeName == structuredType.QName ?
                    dataTypeNodeId : ToNodeId(field.TypeName), namespaceTable);
                ExpandedNodeId ToNodeId(XmlQualifiedName typeName)
                {
                    if (typeName.Namespace is Namespaces.OpcBinarySchema or
                        Namespaces.OpcUa)
                    {
                        switch (typeName.Name)
                        {
                            case "CharArray": return DataTypeIds.String;
                            case "Variant": return DataTypeIds.BaseDataType;
                            case "ExtensionObject": return DataTypeIds.Structure;
                        }
                    }
                    if (!typeDictionary.TryGetValue(typeName, out var referenceId))
                    {
                        // The type was not found in the namespace
                        return NodeId.Null;
                    }
                    return referenceId.TypeId;
                }
                var dataTypeField = new StructureField()
                {
                    Name = field.Name,
                    Description = null,
                    DataType = fieldDataTypeNodeId,
                    IsOptional = false,
                    MaxStringLength = 0,
                    ArrayDimensions = null,
                    ValueRank = -1
                };
                if (field.LengthField != null)
                {
                    // handle array length
                    var lastField = structureDefinition.Fields[^1];
                    if (lastField.Name != field.LengthField)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                            "The length field must precede the type field of an array.");
                    }
                    lastField.Name = field.Name;
                    lastField.DataType = fieldDataTypeNodeId;
                    lastField.ValueRank = 1;
                }
                else
                {
                    if (isUnionType)
                    {
                        // ignore the switchfield
                        if (field.SwitchField == null)
                        {
                            if (structureDefinition.Fields.Count != 0)
                            {
                                throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                                    "The switch field of a union must be the first" +
                                    " field in the complex type.");
                            }
                            continue;
                        }
                        if (structureDefinition.Fields.Count != dataTypeFieldPosition)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                                "The count of the switch field of the union member " +
                                "is not matching the field position.");
                        }
                        dataTypeFieldPosition++;
                    }
                    else if (field.SwitchField != null)
                    {
                        dataTypeField.IsOptional = true;
                        if (!switchFieldBits.TryGetValue(field.SwitchField, out var value))
                        {
                            throw ServiceResultException.Create(StatusCodes.BadTypeDefinitionInvalid,
                                $"The switch field for {field.SwitchField} does not exist.");
                        }
                    }
                    structureDefinition.Fields.Add(dataTypeField);
                }
            }
            return structureDefinition;
        }

        /// <summary>
        /// Convert a binary schema enumerated type to an enum data type definition
        /// Available before OPC UA V1.04.
        /// </summary>
        /// <param name="enumeratedType"></param>
        private static EnumDefinition ToEnumDefinition(Schema.Binary.EnumeratedType enumeratedType)
        {
            var enumDefinition = new EnumDefinition();
            foreach (var enumValue in enumeratedType.EnumeratedValue)
            {
                var enumTypeField = new EnumField
                {
                    Name = enumValue.Name,
                    Value = enumValue.Value,
                    Description = enumValue.Documentation?.Text?.FirstOrDefault(),
                    DisplayName = enumValue.Name
                };
                enumDefinition.Fields.Add(enumTypeField);
            }
            return enumDefinition;
        }
    }

    /// <summary>
    /// Xml schema type system
    /// </summary>
    private sealed record class XmlSchema : DataTypeSystem
    {
        /// <inheritdoc/>
        public override NodeId TypeSystemId => Objects.XmlSchema_TypeSystem;
        /// <inheritdoc/>
        public override QualifiedName TypeSystemName => BrowseNames.XmlSchema_TypeSystem;
        /// <inheritdoc/>
        public override QualifiedName EncodingName => BrowseNames.DefaultXml;

        /// <inheritdoc/>
        public XmlSchema(INodeCache nodeCache, IServiceMessageContext context,
            ILogger<XmlSchema> logger)
            : base(nodeCache, context, logger)
        {
        }

        /// <inheritdoc/>
        protected override DataTypeDictionary Load(NodeId dictionaryId,
            string targetNamespace, byte[] buffer, Dictionary<string, byte[]> imports)
        {
            using var istrm = new MemoryStream(buffer);
            var xmlSchemaValidator = new Schema.Xml.XmlSchemaValidator(imports);
            xmlSchemaValidator.Validate(istrm);
            return new DataTypeDictionary(dictionaryId, targetNamespace, TypeSystemId,
                TypeSystemName, null, xmlSchemaValidator.TargetSchema);
        }

        /// <inheritdoc/>
        protected override void LoadDictionaryDataTypeDefinitions(Dictionary<XmlQualifiedName,
            (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
            DataTypeDictionary dictionary, NamespaceTable namespaceUris)
        {
            foreach (var xelem in dictionary.Schema!.Elements)
            {
                if (xelem is XmlSchemaType item)
                {
                    var qName = item.QualifiedName ??
                        new XmlQualifiedName(item.Name, dictionary.Namespace);
                    if (typeDictionary.TryGetValue(qName, out var entry))
                    {
                        switch (item)
                        {
                            case XmlSchemaComplexType complexType:
                                _ = ToStructureDefinition(
                                    complexType, entry.EncodingId, typeDictionary,
                                    namespaceUris, entry.TypeId);
                                var structure = new DictionaryDataTypeDefinition(
                                    ToStructureDefinition(complexType, entry.EncodingId,
                                        typeDictionary, namespaceUris, entry.TypeId),
                                    qName, entry.EncodingId);
                                Add(entry.EncodingId, entry.TypeId, structure);
                                break;
                            case XmlSchemaSimpleType simpleType:
                                var enumDefinition = ToEnumDefinition(simpleType);
                                var enumeration = new DictionaryDataTypeDefinition(
                                    enumDefinition, qName, entry.EncodingId);
                                Add(entry.EncodingId, entry.TypeId, enumeration);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert the simple type enumeration facet to an enum definition
        /// </summary>
        /// <param name="simpleType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ServiceResultException"></exception>
        private static DataTypeDefinition ToEnumDefinition(XmlSchemaSimpleType simpleType)
        {
            var enumDefinition = new EnumDefinition();
            if (simpleType.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                foreach (var facet in restriction.Facets)
                {
                    if (facet is not XmlSchemaEnumerationFacet enumFacet)
                    {
                        // Is this allowed?
                        continue;
                    }
                    if (enumFacet.Value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                            "Enumeration facet value is missing.");
                    }
                    var index = enumFacet.Value.LastIndexOf('_');
                    long value = 0;
                    if (index <= 0 ||
                        !long.TryParse(enumFacet.Value.AsSpan(index + 1), out value))
                    {
                        // Log
                    }
                    var enumTypeField = new EnumField
                    {
                        Name = enumFacet.Value,
                        Value = value,
                        Description = enumFacet.Annotation?.Items?.OfType<XmlSchemaDocumentation>()
                            .FirstOrDefault()?.Markup?.FirstOrDefault()?.InnerText,
                        DisplayName = enumFacet.Annotation?.Items?.OfType<XmlSchemaDocumentation>()
                            .FirstOrDefault()?.Markup?.FirstOrDefault()?.InnerText
                    };
                    enumDefinition.Fields.Add(enumTypeField);
                }
            }
            return enumDefinition;
        }

        /// <summary>
        /// Convert the complex type to a structure definition
        /// </summary>
        /// <param name="complexType"></param>
        /// <param name="encodingId"></param>
        /// <param name="typeDictionary"></param>
        /// <param name="namespaceUris"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DataTypeDefinition ToStructureDefinition(XmlSchemaComplexType complexType,
            ExpandedNodeId encodingId, Dictionary<XmlQualifiedName,
                (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
            NamespaceTable namespaceUris, ExpandedNodeId typeId)
        {
            // TODO: Implement

            var structureDefinition = new StructureDefinition
            {
                BaseDataType = null,
                DefaultEncodingId = ExpandedNodeId.ToNodeId(encodingId, namespaceUris),
                Fields = [],
                StructureType = StructureType.Structure
            };

            if (complexType.Particle is not XmlSchemaSequence sequence)
            {
                throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                    "Complex type does not contain a sequence.");
            }
            foreach (var particle in sequence.Items)
            {
                if (particle is XmlSchemaElement element)
                {
                    var field = new StructureField
                    {
                        Name = element.Name,
                        Description = null,
                        DataType = ResolveDataType(element.SchemaTypeName,
                            typeDictionary, namespaceUris),
                        IsOptional = element.MinOccurs == 0,
                        MaxStringLength = 0,
                        ArrayDimensions = null,
                        ValueRank = element.MaxOccurs > 1 ? 1 : -1
                    };
                    structureDefinition.Fields.Add(field);
                }
            }

            return structureDefinition;

            NodeId ResolveDataType(XmlQualifiedName typeName,
                Dictionary<XmlQualifiedName,
                    (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
                NamespaceTable namespaceUris)
            {
                if (typeDictionary.TryGetValue(typeName, out var referenceId))
                {
                    return ExpandedNodeId.ToNodeId(referenceId.TypeId, namespaceUris);
                }
                return NodeId.Null;
            }
        }
    }

    /// <summary>
    /// Data type system
    /// </summary>
    private abstract record class DataTypeSystem
    {
        /// <summary>
        /// The data type system identifier
        /// </summary>
        public abstract NodeId TypeSystemId { get; }

        /// <summary>
        /// The name of the data type system
        /// </summary>
        public abstract QualifiedName TypeSystemName { get; }

        /// <summary>
        /// Encoding name
        /// </summary>
        public abstract QualifiedName EncodingName { get; }

        /// <summary>
        /// Create data type system
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        protected DataTypeSystem(INodeCache nodeCache, IServiceMessageContext context,
            ILogger logger)
        {
            _logger = logger;
            _nodeCache = nodeCache;
            _context = context;
        }

        /// <summary>
        /// Get data type definitions
        /// </summary>
        /// <param name="dataTypeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public ValueTask<DictionaryDataTypeDefinition?> GetDataTypeDefinitionAsync(
            ExpandedNodeId dataTypeId, CancellationToken ct)
        {
            if (_typeDefinitions.TryGetValue(dataTypeId, out var definition))
            {
                return ValueTask.FromResult<DictionaryDataTypeDefinition?>(definition);
            }
            return GetDefinitionFromPropertiesAsync(dataTypeId, ct);
        }

        /// <summary>
        /// Load an entire data type system from the server. A data type system
        /// contains all dictionaries relative to the data type. This is the simplest
        /// and fastest way to get access to the dictionaries and resolving all imports
        /// correctly
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public async ValueTask LoadAsync(CancellationToken ct)
        {
            // get all dictionaries in the data type system.
            var dictionaryReferences = await _nodeCache.GetReferencesAsync(TypeSystemId,
                ReferenceTypeIds.HasComponent, false, false, ct).ConfigureAwait(false);
            if (dictionaryReferences.Count == 0)
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid,
                    "Type system does not contain any valid data dictionaries.");
            }

            // Read all dictionaries
            var dictionaryIds = dictionaryReferences
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
                .ToList();

            var values = await _nodeCache.GetValuesAsync(dictionaryIds,
                ct).ConfigureAwait(false);
            Debug.Assert(dictionaryIds.Count == values.Count);
            var schemas = new Dictionary<NodeId, (string Ns, byte[] Buffer)>();
            for (var index = 0; index < dictionaryIds.Count; index++)
            {
                if (StatusCode.IsBad(values[index].StatusCode) ||
                    values[index].Value is not byte[] buffer)
                {
                    throw new ServiceResultException(values[index].StatusCode);
                }

                var zeroTerminator = Array.IndexOf<byte>(buffer, 0);
                if (zeroTerminator >= 0)
                {
                    Array.Resize(ref buffer, zeroTerminator);
                }

                // Read namespace property of the dictionary
                var references = await _nodeCache.GetReferencesAsync(dictionaryIds[index],
                    ReferenceTypeIds.HasProperty, false, false, ct).ConfigureAwait(false);
                var namespaceNodeId = references
                    .FirstOrDefault(r => r.BrowseName == BrowseNames.NamespaceUri)?.NodeId;
                if (namespaceNodeId == null)
                {
                    continue;
                }
                // read namespace property values
                var nameSpaceValue = await _nodeCache.GetValueAsync(ExpandedNodeId.ToNodeId(
                    namespaceNodeId, _context.NamespaceUris), ct).ConfigureAwait(false);
                if (StatusCode.IsBad(nameSpaceValue.StatusCode) ||
                    nameSpaceValue.Value is not string ns)
                {
                    _logger.LogWarning("Failed to load namespace {Ns}: {Error}",
                        namespaceNodeId, nameSpaceValue.StatusCode);
                    continue;
                }
                if (!schemas.TryAdd(dictionaryIds[index], (ns, buffer)))
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Trying to add duplicate dictionary.");
                }
            }

            // build the namespace/schema import dictionary
            var imports = schemas.Values.ToDictionary(v => v.Ns, v => v.Buffer);
            var typeDictionary = new Dictionary<XmlQualifiedName,
                (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)>();
            var dictionaries = new Dictionary<ExpandedNodeId, DataTypeDictionary>();
            foreach (var (dictionaryId, (ns, buffer)) in schemas)
            {
                try
                {
                    dictionaries[dictionaryId] = Load(dictionaryId, ns, buffer, imports);
                    await LoadTypesAsync(dictionaryId, ns, typeDictionary,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "Dictionary load error for Dictionary {NodeId} : {Message}",
                        dictionaryId, ex.Message);
                }
            }
            foreach (var (dictionaryId, dictionary) in dictionaries)
            {
                LoadDictionaryDataTypeDefinitions(typeDictionary, dictionary,
                    _context.NamespaceUris);
            }
        }

        /// <summary>
        /// Load data type definitions
        /// </summary>
        /// <param name="typeDictionary"></param>
        /// <param name="dictionary"></param>
        /// <param name="namespaceUris"></param>
        protected abstract void LoadDictionaryDataTypeDefinitions(Dictionary<XmlQualifiedName,
            (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> typeDictionary,
            DataTypeDictionary dictionary, NamespaceTable namespaceUris);

        /// <summary>
        /// Load and validate the dictionary schema
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <param name="targetNamespace"></param>
        /// <param name="buffer"></param>
        /// <param name="imports"></param>
        /// <returns></returns>
        protected abstract DataTypeDictionary Load(NodeId dictionaryId, string targetNamespace,
            byte[] buffer, Dictionary<string, byte[]> imports);

        /// <summary>
        /// Add definitions
        /// </summary>
        /// <param name="encodingId"></param>
        /// <param name="typeId"></param>
        /// <param name="definition"></param>
        protected void Add(ExpandedNodeId encodingId, ExpandedNodeId typeId,
            DictionaryDataTypeDefinition definition)
        {
            _typeDefinitions[typeId] = definition;
            _typeDefinitions[encodingId] = definition;
        }

        /// <summary>
        /// Get enum definition from the types properties
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<DictionaryDataTypeDefinition?> GetDefinitionFromPropertiesAsync(
            ExpandedNodeId nodeId, CancellationToken ct = default)
        {
            // find the property reference for the enum type
            var references = await _nodeCache.GetReferencesAsync(
                ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris),
                ReferenceTypeIds.HasProperty, false, false, ct).ConfigureAwait(false);

            // Filter down to the supported properties
            var propertiesToRead = references
                .Where(n => n.BrowseName == BrowseNames.EnumValues ||
                            n.BrowseName == BrowseNames.EnumStrings)
                .Select(n => ExpandedNodeId.ToNodeId(n.NodeId, _context.NamespaceUris))
                .ToList();
            if (references.Count == 0)
            {
                // Give up
                return null;
            }
            // read the properties
            var values = await _nodeCache.GetValuesAsync(propertiesToRead,
                ct).ConfigureAwait(false);
            EnumDefinition? enumDefinition = null;
            foreach (var value in values)
            {
                switch (value?.Value)
                {
                    case ExtensionObject[] enumValueTypes:
                        // use EnumValues
                        var enumValues = new EnumDefinition();
                        foreach (var extensionObject in enumValueTypes)
                        {
                            if (extensionObject.Body is not EnumValueType enumValue)
                            {
                                continue;
                            }
                            var name = enumValue.DisplayName.Text;
                            var enumTypeField = new EnumField
                            {
                                Name = name,
                                Value = enumValue.Value,
                                DisplayName = name
                            };
                            enumValues.Fields.Add(enumTypeField);
                        }
                        if (enumValues.Fields.Count > 0)
                        {
                            // Prefer enum values, otherwise use enum strings
                            return new DictionaryDataTypeDefinition(enumValues,
                                new XmlQualifiedName(nodeId.Identifier.ToString(),
                                _context.NamespaceUris.GetString(nodeId.NamespaceIndex)),
                                ExpandedNodeId.Null);
                        }
                        break;
                    case LocalizedText[] enumFieldNames:
                        // Degrade to EnumStrings
                        enumDefinition ??= new EnumDefinition();
                        for (var i = 0; i < enumFieldNames.Length; i++)
                        {
                            var enumFieldName = enumFieldNames[i];
                            var name = enumFieldName.Text;

                            var enumTypeField = new EnumField
                            {
                                Name = name,
                                Value = i,
                                DisplayName = name
                            };
                            enumDefinition.Fields.Add(enumTypeField);
                        }
                        break;
                }
            }
            if (enumDefinition != null)
            {
                return new DictionaryDataTypeDefinition(enumDefinition, new XmlQualifiedName(
                    nodeId.Identifier.ToString(), _context.NamespaceUris.GetString(
                        nodeId.NamespaceIndex)), ExpandedNodeId.Null);
            }
            return null;
        }

        /// <summary>
        /// Get all the data types that are referenced by the dictionary and load them
        /// into the type system lookup table. The logic follows the references from
        /// the dictionary to the referenced data types and records the encoding ids.
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <param name="targetNamespace"></param>
        /// <param name="results"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask LoadTypesAsync(NodeId dictionaryId, string targetNamespace,
            Dictionary<XmlQualifiedName,
                (ExpandedNodeId TypeId, ExpandedNodeId EncodingId)> results,
            CancellationToken ct)
        {
            var descriptions = await _nodeCache.GetReferencesAsync(dictionaryId,
                ReferenceTypeIds.HasComponent, false, false, ct).ConfigureAwait(false);
            var nodeIdCollection = descriptions
                .Select(node => ExpandedNodeId.ToNodeId(node.NodeId, _context.NamespaceUris))
                .ToList();
            //
            // DataTypeDictionaries are complex Variables which expose their Descriptions
            // as Variables using HasComponent References. A DataTypeDescription provides
            // the information necessary to find the formal description of a DataType
            // within the dictionary. The Value of a description depends on the DataTypeSystem
            // of the DataTypeDictionary. When using OPC Binary dictionaries the Value
            // shall be the name of the TypeDescription. When using XML Schema dictionaries
            // the Value shall be an Xpath expression (see XPATH) which points to an XML
            // element in the schema document.
            //
            var descriptionInfos = await _nodeCache.GetValuesAsync(nodeIdCollection,
                ct).ConfigureAwait(false);
            var encodings = await _nodeCache.GetReferencesAsync(nodeIdCollection,
                [ReferenceTypeIds.HasDescription], true, false, ct).ConfigureAwait(false);
            var encodingNodeIds = descriptions
                .Where(node => node.BrowseName == EncodingName) // Filter only the encodings
                .Select(node => ExpandedNodeId.ToNodeId(node.NodeId, _context.NamespaceUris))
                .ToList();
            if (encodingNodeIds.Count != nodeIdCollection.Count)
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingInvalid,
                    "Failed to resolve descriptions from encodings.");
            }
            Debug.Assert(descriptionInfos.Count == encodingNodeIds.Count);
            var dataTypeNodes = await _nodeCache.GetReferencesAsync(encodingNodeIds,
                [ReferenceTypeIds.HasEncoding], true, false, ct).ConfigureAwait(false);
            if (dataTypeNodes.Count != nodeIdCollection.Count)
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingInvalid,
                    "Failed to resolve data types from encodings.");
            }
            for (var i = 0; i < dataTypeNodes.Count; i++)
            {
                var key = descriptionInfos[i];
                if (!StatusCode.IsGood(key.StatusCode) || key.Value is not string typeName)
                {
                    _logger.LogInformation("Bad data type description {NodeId} : {Status}",
                        nodeIdCollection[i], key.StatusCode);
                    continue;
                }
                var xmlName = new XmlQualifiedName(typeName, targetNamespace);
                results[xmlName] = (dataTypeNodes[i].NodeId, encodings[i].NodeId);
            }
        }

        /// <summary>
        /// A dictionary is a holder to represent the loaded dictionary
        /// </summary>
        /// <param name="DictionaryId"></param>
        /// <param name="Namespace"></param>
        /// <param name="TypeSystemId"></param>
        /// <param name="TypeSystemName"></param>
        /// <param name="TypeDictionary"></param>
        /// <param name="Schema"></param>
        internal sealed record class DataTypeDictionary(NodeId DictionaryId,
            string Namespace, NodeId TypeSystemId, QualifiedName TypeSystemName,
            Schema.Binary.TypeDictionary? TypeDictionary, System.Xml.Schema.XmlSchema? Schema);

        private readonly ConcurrentDictionary<ExpandedNodeId,
            DictionaryDataTypeDefinition> _typeDefinitions = [];
        private readonly IServiceMessageContext _context;
        private readonly INodeCache _nodeCache;
        private readonly ILogger _logger;
    }
    private readonly ConcurrentDictionary<QualifiedName, Lazy<Task<DataTypeSystem>>> _systems = [];
    private readonly ILoggerFactory _loggerFactory;
}
