// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System;
using System.Diagnostics;
using System.Xml;

/// <summary>
/// Describes an enum
/// </summary>
public sealed class EnumDescription : DataTypeDescription
{
    /// <summary>
    /// Empty type info
    /// </summary>
    internal static EnumDescription Null { get; } = new();

    /// <summary>
    /// Enum definition
    /// </summary>
    public EnumDefinition EnumDefinition { get; }

    /// <summary>
    /// Create description
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="enumDefinition"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    internal EnumDescription(ExpandedNodeId typeId,
        EnumDefinition enumDefinition, XmlQualifiedName xmlName,
        bool isAbstract = false)
        : base(typeId, xmlName, isAbstract)
    {
        EnumDefinition = enumDefinition;
    }

    /// <summary>
    /// Create null description
    /// </summary>
    private EnumDescription()
    {
        EnumDefinition = new EnumDefinition();
    }

    /// <summary>
    /// Decode
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public object? Decode(IDecoder decoder, string fieldName)
    {
        Debug.Assert(EnumDefinition.Fields != null);
        if (EnumDefinition.Fields.Count == 0)
        {
            return null;
        }
        EnumField? field = null;
        switch (decoder)
        {
            case JsonDecoder json:
                if (!json.ReadField(fieldName, out var token))
                {
                    break;
                }
                switch (token)
                {
                    case long code:
                        field = EnumDefinition.Fields
                            .Find(f => f.Value == code);
                        break;
                    case string text:
                        var index = text.LastIndexOf('_');
                        if (index > 0 &&
                            long.TryParse(text.AsSpan(index + 1),
                                out var value))
                        {
                            field = EnumDefinition.Fields
                                .Find(f => f.Value == value);
                        }
                        if (field == null)
                        {
                            field = EnumDefinition.Fields
                                .Find(f => f.Name == text);
                        }
                        break;
                }
                break;
            default:
                var v = decoder.ReadInt32(fieldName);
                field = EnumDefinition.Fields
                    .Find(f => f.Value == v);
                break;
        }

        field ??= EnumDefinition.Fields[0];
        return new EnumValue(field.Name, field.Value);
    }

    /// <summary>
    /// Encode
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="fieldName"></param>
    /// <param name="o"></param>
    public void Encode(IEncoder encoder, string fieldName, object? o)
    {
        // Initialize a null value to the first field in the description
        if (o is not EnumValue e)
        {
            Debug.Assert(EnumDefinition.Fields != null);
            e = EnumDefinition.Fields.Count == 0
                ? new EnumValue("0", 0)
                : new EnumValue(
                    EnumDefinition.Fields[0].Name,
                    EnumDefinition.Fields[0].Value);
        }
        if (encoder is JsonEncoder json &&
            json.EncodingToUse != JsonEncodingType.Reversible &&
            json.EncodingToUse != JsonEncodingType.Compact)
        {
            json.WriteString(fieldName, e.Symbol);
            return;
        }
        encoder.WriteInt32(fieldName, (int)e.Value);
    }
}
