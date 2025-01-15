// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Writes objects to a json
    /// </summary>
    public sealed class JsonEncoderEx : IEncoder
    {
        /// <inheritdoc/>
        public EncodingType EncodingType => EncodingType.Json;

        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <inheritdoc/>
        public bool UseReversibleEncoding { get; set; } = true;

        /// <summary>
        /// Encode nodes as uri
        /// </summary>
        public bool UseUriEncoding { get; set; } = true;

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; set; }
            = Publisher.Models.NamespaceFormat.Uri; // backcompat

        /// <summary>
        /// Encode using microsoft variant
        /// </summary>
        public bool UseAdvancedEncoding { get; set; }

        /// <summary>
        /// Ignore null values
        /// </summary>
        public bool IgnoreNullValues { get; set; }

        /// <summary>
        /// Ignore default primitive values
        /// </summary>
        public bool IgnoreDefaultValues { get; set; }

        /// <summary>
        /// State of the writer
        /// </summary>
        public enum JsonEncoding
        {
            /// <summary>
            /// Start writing object (default)
            /// </summary>
            StartObject,

            /// <summary>
            /// Start writing array
            /// </summary>
            Array,

            /// <summary>
            /// Assume object or array already written
            /// </summary>
            Token
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="formatting"></param>
        /// <param name="leaveOpen"></param>
        public JsonEncoderEx(Stream stream, IServiceMessageContext? context = null,
            JsonEncoding encoding = JsonEncoding.StartObject,
            Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None,
            bool leaveOpen = true) :
            this(new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: leaveOpen),
                context, encoding, formatting, leaveOpen)
        {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="formatting"></param>
        /// <param name="leaveOpen"></param>
        public JsonEncoderEx(TextWriter writer, IServiceMessageContext? context = null,
            JsonEncoding encoding = JsonEncoding.StartObject,
            Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None,
            bool leaveOpen = true) :
            this(new JsonTextWriter(writer)
            {
                AutoCompleteOnClose = true,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatFormatHandling = FloatFormatHandling.String,
                Formatting = formatting,
                CloseOutput = !leaveOpen
            }, context, encoding, true)
        {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="ownedWriter"></param>
        public JsonEncoderEx(JsonWriter writer, IServiceMessageContext? context = null,
            JsonEncoding encoding = JsonEncoding.StartObject, bool ownedWriter = false)
        {
            _namespaces = new Stack<string>();
            Context = context ?? new ServiceMessageContext();
            _ownedWriter = ownedWriter;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _encoding = encoding;
            switch (encoding)
            {
                case JsonEncoding.StartObject:
                    _writer.WriteStartObject();
                    break;
                case JsonEncoding.Array:
                    _writer.WriteStartArray();
                    break;
            }
        }

        /// <inheritdoc/>
        public int Close()
        {
            if (_writer != null)
            {
                switch (_encoding)
                {
                    case JsonEncoding.StartObject:
                        _writer.WriteEndObject();
                        break;
                    case JsonEncoding.Array:
                        _writer.WriteEndArray();
                        break;
                }

                _writer.Flush();
                if (_ownedWriter)
                {
                    _writer.Close();
                }
                _writer = null;
            }
            return -1; // Not supported
        }

        /// <inheritdoc/>
        public string? CloseAndReturnText()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();
        }

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable namespaceUris,
            StringTable serverUris)
        {
            _namespaceMappings = null;

            if (namespaceUris != null && Context.NamespaceUris != null)
            {
                _namespaceMappings = namespaceUris.CreateMapping(
                    Context.NamespaceUris, false);
            }
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri)
        {
            _namespaces.Push(namespaceUri);
        }

        /// <inheritdoc/>
        public void PopNamespace()
        {
            _namespaces.Pop();
        }

        /// <inheritdoc/>
        public void WriteSByte(string? fieldName, sbyte value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByte(string? fieldName, byte value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt16(string? fieldName, short value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteUInt16(string? fieldName, ushort value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt32(string? fieldName, int value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteUInt32(string? fieldName, uint value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt64(string? fieldName, long value)
        {
            if (PreWriteValue(fieldName, value))
            {
                if (UseAdvancedEncoding)
                {
                    _writer?.WriteValue(value);
                }
                else
                {
                    _writer?.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        /// <inheritdoc/>
        public void WriteUInt64(string? fieldName, ulong value)
        {
            if (PreWriteValue(fieldName, value))
            {
                if (UseAdvancedEncoding)
                {
                    _writer?.WriteValue(value);
                }
                else
                {
                    _writer?.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        /// <inheritdoc/>
        public void WriteBoolean(string? fieldName, bool value)
        {
            if (PreWriteValue(fieldName, value))
            {
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteString(string? fieldName, string? value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteFloat(string? fieldName, float value)
        {
            if (!string.IsNullOrEmpty(fieldName))
            {
                if (IgnoreDefaultValues && Math.Abs(value) < float.Epsilon)
                {
                    return;
                }
                _writer?.WritePropertyName(fieldName);
            }
            if (float.IsPositiveInfinity(value))
            {
                _writer?.WriteValue("Infinity");
            }
            else if (float.IsNegativeInfinity(value))
            {
                _writer?.WriteValue("-Infinity");
            }
            else if (float.IsNaN(value))
            {
                _writer?.WriteValue("NaN");
            }
            else
            {
                _writer?.WriteRawValue(value.ToString("G9", CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteDouble(string? fieldName, double value)
        {
            if (!string.IsNullOrEmpty(fieldName))
            {
                if (IgnoreDefaultValues && Math.Abs(value) < double.Epsilon)
                {
                    return;
                }
                _writer?.WritePropertyName(fieldName);
            }
            if (double.IsPositiveInfinity(value))
            {
                _writer?.WriteValue("Infinity");
            }
            else if (double.IsNegativeInfinity(value))
            {
                _writer?.WriteValue("-Infinity");
            }
            else if (double.IsNaN(value))
            {
                _writer?.WriteValue("NaN");
            }
            else
            {
                _writer?.WriteRawValue(value.ToString("G17", CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc/>
        public void WriteDateTime(string? fieldName, DateTime value)
        {
            if (value == default)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                _writer?.WriteValue(value.ToOpcUaJsonEncodedTime());
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Uuid value)
        {
            if (value == default)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Guid value)
        {
            if (value == default)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByteString(string? fieldName, byte[]? value)
        {
            if (value == null || value.Length == 0)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                // check the length.
                if (Context.MaxByteStringLength > 0 &&
                    Context.MaxByteStringLength < value.Length)
                {
                    throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                        $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded.");
                }
                _writer?.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByteString(string? fieldName, ReadOnlySpan<byte> value)
        {
            WriteByteString(fieldName, value.ToArray());
        }

        /// <inheritdoc/>
        public void WriteByteString(string? fieldName, byte[] value, int index, int count)
        {
            WriteByteString(fieldName, value.AsSpan().Slice(index, count));
        }

        /// <inheritdoc/>
        public void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldName))
                {
                    _writer?.WritePropertyName(fieldName);
                }
                _writer?.WriteValue(Encoding.UTF8.GetBytes(value.OuterXml));
            }
        }

        /// <inheritdoc/>
        public void WriteNodeId(string? fieldName, NodeId? value)
        {
            if (value == null || NodeId.IsNull(value))
            {
                WriteNull(fieldName);
            }
            else if (UseAdvancedEncoding)
            {
                if (UseUriEncoding || UseReversibleEncoding)
                {
                    WriteString(fieldName, value.AsString(Context,
                        NamespaceFormat));
                }
                else
                {
                    WriteString(fieldName, value.ToString());
                }
            }
            else
            {
                PushObject(fieldName);
                if (value.IdType != IdType.Numeric)
                {
                    WriteByte("IdType", (byte)value.IdType);
                }
                switch (value.IdType)
                {
                    case IdType.Numeric:
                        WriteUInt32("Id", (uint)value.Identifier);
                        break;
                    case IdType.String:
                        WriteString("Id", (string)value.Identifier);
                        break;
                    case IdType.Guid:
                        WriteGuid("Id", (Guid)value.Identifier);
                        break;
                    case IdType.Opaque:
                        WriteByteString("Id", (byte[])value.Identifier);
                        break;
                }
                switch (value.NamespaceIndex)
                {
                    case 0:
                        // default namespace - nothing to do
                        break;
                    case 1:
                        // always as integer
                        WriteUInt16("Namespace", value.NamespaceIndex);
                        break;
                    default:
                        var namespaceUri = Context.NamespaceUris.GetString(value.NamespaceIndex);
                        if (namespaceUri != null && !UseReversibleEncoding)
                        {
                            WriteString("Namespace", namespaceUri);
                        }
                        else
                        {
                            WriteUInt16("Namespace", value.NamespaceIndex);
                        }
                        break;
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string? fieldName, ExpandedNodeId? value)
        {
            if (value == null || NodeId.IsNull(value))
            {
                WriteNull(fieldName);
            }
            else if (UseAdvancedEncoding)
            {
                if (UseUriEncoding || UseReversibleEncoding)
                {
                    WriteString(fieldName, value.AsString(Context,
                        NamespaceFormat));
                }
                else
                {
                    WriteString(fieldName, value.ToString());
                }
            }
            else
            {
                PushObject(fieldName);
                if (value.IdType != IdType.Numeric)
                {
                    WriteByte("IdType", (byte)value.IdType);
                }
                switch (value.IdType)
                {
                    case IdType.Numeric:
                        WriteUInt32("Id", (uint)value.Identifier);
                        break;
                    case IdType.String:
                        WriteString("Id", (string)value.Identifier);
                        break;
                    case IdType.Guid:
                        WriteGuid("Id", (Guid)value.Identifier);
                        break;
                    case IdType.Opaque:
                        WriteByteString("Id", (byte[])value.Identifier);
                        break;
                }
                switch (value.NamespaceIndex)
                {
                    case 0:
                        // default namespace - nothing to do
                        break;
                    case 1:
                        // namespace 1 always as integer
                        WriteUInt16("Namespace", value.NamespaceIndex);
                        break;
                    default:
                        var namespaceUri = UseReversibleEncoding ?
                            null : Context.NamespaceUris.GetString(value.NamespaceIndex);
                        if (namespaceUri != null)
                        {
                            WriteString("Namespace", namespaceUri);
                        }
                        else
                        {
                            WriteUInt16("Namespace", value.NamespaceIndex);
                        }
                        break;
                }
                if (value.ServerIndex != 0)
                {
                    var serverUri = Context.ServerUris.GetString(value.ServerIndex);
                    if (serverUri != null)
                    {
                        WriteString("ServerUri", serverUri);
                    }
                    else
                    {
                        WriteUInt32("ServerUri", value.ServerIndex);
                    }
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string? fieldName, StatusCode value)
        {
            if (value == StatusCodes.Good)
            {
                WriteNull(fieldName);
            }
            else
            {
                var symbol = string.Empty;
                if (!UseReversibleEncoding || UseAdvancedEncoding)
                {
                    symbol = value.AsString();
                }
                if (!UseReversibleEncoding || !string.IsNullOrEmpty(symbol))
                {
                    PushObject(fieldName);
                    WriteString("Symbol", symbol);
                    WriteUInt32("Code", value.Code);
                    PopObject();
                }
                else
                {
                    WriteUInt32(fieldName, value.Code);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfo(string? fieldName, DiagnosticInfo value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                PushObject(fieldName);
                if (value.SymbolicId >= 0)
                {
                    WriteInt32("SymbolicId", value.SymbolicId);
                }
                if (value.NamespaceUri >= 0)
                {
                    WriteInt32("NamespaceUri", value.NamespaceUri);
                }
                if (value.Locale >= 0)
                {
                    WriteInt32("Locale", value.Locale);
                }
                if (value.LocalizedText >= 0)
                {
                    WriteInt32("LocalizedText", value.LocalizedText);
                }
                if (value.AdditionalInfo != null)
                {
                    WriteString("AdditionalInfo", value.AdditionalInfo);
                }
                if (value.InnerStatusCode != StatusCodes.Good)
                {
                    WriteStatusCode("InnerStatusCode", value.InnerStatusCode);
                }
                if (value.InnerDiagnosticInfo != null)
                {
                    WriteDiagnosticInfo("InnerDiagnosticInfo", value.InnerDiagnosticInfo);
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteQualifiedName(string? fieldName, QualifiedName? value)
        {
            if (value == null || QualifiedName.IsNull(value))
            {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding)
            {
                if (UseUriEncoding && UseAdvancedEncoding)
                {
                    WriteString(fieldName, value.AsString(Context,
                        NamespaceFormat));
                }
                else
                {
                    // Back compat to json encoding
                    PushObject(fieldName);
                    WriteString("Name", value.Name);
                    if (value.NamespaceIndex > 0)
                    {
                        WriteUInt16("Uri", value.NamespaceIndex);
                    }
                    PopObject();
                }
            }
            else
            {
                PushObject(fieldName);
                WriteString("Name", value.Name);
                WriteNamespaceIndex(value.NamespaceIndex);
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteLocalizedText(string? fieldName, LocalizedText? value)
        {
            if (value == null || LocalizedText.IsNullOrEmpty(value))
            {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding)
            {
                PushObject(fieldName);
                WriteString("Text", value.Text);
                if (!string.IsNullOrEmpty(value.Locale))
                {
                    WriteString("Locale", value.Locale);
                }
                PopObject();
            }
            else
            {
                WriteString(fieldName, value.Text);
            }
        }

        /// <inheritdoc/>
        public void WriteVariant(string? fieldName, Variant value)
        {
            var variant = value;
            if (UseAdvancedEncoding &&
                value.Value is Variant[] vararray &&
                value.TypeInfo.ValueRank == 1 &&
                vararray.Length > 0)
            {
                var type = vararray[0].TypeInfo?.BuiltInType;
                var rank = vararray[0].TypeInfo?.ValueRank;

                if (vararray.All(v => v.TypeInfo?.BuiltInType == type))
                {
                    try
                    {
                        // Demote and encode as simple array
                        variant = new TypeInfo(type ?? BuiltInType.Null, 1)
                            .CreateVariant(vararray
                                .Select(v => v.Value)
                                .ToArray());
                    }
                    catch
                    {
                        // Fails when different ranks are in use in array
                        variant = value;
                    }
                }
            }

            var valueRank = variant.TypeInfo?.ValueRank ?? -1;
            var builtInType = variant.TypeInfo?.BuiltInType ?? BuiltInType.Null;

            if (UseReversibleEncoding)
            {
                PushObject(fieldName);
                WriteBuiltInType("Type", builtInType);
                fieldName = "Body";
            }

            if (!string.IsNullOrEmpty(fieldName))
            {
                _writer?.WritePropertyName(fieldName);
            }

            WriteVariantContents(variant.Value, valueRank, builtInType);

            if (UseReversibleEncoding)
            {
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteDataValue(string? fieldName, DataValue? value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                if (value.StatusCode != StatusCodes.Good ||
                    value.SourceTimestamp != DateTime.MinValue ||
                    value.ServerTimestamp != DateTime.MinValue ||
                    value.SourcePicoseconds != 0 ||
                    value.ServerPicoseconds != 0)
                {
                    PushObject(fieldName);
                    if (value.WrappedValue.TypeInfo != null &&
                        value.WrappedValue.TypeInfo.BuiltInType != BuiltInType.Null)
                    {
                        WriteVariant("Value", value.WrappedValue);
                    }

                    if (value.StatusCode != StatusCodes.Good)
                    {
                        WriteStatusCode("StatusCode", value.StatusCode);
                    }
                    if (value.SourceTimestamp != DateTime.MinValue)
                    {
                        WriteDateTime("SourceTimestamp", value.SourceTimestamp);
                        if (value.SourcePicoseconds != 0)
                        {
                            WriteUInt16("SourcePicoseconds", value.SourcePicoseconds);
                        }
                    }
                    if (value.ServerTimestamp != DateTime.MinValue)
                    {
                        WriteDateTime("ServerTimestamp", value.ServerTimestamp);
                        if (value.ServerPicoseconds != 0)
                        {
                            WriteUInt16("ServerPicoseconds", value.ServerPicoseconds);
                        }
                    }
                    PopObject();
                }
                else
                {
                    // raw value
                    if (value.WrappedValue.TypeInfo != null &&
                        value.WrappedValue.TypeInfo.BuiltInType != BuiltInType.Null)
                    {
                        WriteVariant(fieldName, value.WrappedValue);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string? fieldName, ExtensionObject? value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
                return;
            }

            var body = value.Body;

            if (UseReversibleEncoding)
            {
                PushObject(fieldName);
                var typeId = value.TypeId;
                if (body is IJsonEncodeable withType)
                {
                    typeId = withType.JsonEncodingId;
                }
                if (!NodeId.IsNull(typeId))
                {
                    WriteExpandedNodeId("TypeId", typeId);
                }
                else if (!UseAdvancedEncoding)
                {
                    throw new EncodingException(
                       "Cannot encode extension object without type id.");
                }
                if (UseAdvancedEncoding)
                {
                    // Backcompat
                    if (body is XmlElement)
                    {
                        WriteString("Encoding", nameof(ExtensionObjectEncoding.Xml));
                    }
                    else if (body is not byte[] and not null)
                    {
                        WriteString("Encoding", nameof(ExtensionObjectEncoding.Json));
                    }
                }
                else
                {
                    // https://reference.opcfoundation.org/Core/Part6/v105/docs/5.4.2.16
                    WriteInt32("Encoding", body switch
                    {
                        byte[] => 1,        // Byte string
                        XmlElement => 2,    // Xml
                        _ => 0,             // Structure - omitted in default encoding.
                    });
                }
                fieldName = "Body";
            }
            switch (body)
            {
                case EncodeableJToken jt:
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        _writer?.WritePropertyName(fieldName);
                    }
                    _writer?.WriteRaw(jt.JToken.ToString());
                    break;
                case IEncodeable encodeable:
                    PushObject(fieldName);
                    encodeable.Encode(this);
                    PopObject();
                    break;
                case XmlElement xml:
                    WriteXmlElement(fieldName, xml);
                    break;
                case byte[] buffer:
                    WriteByteString(fieldName, buffer);
                    break;
                case null:
                    WriteNull(fieldName);
                    break;
                default:
                    throw new EncodingException("Unexpected value encountered while " +
                        $"encoding body:{body}");
            }
            if (UseReversibleEncoding)
            {
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void EncodeMessage(IEncodeable message)
        {
            message.Encode(this);
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string? fieldName, IEncodeable? value,
            Type systemType)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                PushObject(fieldName);
                value.Encode(this);
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string? fieldName, Enum value)
        {
            if (value == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                var numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (UseReversibleEncoding)
                {
                    if (PreWriteValue(fieldName, numeric))
                    {
                        _writer?.WriteValue(numeric);
                    }
                }
                else
                {
                    WriteString(fieldName, $"{value}_{numeric}");
                }
            }
        }

        /// <inheritdoc/>
        public void WriteBooleanArray(string? fieldName, IList<bool>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteSByteArray(string? fieldName, IList<sbyte>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteByteArray(string? fieldName, IList<byte>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteInt16Array(string? fieldName, IList<short>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteUInt16Array(string? fieldName, IList<ushort>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteInt32Array(string? fieldName, IList<int>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteUInt32Array(string? fieldName, IList<uint>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteInt64Array(string? fieldName, IList<long>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteUInt64Array(string? fieldName, IList<ulong>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteFloatArray(string? fieldName, IList<float>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteDoubleArray(string? fieldName, IList<double>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteStringArray(string? fieldName, IList<string?>? values)
        {
            WriteArray(fieldName, values, v => _writer?.WriteValue(v));
        }

        /// <inheritdoc/>
        public void WriteStringDictionary(string? property,
            IEnumerable<(string, string?)> values)
        {
            WriteDictionary(property, values, WriteString);
        }

        /// <inheritdoc/>
        public void WriteDateTimeArray(string? fieldName, IList<DateTime>? values)
        {
            WriteArray(fieldName, values, v => WriteDateTime(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName, IList<Uuid>? values)
        {
            WriteArray(fieldName, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName, IList<Guid>? values)
        {
            WriteArray(fieldName, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteByteStringArray(string? fieldName, IList<byte[]?>? values)
        {
            WriteArray(fieldName, values, v => WriteByteString(null, v));
        }

        /// <inheritdoc/>
        public void WriteXmlElementArray(string? fieldName, IList<XmlElement?>? values)
        {
            WriteArray(fieldName, values, v => WriteXmlElement(null, v));
        }

        /// <inheritdoc/>
        public void WriteNodeIdArray(string? fieldName, IList<NodeId?>? values)
        {
            WriteArray(fieldName, values, v => WriteNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string? fieldName, IList<ExpandedNodeId?>? values)
        {
            WriteArray(fieldName, values, v => WriteExpandedNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string? fieldName, IList<StatusCode>? values)
        {
            WriteArray(fieldName, values, v => WriteStatusCode(null, v));
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string? fieldName, IList<DiagnosticInfo>? values)
        {
            WriteArray(fieldName, values, v => WriteDiagnosticInfo(null, v));
        }

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string? fieldName, IList<QualifiedName?>? values)
        {
            WriteArray(fieldName, values, v => WriteQualifiedName(null, v));
        }

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string? fieldName, IList<LocalizedText?>? values)
        {
            WriteArray(fieldName, values, v => WriteLocalizedText(null, v));
        }

        /// <inheritdoc/>
        public void WriteVariantArray(string? fieldName, IList<Variant>? values)
        {
            WriteArray(fieldName, values, v => WriteVariant(null, v));
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string? fieldName, IList<DataValue?>? values)
        {
            WriteArray(fieldName, values, v => WriteDataValue(null, v));
        }

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string? fieldName, IList<ExtensionObject?>? values)
        {
            WriteArray(fieldName, values, v => WriteExtensionObject(null, v));
        }

        /// <inheritdoc/>
        public void WriteEncodeableArray(string? fieldName, IList<IEncodeable>? values,
            Type systemType)
        {
            WriteArray(fieldName, values, v => WriteEncodeable(null, v, systemType));
        }

        /// <inheritdoc/>
        public void WriteDataSet(string? property, DataSet? dataSet)
        {
            if (dataSet == null)
            {
                WriteNull(property);
                return;
            }
            var useUriEncoding = UseUriEncoding;
            var useReversibleEncoding = UseReversibleEncoding;
            try
            {
                var fieldContentMask = dataSet.DataSetFieldContentMask;
                var writeSingleValue = (dataSet.DataSetFields.Count == 1) &&
                   fieldContentMask.HasFlag(DataSetFieldContentFlags.SingleFieldDegradeToValue);
                if (fieldContentMask.HasFlag(DataSetFieldContentFlags.RawData))
                {
                    //
                    // If the DataSetFieldContentMask results in a RawData representation,
                    // the field value is a Variant encoded using the non-reversible OPC UA
                    // JSON Data Encoding defined in OPC 10000-6
                    //
                    UseUriEncoding = true;
                    UseReversibleEncoding = false;
                    Write(property, dataSet.DataSetFields,
                        (k, v) => WriteVariant(k, v?.WrappedValue ?? default), writeSingleValue);
                }
                else if (fieldContentMask == 0)
                {
                    //
                    // If the DataSetFieldContentMask results in a Variant representation,
                    // the field value is encoded as a Variant encoded using the reversible
                    // OPC UA JSON Data Encoding defined in OPC 10000-6.
                    //
                    UseUriEncoding = false;
                    UseReversibleEncoding = true;
                    Write(property, dataSet.DataSetFields,
                        (k, v) => WriteVariant(k, v?.WrappedValue ?? default), writeSingleValue);
                }
                else
                {
                    //
                    // If the DataSetFieldContentMask results in a DataValue representation,
                    // the field value is a DataValue encoded using the non-reversible OPC UA
                    // JSON Data Encoding or reversible depending on encoder configuration.
                    //
                    Write(property, dataSet.DataSetFields, (k, value) =>
                    {
                        PushObject(k);
                        try
                        {
                            WriteVariant("Value", value?.WrappedValue ?? default);
                            if (value != null)
                            {
                                if (fieldContentMask.HasFlag(DataSetFieldContentFlags.StatusCode))
                                {
                                    WriteStatusCode("StatusCode", value.StatusCode);
                                }
                                if (fieldContentMask.HasFlag(DataSetFieldContentFlags.SourceTimestamp))
                                {
                                    WriteDateTime("SourceTimestamp", value.SourceTimestamp);
                                    if (fieldContentMask.HasFlag(DataSetFieldContentFlags.SourcePicoSeconds))
                                    {
                                        WriteUInt16("SourcePicoseconds", value.SourcePicoseconds);
                                    }
                                }
                                if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerTimestamp))
                                {
                                    WriteDateTime("ServerTimestamp", value.ServerTimestamp);
                                    if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerPicoSeconds))
                                    {
                                        WriteUInt16("ServerPicoseconds", value.ServerPicoseconds);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            PopObject();
                        }
                    }, writeSingleValue);
                }

                void Write<T>(string? property, IEnumerable<(string, T)> values, Action<string?, T> writer,
                    bool writeSingleValue)
                {
                    if (writeSingleValue)
                    {
                        writer(property, values.Single().Item2);
                    }
                    else
                    {
                        WriteDictionary(property, values, writer);
                    }
                }
            }
            finally
            {
                UseUriEncoding = useUriEncoding;
                UseReversibleEncoding = useReversibleEncoding;
            }
        }

        /// <inheritdoc/>
        public void WriteObjectArray(string? property, IList<object>? values)
        {
            PushArray(property, values?.Count ?? 0);
            if (values != null)
            {
                foreach (var value in values)
                {
                    WriteVariant("Variant", new Variant(value));
                }
            }
            PopArray();
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string? fieldName, Array? values, Type systemType)
        {
            if (values == null)
            {
                WriteNull(fieldName);
            }
            else
            {
                PushArray(fieldName, values.Length);
                // encode each element in the array.
                if (systemType.IsEnum)
                {
                    foreach (Enum value in values)
                    {
                        WriteEnumerated(null, value);
                    }
                }
                else if (systemType == typeof(int))
                {
                    foreach (int value in values)
                    {
                        WriteInt32(null, value);
                    }
                }
                else
                {
                    throw new ArgumentException("Not an enum type", nameof(systemType));
                }
                PopArray();
            }
        }

        /// <summary>
        /// Writes the contents of an Variant to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueRank"></param>
        /// <param name="builtInType"></param>
        /// <exception cref="EncodingException"></exception>
        private void WriteVariantContents(object value, int valueRank,
            BuiltInType builtInType)
        {
            // Handle special value ranks
            if (valueRank is <= (-2) or 0)
            {
                if (valueRank < -3)
                {
                    throw new EncodingException(
                        $"Bad variant: Value rank '{valueRank}' is invalid.");
                }
                // Cannot deduce rank - write null
                if (value == null)
                {
                    WriteNull(null);
                    return;
                }
                // Handle one or more (0), Any (-2) or scalar or one dimension (-3)
                if (value.GetType().IsArray)
                {
                    var rank = value.GetType().GetArrayRank();
                    if (valueRank == -3 && rank != 1)
                    {
                        throw new EncodingException(
                            "Bad variant: Scalar or one dimension with matrix value.");
                    }
                    // Write as array or matrix
                    valueRank = rank;
                }
                else
                {
                    if (valueRank == 0)
                    {
                        throw new EncodingException(
                            "Bad variant: One or more dimension rank with scalar value.");
                    }
                    // Force write as scalar
                    valueRank = -1;
                }
            }

            // write scalar.
            if (valueRank == -1)
            {
                switch (builtInType)
                {
                    case BuiltInType.Null:
                        WriteNull(null);
                        return;
                    case BuiltInType.Boolean:
                        WriteBoolean(null, ToTypedScalar<bool>(value));
                        return;
                    case BuiltInType.SByte:
                        WriteSByte(null, ToTypedScalar<sbyte>(value));
                        return;
                    case BuiltInType.Byte:
                        WriteByte(null, ToTypedScalar<byte>(value));
                        return;
                    case BuiltInType.Int16:
                        WriteInt16(null, ToTypedScalar<short>(value));
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16(null, ToTypedScalar<ushort>(value));
                        return;
                    case BuiltInType.Int32:
                        WriteInt32(null, ToTypedScalar<int>(value));
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32(null, ToTypedScalar<uint>(value));
                        return;
                    case BuiltInType.Int64:
                        WriteInt64(null, ToTypedScalar<long>(value));
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64(null, ToTypedScalar<ulong>(value));
                        return;
                    case BuiltInType.Float:
                        WriteFloat(null, ToTypedScalar<float>(value));
                        return;
                    case BuiltInType.Double:
                        WriteDouble(null, ToTypedScalar<double>(value));
                        return;
                    case BuiltInType.String:
                        WriteString(null, ToTypedScalar<string>(value, null));
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTime(null, ToTypedScalar<DateTime>(value));
                        return;
                    case BuiltInType.Guid:
                        WriteGuid(null, ToTypedScalar<Uuid>(value));
                        return;
                    case BuiltInType.ByteString:
                        WriteByteString(null, ToTypedScalar(value, Array.Empty<byte>()));
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElement(null, ToTypedScalar<XmlElement>(value, null));
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeId(null, ToTypedScalar<NodeId>(value, null));
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeId(null, ToTypedScalar<ExpandedNodeId>(value, null));
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCode(null, ToTypedScalar<StatusCode>(value));
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedName(null, ToTypedScalar<QualifiedName>(value, null));
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedText(null, ToTypedScalar<LocalizedText>(value, null));
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObject(null, ToTypedScalar<ExtensionObject>(value, null));
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValue(null, ToTypedScalar<DataValue>(value, null));
                        return;
                    case BuiltInType.Enumeration:
                        WriteInt32(null, ToTypedScalar<int>(value));
                        return;
                    case BuiltInType.Number:
                    case BuiltInType.Integer:
                    case BuiltInType.UInteger:
                    case BuiltInType.Variant:
                        throw new EncodingException(
                            "Bad variant: Unexpected type encountered while encoding " +
                            value.GetType());
                }
            }

            // write array.
            if (valueRank == 1)
            {
                switch (builtInType)
                {
                    case BuiltInType.Null:
                        WriteNull(null);
                        return;
                    case BuiltInType.Boolean:
                        WriteBooleanArray(null, ToTypedArray<bool>(value));
                        return;
                    case BuiltInType.SByte:
                        WriteSByteArray(null, ToTypedArray<sbyte>(value));
                        return;
                    case BuiltInType.Byte:
                        WriteByteArray(null, ToTypedArray<byte>(value));
                        return;
                    case BuiltInType.Int16:
                        WriteInt16Array(null, ToTypedArray<short>(value));
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16Array(null, ToTypedArray<ushort>(value));
                        return;
                    case BuiltInType.Int32:
                        WriteInt32Array(null, ToTypedArray<int>(value));
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32Array(null, ToTypedArray<uint>(value));
                        return;
                    case BuiltInType.Int64:
                        WriteInt64Array(null, ToTypedArray<long>(value));
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64Array(null, ToTypedArray<ulong>(value));
                        return;
                    case BuiltInType.Float:
                        WriteFloatArray(null, ToTypedArray<float>(value));
                        return;
                    case BuiltInType.Double:
                        WriteDoubleArray(null, ToTypedArray<double>(value));
                        return;
                    case BuiltInType.String:
                        WriteStringArray(null, ToTypedArray<string>(value, null));
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTimeArray(null, ToTypedArray<DateTime>(value));
                        return;
                    case BuiltInType.Guid:
                        WriteGuidArray(null, ToTypedArray<Uuid>(value));
                        return;
                    case BuiltInType.ByteString:
                        WriteByteStringArray(null, ToTypedArray<byte[]>(value, null));
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElementArray(null, ToTypedArray<XmlElement>(value, null));
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeIdArray(null, ToTypedArray<NodeId>(value, null));
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeIdArray(null, ToTypedArray<ExpandedNodeId>(value, null));
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCodeArray(null, ToTypedArray<StatusCode>(value));
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedNameArray(null, ToTypedArray<QualifiedName>(value, null));
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedTextArray(null, ToTypedArray<LocalizedText>(value, null));
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObjectArray(null, ToTypedArray<ExtensionObject>(value, null));
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValueArray(null, ToTypedArray<DataValue>(value, null));
                        return;
                    case BuiltInType.Enumeration:
                        if (value is not Enum[] enums)
                        {
                            throw new EncodingException(
                                "Bad enum: Unexpected type encountered while encoding " +
                                $"enumeration type: {value.GetType()}");
                        }
                        var values = new string[enums.Length];
                        for (var index = 0; index < enums.Length; index++)
                        {
                            var text = enums[index].ToString();
                            text += "_";
                            text += ((int)(object)enums[index])
                                .ToString(CultureInfo.InvariantCulture);
                            values[index] = text;
                        }

                        WriteStringArray(null, values);
                        return;
                    case BuiltInType.Number:
                    case BuiltInType.UInteger:
                    case BuiltInType.Integer:
                    case BuiltInType.Variant:
                        if (value is Variant[] variants)
                        {
                            WriteVariantArray(null, variants);
                            return;
                        }
                        if (value is object[] objects)
                        {
                            WriteObjectArray(null, objects);
                            return;
                        }
                        throw new EncodingException(
                            "Bad variant: Unexpected type encountered while encoding an array" +
                            $" of Variants: {value.GetType()}");
                }
            }

            if (valueRank > 1)
            {
                // Write matrix
                if (value == null)
                {
                    WriteNull(null);
                    return;
                }

                // TODO: JSON array encoding only for
                // non reversible encoding, otherwise
                // flatten array and add Dimension.
                // if (!UseReversibleEncoding) {
                if (value is Matrix matrix)
                {
                    var index = 0;
                    WriteMatrix(matrix, 0, ref index, builtInType);
                    return;
                }
            }

            // Should never happen.
            throw new EncodingException(
                $"Bad variant: Type '{value.GetType().FullName}' is not allowed in Variant.");
        }

        /// <summary>
        /// Cast to array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static IList<T> ToTypedArray<T>(object value) where T : struct
        {
            return ToTypedArray(value, default(T));
        }

        /// <summary>
        /// Cast to array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <param name="outerException"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private static IList<T?> ToTypedArray<T>(object? value, T? defaultValue,
            Func<Exception, Exception>? outerException = null)
        {
            if (value == null)
            {
                return [];
            }
            if (value is T[] t)
            {
                return t;
            }
            if (value is not Array arr)
            {
                return ToTypedScalar(value, defaultValue).YieldReturn().ToArray();
            }
            if (arr.Length == 0)
            {
                return [];
            }
            var result = new T?[arr.Length];
            for (var index = 0; index < arr.Length; index++)
            {
                var item = arr.GetValue(index);
                if (item == null)
                {
                    result[index] = defaultValue;
                    continue;
                }
                if (item is not byte[] and
                    Array itemArray)
                {
                    return ToTypedArray(itemArray, defaultValue,
                        ex => GetException(value, arr, item, ex));
                }
                try
                {
                    result[index] = (T?)item;
                }
                catch
                {
                    try
                    {
                        result[index] = item.As<T?>();
                    }
                    catch (Exception ex)
                    {
                        if (outerException != null)
                        {
                            ex = outerException(ex);
                        }
                        throw GetException(value, arr, item, ex);
                    }
                }
            }
            return result;

            static EncodingException GetException(object value, Array arr, object item,
                Exception ex)
            {
                return new EncodingException("Bad variant: " +
                    $"Value '{value}' with length {arr.Length} of type '{value.GetType().FullName}'" +
                    $" with item '{item}' of type '{item.GetType().FullName}' is not of type " +
                    $"'{typeof(T).GetType().FullName}'.", ex);
            }
        }

        /// <summary>
        /// Cast to primitive scalar
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T ToTypedScalar<T>(object value) where T : struct
        {
            return ToTypedScalar(value, default(T));
        }

        /// <summary>
        /// Cast to array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        [return: NotNullIfNotNull(nameof(defaultValue))]
        private static T? ToTypedScalar<T>(object? value, T? defaultValue)
        {
            try
            {
                if (value == null)
                {
                    return defaultValue;
                }
                if (value is T t)
                {
                    return t;
                }
                return (T)value;
            }
            catch (Exception ex)
            {
                throw new EncodingException(
                    $"Bad variant: Value '{value}' of type '{value?.GetType().FullName}' " +
                    $"is not a scalar of type '{typeof(T).GetType().FullName}'.", ex);
            }
        }

        /// <summary>
        /// Write multi dimensional array
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="dim"></param>
        /// <param name="index"></param>
        /// <param name="builtInType"></param>
        private void WriteMatrix(Matrix matrix, int dim, ref int index,
            BuiltInType builtInType)
        {
            var arrayLen = matrix.Dimensions[dim];
            if (dim == matrix.Dimensions.Length - 1)
            {
                // Create a slice of values for the top dimension
                var copy = Array.CreateInstance(
                    matrix.Elements.GetType()!.GetElementType()!, arrayLen);
                Array.Copy(matrix.Elements, index, copy, 0, arrayLen);
                // Write slice as value rank
                WriteVariantContents(copy, 1, builtInType);
                index += arrayLen;
            }
            else
            {
                PushArray(null, arrayLen);
                for (var i = 0; i < arrayLen; i++)
                {
                    WriteMatrix(matrix, dim + 1, ref index, builtInType);
                }
                PopArray();
            }
        }

        /// <summary>
        /// Write multi dimensional array in structure.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="matrix"></param>
        /// <param name="dim"></param>
        /// <param name="index"></param>
        /// <param name="typeInfo"></param>
        /// <exception cref="EncodingException"></exception>
        private void WriteStructureMatrix(string? fieldName,
            Matrix matrix, int dim, ref int index, TypeInfo typeInfo)
        {
            // check the nesting level for avoiding a stack overflow.
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded.");
            }

            _nestingLevel++;

            try
            {
                var arrayLen = matrix.Dimensions[dim];
                if (dim == matrix.Dimensions.Length - 1)
                {
                    // Create a slice of values for the top dimension
                    var copy = Array.CreateInstance(
                        matrix.Elements.GetType()!.GetElementType()!, arrayLen);
                    Array.Copy(matrix.Elements, index, copy, 0, arrayLen);
                    // Write slice as value rank

                    WriteVariantContents(copy, 1, typeInfo.BuiltInType);
                    index += arrayLen;
                }
                else
                {
                    PushArray(fieldName, arrayLen);
                    for (var i = 0; i < arrayLen; i++)
                    {
                        WriteStructureMatrix(null, matrix, dim + 1, ref index, typeInfo);
                    }
                    PopArray();
                }
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <summary>
        /// Write type
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        private void WriteBuiltInType(string? property, BuiltInType type)
        {
            if (UseAdvancedEncoding)
            {
                WriteString(property, type.ToString());
            }
            else
            {
                WriteByte(property, (byte)type);
            }
        }

        /// <summary>
        /// Writes namespace
        /// </summary>
        /// <param name="namespaceIndex"></param>
        private void WriteNamespaceIndex(ushort namespaceIndex)
        {
            if (namespaceIndex > 1)
            {
                var uri = Context.NamespaceUris.GetString(namespaceIndex);
                if (!string.IsNullOrEmpty(uri))
                {
                    WriteString("Uri", uri);
                    return;
                }
            }
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            if (namespaceIndex != 0)
            {
                WriteUInt32("Index", namespaceIndex);
            }
        }

        /// <summary>
        /// Encode an array according to its valueRank and BuiltInType
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="array"></param>
        /// <param name="valueRank"></param>
        /// <param name="builtInType"></param>
        /// <exception cref="EncodingException"></exception>
        public void WriteArray(string fieldName, object array, int valueRank,
            BuiltInType builtInType)
        {
            // write array.
            if (valueRank == ValueRanks.OneDimension)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        WriteBooleanArray(fieldName, (bool[])array);
                        return;
                    case BuiltInType.SByte:
                        WriteSByteArray(fieldName, (sbyte[])array);
                        return;
                    case BuiltInType.Byte:
                        WriteByteArray(fieldName, (byte[])array);
                        return;
                    case BuiltInType.Int16:
                        WriteInt16Array(fieldName, (short[])array);
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16Array(fieldName, (ushort[])array);
                        return;
                    case BuiltInType.Int32:
                        WriteInt32Array(fieldName, (int[])array);
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32Array(fieldName, (uint[])array);
                        return;
                    case BuiltInType.Int64:
                        WriteInt64Array(fieldName, (long[])array);
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64Array(fieldName, (ulong[])array);
                        return;
                    case BuiltInType.Float:
                        WriteFloatArray(fieldName, (float[])array);
                        return;
                    case BuiltInType.Double:
                        WriteDoubleArray(fieldName, (double[])array);
                        return;
                    case BuiltInType.String:
                        WriteStringArray(fieldName, (string[])array);
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTimeArray(fieldName, (DateTime[])array);
                        return;
                    case BuiltInType.Guid:
                        WriteGuidArray(fieldName, (Uuid[])array);
                        return;
                    case BuiltInType.ByteString:
                        WriteByteStringArray(fieldName, (byte[][])array);
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElementArray(fieldName, (XmlElement[])array);
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeIdArray(fieldName, (NodeId[])array);
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeIdArray(fieldName, (ExpandedNodeId[])array);
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCodeArray(fieldName, (StatusCode[])array);
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedNameArray(fieldName, (QualifiedName[])array);
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedTextArray(fieldName, (LocalizedText[])array);
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObjectArray(fieldName, (ExtensionObject[])array);
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValueArray(fieldName, (DataValue[])array);
                        return;
                    case BuiltInType.DiagnosticInfo:
                        WriteDiagnosticInfoArray(fieldName, (DiagnosticInfo[])array);
                        return;
                    case BuiltInType.Enumeration:
                        if (array is not Array enumArray)
                        {
                            throw new EncodingException(
                                "Unexpected non Array type encountered while encoding an array of enumeration.");
                        }
                        var enumType = enumArray.GetType().GetElementType();
                        Debug.Assert(enumType != null);
                        WriteEnumeratedArray(fieldName, enumArray, enumType);
                        return;
                    case BuiltInType.Variant:
                        switch (array)
                        {
                            case Variant[] variants:
                                WriteVariantArray(fieldName, variants);
                                return;
                            case object[] objects:
                                WriteObjectArray(fieldName, objects);
                                return;
                            case null:
                                WriteObjectArray(fieldName, null);
                                return;
                            default:
                                throw new EncodingException("Unexpected type encountered " +
                                    $"while encoding an array of Variants: {array.GetType()}");
                        }
                }
            }
            // write matrix.
            else if (valueRank > ValueRanks.OneDimension)
            {
                if (array is Matrix matrix)
                {
                    var index = 0;
                    WriteStructureMatrix(fieldName, matrix, 0, ref index, matrix.TypeInfo);
                    return;
                }
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        internal void WriteArray<T>(string? property, IList<T>? values,
            Action<T> writer)
        {
            if (values == null)
            {
                WriteNull(property);
            }
            else
            {
                PushArray(property, values.Count);
                foreach (var value in values)
                {
                    writer(value);
                }
                PopArray();
            }
        }

        /// <inheritdoc/>
        internal void WriteObject<T>(string? property, T value, Action<T> writer)
            where T : class
        {
            if (value == null)
            {
                WriteNull(property);
            }
            else
            {
                PushObject(property);
                writer(value);
                PopObject();
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        private void WriteDictionary<T>(string? property,
            IEnumerable<(string Key, T Value)>? values, Action<string, T> writer)
        {
            if (values == null)
            {
                WriteNull(property);
            }
            else
            {
                PushObject(property);
                foreach (var (Key, Value) in values)
                {
                    writer(Key, Value);
                }
                PopObject();
            }
        }

        /// <summary>
        /// Check whether to write the simple value.  If so
        /// andthis is not called in the context of array
        /// write (property == null) write property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns>true if should write value.</returns>
        private bool PreWriteValue<T>(string? property, T value) where T : struct
        {
            if (!string.IsNullOrEmpty(property))
            {
                if (IgnoreDefaultValues && EqualityComparer<T>.Default.Equals(value, default))
                {
                    return false;
                }
                _writer?.WritePropertyName(property);
            }
            return true;
        }

        /// <summary>
        /// Write null
        /// </summary>
        /// <param name="property"></param>
        private void WriteNull(string? property)
        {
            if (!string.IsNullOrEmpty(property))
            {
                if (IgnoreNullValues || IgnoreDefaultValues)
                {
                    // only skip null if not in array context.
                    return;
                }
                _writer?.WritePropertyName(property);
            }
            _writer?.WriteNull();
        }

        /// <summary>
        /// Push new object
        /// </summary>
        /// <param name="property"></param>
        /// <exception cref="EncodingException"></exception>
        private void PushObject(string? property)
        {
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded");
            }
            if (!string.IsNullOrEmpty(property))
            {
                _writer?.WritePropertyName(property);
            }
            _writer?.WriteStartObject();
            _nestingLevel++;
        }

        /// <summary>
        /// Pop structure
        /// </summary>
        private void PopObject()
        {
            _writer?.WriteEndObject();
            _nestingLevel--;
        }

        /// <summary>
        /// Push new array
        /// </summary>
        /// <param name="property"></param>
        /// <param name="count"></param>
        /// <exception cref="EncodingException"></exception>
        private void PushArray(string? property, int count)
        {
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < count)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxArrayLength {Context.MaxArrayLength} < {count}.");
            }
            if (!string.IsNullOrEmpty(property))
            {
                _writer?.WritePropertyName(property);
            }
            _writer?.WriteStartArray();
        }

        /// <summary>
        /// Pop array
        /// </summary>
        private void PopArray()
        {
            _writer?.WriteEndArray();
        }

        private JsonWriter? _writer;
        private readonly JsonEncoding _encoding;
        private readonly Stack<string> _namespaces;
        private ushort[]? _namespaceMappings;
        private readonly bool _ownedWriter;
        private uint _nestingLevel;
    }
}
