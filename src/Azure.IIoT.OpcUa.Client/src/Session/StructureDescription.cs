// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

/// <summary>
/// Structure description
/// </summary>
public abstract class StructureDescription : DataTypeDescription
{
    /// <summary>
    /// Empty type info
    /// </summary>
    internal static StructureDescription Null { get; } = new NullDescription();

    /// <summary>
    /// Structure definition
    /// </summary>
    public StructureDefinition StructureDefinition { get; }

    /// <summary>
    /// Allows subtypes in the fields
    /// </summary>
    internal bool FieldsCanHaveSubtypedValues
    {
        get
        {
            switch (StructureDefinition.StructureType)
            {
                case StructureType.UnionWithSubtypedValues:
                case StructureType.StructureWithSubtypedValues:
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Create structure description for the structure definition provided
    /// Returns a structure, union or structure with optional fields which
    /// are all encoded and decoded differently.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="typeId"></param>
    /// <param name="structureDefinition"></param>
    /// <param name="xmlName"></param>
    /// <param name="binaryEncodingId"></param>
    /// <param name="xmlEncodingId"></param>
    /// <param name="jsonEncodingId"></param>
    /// <returns></returns>
    internal static StructureDescription Create(DataTypeSystem cache,
        ExpandedNodeId typeId, StructureDefinition structureDefinition,
        XmlQualifiedName xmlName, ExpandedNodeId binaryEncodingId,
        ExpandedNodeId xmlEncodingId, ExpandedNodeId jsonEncodingId)
    {
        switch (structureDefinition.StructureType)
        {
            case StructureType.Structure:
            case StructureType.StructureWithSubtypedValues:
                return new Structure(cache, typeId, structureDefinition, xmlName,
                    binaryEncodingId, xmlEncodingId, jsonEncodingId);
            case StructureType.StructureWithOptionalFields:
                return new StructureWithOptionalFields(cache, typeId,
                    structureDefinition, xmlName, binaryEncodingId,
                    xmlEncodingId, jsonEncodingId);
            case StructureType.Union:
            case StructureType.UnionWithSubtypedValues:
                return new Union(cache, typeId, structureDefinition, xmlName,
                    binaryEncodingId, xmlEncodingId, jsonEncodingId);
            default:
                return Null;
        }
    }

    /// <summary>
    /// Create structure description
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="typeId"></param>
    /// <param name="structureDefinition"></param>
    /// <param name="xmlName"></param>
    /// <param name="binaryEncodingId"></param>
    /// <param name="xmlEncodingId"></param>
    /// <param name="jsonEncodingId"></param>
    private StructureDescription(DataTypeSystem cache, ExpandedNodeId typeId,
        StructureDefinition structureDefinition, XmlQualifiedName xmlName,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
        ExpandedNodeId jsonEncodingId) : base(typeId, xmlName,
            binaryEncodingId, xmlEncodingId, jsonEncodingId)
    {
        StructureDefinition = structureDefinition;
        _fields = structureDefinition.Fields
            .Select((f, order) => new StructureFieldDescription(
                cache, f, FieldsCanHaveSubtypedValues, order))
            .ToArray();
    }

    /// <summary>
    /// Private constructor for null value
    /// </summary>
    private StructureDescription()
    {
        StructureDefinition = new StructureDefinition();
        _fields = [];
    }

    /// <summary>
    /// Decode structure
    /// </summary>
    /// <param name="decoder"></param>
    /// <returns></returns>
    public abstract object?[]? Decode(IDecoder decoder);

    /// <summary>
    /// Encode structure
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="values"></param>
    public abstract void Encode(IEncoder encoder, object?[]? values);

    internal sealed class NullDescription : StructureDescription
    {
        /// <inheritdoc/>
        public override object?[]? Decode(IDecoder decoder)
        {
            throw ServiceResultException.Create(StatusCodes.BadDataTypeIdUnknown,
                "Data type not found");
        }
        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, object?[]? values)
        {
            throw ServiceResultException.Create(StatusCodes.BadDataTypeIdUnknown,
                "Data type not found");
        }
    }

    /// <summary>
    /// Regular structure
    /// </summary>
    internal sealed class Structure : StructureDescription
    {
        /// <inheritdoc/>
        public Structure(DataTypeSystem cache, ExpandedNodeId typeId,
            StructureDefinition structureDefinition, XmlQualifiedName xmlName,
            ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
            ExpandedNodeId jsonEncodingId) : base(cache, typeId,
                structureDefinition, xmlName, binaryEncodingId,
                xmlEncodingId, jsonEncodingId)
        {
        }

        /// <inheritdoc/>
        public override object?[]? Decode(IDecoder decoder)
        {
            var values = new object?[_fields.Length];
            for (int i = 0; i < _fields.Length; i++)
            {
                values[i] = _fields[i].Decode(decoder);
            }
            return values;
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, object?[]? values)
        {
            for (int i = 0; i < _fields.Length; i++)
            {
                _fields[i].Encode(encoder, values == null ||
                    i >= _fields.Length ? null : values[i]);
            }
        }
    }

    /// <summary>
    /// Regular structure
    /// </summary>
    internal sealed class Union : StructureDescription
    {
        /// <inheritdoc/>
        public Union(DataTypeSystem cache, ExpandedNodeId typeId,
            StructureDefinition structureDefinition, XmlQualifiedName xmlName,
            ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
            ExpandedNodeId jsonEncodingId) : base(cache, typeId,
                structureDefinition, xmlName, binaryEncodingId,
                xmlEncodingId, jsonEncodingId)
        {
        }

        /// <inheritdoc/>
        public override object?[]? Decode(IDecoder decoder)
        {
            var switchField = decoder.ReadUInt32("SwitchField");
            if (switchField >= _fields.Length)
            {
                throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                    "Union selector out of range");
            }
            return new object?[]
            {
                switchField,
                switchField == 0 ? null : _fields[switchField - 1].Decode(decoder)
            };
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, object?[]? values)
        {
            if (values == null || values.Length < 2 ||
                values[0] is not uint switchField)
            {
                throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                    "Union selector or value missing.");
            }
            if (_fields.Length <= switchField)
            {
                throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                    "Union selector out of range");
            }
            string? fieldName = null;
            if (encoder.UseReversibleEncoding)
            {
                encoder.WriteUInt32("SwitchField", switchField);
                fieldName = "Value";
            }
            if (switchField > 0) // Not null
            {
                _fields[switchField - 1].Encode(encoder, values[1], fieldName);
            }
            else if (!encoder.UseReversibleEncoding)
            {
                encoder.WriteString(null, "null"); // TODO: Check if correct!
            }
        }
    }

    /// <summary>
    /// Regular structure
    /// </summary>
    internal sealed class StructureWithOptionalFields : StructureDescription
    {
        /// <inheritdoc/>
        public StructureWithOptionalFields(DataTypeSystem cache, ExpandedNodeId typeId,
            StructureDefinition structureDefinition, XmlQualifiedName xmlName,
            ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId,
            ExpandedNodeId jsonEncodingId) : base(cache, typeId,
                structureDefinition, xmlName, binaryEncodingId,
                xmlEncodingId, jsonEncodingId)
        {
        }

        /// <inheritdoc/>
        public override object?[]? Decode(IDecoder decoder)
        {
            var values = new object?[_fields.Length + 1];
            var encodingMask = decoder.ReadUInt32("EncodingMask");
            values[0] = encodingMask;
            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                if (!field.Field.IsOptional ||
                    (field.OptionalFieldMask & encodingMask) != 0)
                {
                    values[i + 1] = _fields[i].Decode(decoder);
                }
            }
            return values;
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, object?[]? values)
        {
            if (values == null || values.Length != _fields.Length + 1 ||
                values[0] is not uint encodingMask)
            {
                throw ServiceResultException.Create(StatusCodes.BadDataEncodingInvalid,
                    "Encoding mask missing or less values than expected");
            }
            if (encoder.UseReversibleEncoding)
            {
                encoder.WriteUInt32("EncodingMask", encodingMask);
            }
            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                if (!field.Field.IsOptional ||
                    (field.OptionalFieldMask & encodingMask) != 0)
                {
                    _fields[i].Encode(encoder, values[i + 1]);
                }
            }
        }
    }

    private readonly StructureFieldDescription[] _fields;
}
