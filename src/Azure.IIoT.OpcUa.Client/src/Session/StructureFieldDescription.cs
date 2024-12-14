// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System;
using System.Globalization;
using System.Text;
using System.Xml;

/// <summary>
/// Structure field description
/// </summary>
internal sealed class StructureFieldDescription :
    IComparable<StructureFieldDescription>
{
    /// <summary>
    /// Field index inside the structure.
    /// </summary>
    public int FieldIndex { get; }

    /// <summary>
    /// The structure field attributes of the complex type.
    /// </summary>
    public StructureField Field { get; }

    /// <summary>
    /// The structure field can have subtypes and thus the
    /// encoding of the field is in the form of an extension
    /// object rather than inline.
    /// </summary>
    public bool IsAllowSubTypes { get; }

    /// <summary>
    /// Optional mask for the field in the field.
    /// </summary>
    public uint OptionalFieldMask { get; set; }

    /// <summary>
    /// Built in
    /// </summary>
    public BuiltInType BuiltInType => GetBuiltInType(Field.DataType);

    /// <summary>
    /// Create field info
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="fieldIndex"></param>
    /// <param name="field"></param>
    /// <param name="isAllowSubTypes"></param>
    public StructureFieldDescription(DataTypeSystem cache,
        StructureField field, bool isAllowSubTypes, int fieldIndex)
    {
        Field = field;
        IsAllowSubTypes = isAllowSubTypes;
        FieldIndex = fieldIndex;
        OptionalFieldMask = 0;
        _cache = cache;
    }

    /// <inheritdoc/>
    public int CompareTo(StructureFieldDescription? other)
    {
        return FieldIndex.CompareTo(other?.FieldIndex);
    }

    /// <summary>
    /// Get the value of a field.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="fieldNameOverride"></param>
    /// <exception cref="ServiceResultException"></exception>
    public object? Decode(IDecoder decoder, string? fieldNameOverride = null)
    {
        var valueRank = Field.ValueRank;
        fieldNameOverride ??= Field.Name;
        if (valueRank == ValueRanks.Scalar)
        {
            return DecodeScalar(decoder, fieldNameOverride);
        }
        if (valueRank >= ValueRanks.OneDimension)
        {
            return DecodeArray(decoder, fieldNameOverride);
        }
        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
            "Cannot decode a field with unsupported ValueRank {0}.",
            valueRank);
    }

    /// <summary>
    /// Set the value of a field.
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="o"></param>
    /// <param name="fieldNameOverride"></param>
    /// <exception cref="ServiceResultException"></exception>
    public void Encode(IEncoder encoder, object? o, string? fieldNameOverride = null)
    {
        var valueRank = Field.ValueRank;
        fieldNameOverride ??= Field.Name;
        if (valueRank == ValueRanks.Scalar)
        {
            EncodeScalar(encoder, fieldNameOverride, o);
        }
        else if (valueRank >= ValueRanks.OneDimension)
        {
            EncodeArray(encoder, fieldNameOverride, o);
        }
        else
        {
            throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                "Cannot encode a field with unsupported ValueRank {0}.",
                valueRank);
        }
    }

    /// <summary>
    /// Encode a scalar field based on the field type.
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="fieldName"></param>
    /// <param name="o"></param>
    /// <exception cref="ServiceResultException"></exception>
    private void EncodeScalar(IEncoder encoder, string fieldName, object? o)
    {
        var builtInType = BuiltInType;
        var type = GetType(ref builtInType);
        switch (builtInType)
        {
            case BuiltInType.Boolean:
                encoder.WriteBoolean(fieldName, (bool?)o ?? false);
                break;
            case BuiltInType.SByte:
                encoder.WriteSByte(fieldName, (sbyte?)o ?? 0);
                break;
            case BuiltInType.Byte:
                encoder.WriteByte(fieldName, (byte?)o ?? 0);
                break;
            case BuiltInType.Int16:
                encoder.WriteInt16(fieldName, (short?)o ?? 0);
                break;
            case BuiltInType.UInt16:
                encoder.WriteUInt16(fieldName, (ushort?)o ?? 0);
                break;
            case BuiltInType.Int32:
                encoder.WriteInt32(fieldName, (int?)o ?? 0);
                break;
            case BuiltInType.UInt32:
                encoder.WriteUInt32(fieldName, (uint?)o ?? 0);
                break;
            case BuiltInType.Int64:
                encoder.WriteInt64(fieldName, (long?)o ?? 0);
                break;
            case BuiltInType.UInt64:
                encoder.WriteUInt64(fieldName, (ulong?)o ?? 0);
                break;
            case BuiltInType.Float:
                encoder.WriteFloat(fieldName, (float?)o ?? 0f);
                break;
            case BuiltInType.Double:
                encoder.WriteDouble(fieldName, (double?)o ?? 0);
                break;
            case BuiltInType.String:
                encoder.WriteString(fieldName, (string?)o);
                break;
            case BuiltInType.DateTime:
                encoder.WriteDateTime(fieldName, (DateTime?)o ?? DateTime.MinValue);
                break;
            case BuiltInType.Guid:
                encoder.WriteGuid(fieldName, (Uuid?)o ?? Guid.Empty);
                break;
            case BuiltInType.ByteString:
                encoder.WriteByteString(fieldName, (byte[]?)o);
                break;
            case BuiltInType.XmlElement:
                encoder.WriteXmlElement(fieldName, (XmlElement?)o);
                break;
            case BuiltInType.NodeId:
                encoder.WriteNodeId(fieldName, (NodeId?)o);
                break;
            case BuiltInType.ExpandedNodeId:
                encoder.WriteExpandedNodeId(fieldName, (ExpandedNodeId?)o);
                break;
            case BuiltInType.StatusCode:
                encoder.WriteStatusCode(fieldName, (StatusCode?)o ?? StatusCodes.Good);
                break;
            case BuiltInType.DiagnosticInfo:
                encoder.WriteDiagnosticInfo(fieldName, (DiagnosticInfo?)o);
                break;
            case BuiltInType.QualifiedName:
                encoder.WriteQualifiedName(fieldName, (QualifiedName?)o);
                break;
            case BuiltInType.LocalizedText:
                encoder.WriteLocalizedText(fieldName, (LocalizedText?)o);
                break;
            case BuiltInType.DataValue:
                encoder.WriteDataValue(fieldName, (DataValue?)o);
                break;
            case BuiltInType.Variant:
                encoder.WriteVariant(fieldName, (Variant?)o ?? Variant.Null);
                break;
            case BuiltInType.ExtensionObject:
                encoder.WriteExtensionObject(fieldName, (ExtensionObject?)o);
                break;
            case BuiltInType.Enumeration:
                if (type.IsEnum)
                {
                    encoder.WriteEnumerated(fieldName, (Enum?)o);
                    break;
                }
                var e = _cache.GetEnumDescription(Field.DataType);
                if (e != null && o is EnumValue ev)
                {
                    e.Encode(encoder, fieldName, ev);
                    break;
                }
                encoder.WriteInt32(fieldName, (int?)o ?? 0);
                break;
            default:
                if (!typeof(IEncodeable).IsAssignableFrom(type))
                {
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                        "Cannot encode unknown type {0}.", type.Name);
                }
                encoder.WriteEncodeable(fieldName, (IEncodeable?)o, (Type?)type);
                break;
        }
    }

    /// <summary>
    /// Encode an array field based on the base field type.
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="fieldName"></param>
    /// <param name="o"></param>
    private void EncodeArray(IEncoder encoder, string fieldName, object? o)
    {
        var builtInType = BuiltInType;
        var elementType = GetType(ref builtInType);
        encoder.WriteArray(fieldName, o, Field.ValueRank, builtInType);
    }

    /// <summary>
    /// Decode a scalar field based on the field type.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="fieldName"></param>
    /// <exception cref="ServiceResultException"></exception>
    private object? DecodeScalar(IDecoder decoder, string fieldName)
    {
        var builtInType = BuiltInType;
        var type = GetType(ref builtInType);
        switch (builtInType)
        {
            case BuiltInType.Boolean:
                return decoder.ReadBoolean(fieldName);
            case BuiltInType.SByte:
                return decoder.ReadSByte(fieldName);
            case BuiltInType.Byte:
                return decoder.ReadByte(fieldName);
            case BuiltInType.Int16:
                return decoder.ReadInt16(fieldName);
            case BuiltInType.UInt16:
                return decoder.ReadUInt16(fieldName);
            case BuiltInType.Int32:
                return decoder.ReadInt32(fieldName);
            case BuiltInType.UInt32:
                return decoder.ReadUInt32(fieldName);
            case BuiltInType.Int64:
                return decoder.ReadInt64(fieldName);
            case BuiltInType.UInt64:
                return decoder.ReadUInt64(fieldName);
            case BuiltInType.Float:
                return decoder.ReadFloat(fieldName);
            case BuiltInType.Double:
                return decoder.ReadDouble(fieldName);
            case BuiltInType.String:
                return decoder.ReadString(fieldName);
            case BuiltInType.DateTime:
                return decoder.ReadDateTime(fieldName);
            case BuiltInType.Guid:
                return decoder.ReadGuid(fieldName);
            case BuiltInType.ByteString:
                return decoder.ReadByteString(fieldName);
            case BuiltInType.XmlElement:
                return decoder.ReadXmlElement(fieldName);
            case BuiltInType.NodeId:
                return decoder.ReadNodeId(fieldName);
            case BuiltInType.ExpandedNodeId:
                return decoder.ReadExpandedNodeId(fieldName);
            case BuiltInType.StatusCode:
                return decoder.ReadStatusCode(fieldName);
            case BuiltInType.QualifiedName:
                return decoder.ReadQualifiedName(fieldName);
            case BuiltInType.LocalizedText:
                return decoder.ReadLocalizedText(fieldName);
            case BuiltInType.DataValue:
                return decoder.ReadDataValue(fieldName);
            case BuiltInType.Variant:
                return decoder.ReadVariant(fieldName);
            case BuiltInType.DiagnosticInfo:
                return decoder.ReadDiagnosticInfo(fieldName);
            case BuiltInType.ExtensionObject:
                if (typeof(IEncodeable).IsAssignableFrom(type))
                {
                    return decoder.ReadEncodeable(fieldName, type);
                }
                return decoder.ReadExtensionObject(fieldName);
            case BuiltInType.Enumeration:
                if (type.IsEnum)
                {
                    return decoder.ReadEnumerated(fieldName, type);
                }
                return decoder.ReadInt32(fieldName);
            default:
                if (typeof(IEncodeable).IsAssignableFrom(type))
                {
                    return decoder.ReadEncodeable(fieldName, type);
                }
                break;
        }
        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
           "Cannot decode unknown type {0}.", type.Name);
    }

    /// <summary>
    /// Decode an array field based on the base field type.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="fieldName"></param>
    private Array? DecodeArray(IDecoder decoder, string fieldName)
    {
        var builtInType = BuiltInType;
        var elementType = GetType(ref builtInType);

        // Check for enum

        return decoder.ReadArray(fieldName, Field.ValueRank,
            builtInType, elementType);
    }

    /// <summary>
    /// Get type
    /// </summary>
    /// <param name="builtInType"></param>
    /// <returns></returns>
    private Type GetType(ref BuiltInType builtInType)
    {
        var type = _cache.GetSystemType(Field.DataType) ?? typeof(object);
        if (type.IsEnum || type == typeof(EnumValue))
        {
            builtInType = BuiltInType.Enumeration;
        }
        return type;
    }

    /// <summary>
    /// Convert a DataTypeId to a BuiltInType that can be used
    /// for the switch table.
    /// </summary>
    /// <remarks>
    /// As a prerequisite the complex type resolver found a
    /// valid .NET supertype that can be mapped to a BuiltInType.
    /// IEncodeable types are mapped to BuiltInType.Null.
    /// </remarks>
    /// <param name="datatypeId">The data type identifier.</param>
    /// <returns>An <see cref="BuiltInType"/> for
    /// <paramref name="datatypeId"/></returns>
    private static BuiltInType GetBuiltInType(NodeId datatypeId)
    {
        if (datatypeId.IsNullNodeId || datatypeId.NamespaceIndex != 0 ||
            datatypeId.IdType != IdType.Numeric)
        {
            return BuiltInType.Null;
        }

        var builtInType = (BuiltInType)Enum.ToObject(typeof(BuiltInType),
            datatypeId.Identifier);

        if (builtInType is <= BuiltInType.DiagnosticInfo or
            BuiltInType.Enumeration)
        {
            return builtInType;
        }

        // The special case is the internal treatment of Number, Integer and
        // UInteger types which are mapped to Variant, but they have an internal
        // representation in the BuiltInType enum, hence it needs the special
        // handling here to return the BuiltInType.Variant.
        // Other DataTypes which map directly to .NET types in
        // <see cref="TypeInfo.GetSystemType(BuiltInType, int)"/>
        // are handled in <see cref="TypeInfo.GetBuiltInType()"/>
        switch ((uint)builtInType)
        {
            // supertypes of numbers
            case DataTypes.Integer:
            case DataTypes.UInteger:
            case DataTypes.Number:
            case DataTypes.Decimal:
                return BuiltInType.Variant;
            default:
                return Ua.TypeInfo.GetBuiltInType(datatypeId);
        }
    }

    private readonly DataTypeSystem _cache;
}
