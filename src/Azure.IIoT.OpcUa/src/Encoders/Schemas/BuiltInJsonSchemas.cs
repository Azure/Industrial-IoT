// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Microsoft.Json.Schema;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;

    /// <summary>
    /// Provides the json encodings of built in types and objects in Avro schema
    /// </summary>
    internal class BuiltInJsonSchemas : BaseBuiltInSchemas<JsonSchema>
    {
        /// <summary>
        /// Schema definitions
        /// </summary>
        public Dictionary<string, JsonSchema> Definitions { get; }

        private JsonSchema EnumerationSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    // Enumeration values shall be encoded as a JSON number
                    // for the reversible encoding.
                    return PrimitiveType((int)BuiltInType.Enumeration,
                        SchemaType.Integer, "int32");
                }

                // For the non - reversible form, Enumeration values are
                // encoded as a JSON string with the following format:
                // <name>_<value>
                return PrimitiveType((int)BuiltInType.Enumeration,
                    SchemaType.String);
            }
        }

        private JsonSchema DiagnosticInfoSchema
        {
            get
            {
                return new JsonSchema
                {
                    Title = nameof(BuiltInType.DiagnosticInfo),
                    Id = GetDataTypeId(BuiltInType.DiagnosticInfo),
                    Type = new[] { SchemaType.Object },
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["SymbolicId"] = GetSchemaForBuiltInType(BuiltInType.Int32),
                        ["NamespaceUri"] = GetSchemaForBuiltInType(BuiltInType.Int32),
                        ["Locale"] = GetSchemaForBuiltInType(BuiltInType.Int32),
                        ["LocalizedText"] = GetSchemaForBuiltInType(BuiltInType.Int32),
                        ["AdditionalInfo"] = GetSchemaForBuiltInType(BuiltInType.String),
                        ["InnerStatusCode"] = GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        ["InnerDiagnosticInfo"] = new JsonSchema
                        {
                            // Self reference
                            Reference = new UriOrFragment("#")
                        }
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema VariantSchema
        {
            get
            {
                if (!_reversibleEncoding)
                {
                    // For the non-reversible form, Variant values shall be
                    // encoded as a JSON value containing only the value of
                    // the Body field. The Type and Dimensions fields are
                    // dropped. Multi-dimensional arrays are encoded as a
                    // multi-dimensional JSON array as described in 5.4.5.
                    return new JsonSchema();
                }
                return new JsonSchema
                {
                    Id = GetDataTypeId(BuiltInType.Variant),
                    Type = new[] { SchemaType.Object },
                    Title = nameof(BuiltInType.Variant),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["Type"] = GetSchemaForBuiltInType(BuiltInType.Byte),
                        ["Body"] = new JsonSchema(),
                        ["Dimensions"] = GetSchemaForBuiltInType(BuiltInType.UInt32,
                            ValueRanks.OneDimension)
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema ExtensionObjectSchema
        {
            get
            {
                if (!_reversibleEncoding)
                {
                    // For the non-reversible form, ExtensionObject values
                    // shall be encoded as a JSON value containing only the
                    // value of the Body field. The TypeId and Encoding
                    // fields are dropped.
                    return new JsonSchema
                    {
                        Type = new[] { SchemaType.Object }
                    };
                }
                return new JsonSchema
                {
                    Id = GetDataTypeId(BuiltInType.ExtensionObject),
                    Type = new[] { SchemaType.Object },
                    Title = nameof(BuiltInType.ExtensionObject),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["TypeId"] = GetSchemaForBuiltInType(BuiltInType.NodeId),
                        ["Encoding"] = GetSchemaForBuiltInType(BuiltInType.Byte),
                        ["Body"] = new JsonSchema
                        {
                            Type = new[] { SchemaType.Object }
                        }
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema StatusCodeSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    //
                    // StatusCode values shall be encoded as a JSON number for
                    // the reversible encoding. If the StatusCode is Good (0)
                    // it is only encoded if it is an element of a JSON array.
                    //
                    return PrimitiveType((int)BuiltInType.StatusCode,
                        SchemaType.Integer, "uint32");
                }

                // For the non - reversible form, StatusCode values
                // shall be encoded as a JSON object with the fields
                // defined here.
                return new JsonSchema
                {
                    Id = GetDataTypeId(BuiltInType.StatusCode),
                    Type = new[] { SchemaType.Object },
                    Title = nameof(BuiltInType.StatusCode),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["Code"] = GetSchemaForBuiltInType(BuiltInType.UInt32),
                        ["Symbol"] = GetSchemaForBuiltInType(BuiltInType.String)
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema QualifiedNameSchema
        {
            get
            {
                if (_encodeNamespacedValuesAsUri)
                {
                    return PrimitiveType((int)BuiltInType.QualifiedName, SchemaType.String,
                        "opcuaQualifiedName");
                }

                // For non-reversible encoding this field is the JSON
                // string containing the NamespaceUri associated with
                // the NamespaceIndex unless the NamespaceIndex is 0.
                // If the NamespaceIndex is 0 the field is omitted.
                return new JsonSchema
                {
                    Title = nameof(BuiltInType.QualifiedName),
                    Id = GetDataTypeId(BuiltInType.QualifiedName),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["Name"] = GetSchemaForBuiltInType(BuiltInType.String),
                        ["Uri"] = GetSchemaForBuiltInType(
                            _reversibleEncoding ? BuiltInType.UInt32 : BuiltInType.String)
                    },
                    Required = new[] { "Name" },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema LocalizedTextSchema
        {
            get
            {
                if (!_reversibleEncoding)
                {
                    // For the non-reversible form, LocalizedText value shall
                    // be encoded as a JSON string containing the Text component.
                    return PrimitiveType((int)BuiltInType.LocalizedText, SchemaType.String);
                }
                return new JsonSchema
                {
                    Title = nameof(BuiltInType.LocalizedText),
                    Id = GetDataTypeId(BuiltInType.LocalizedText),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["Locale"] = PrimitiveType((int)BuiltInType.String,
                                SchemaType.String, "rfc3066"),
                        ["Text"] = GetSchemaForBuiltInType(BuiltInType.String)
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema NodeIdSchema
        {
            get
            {
                if (_encodeNamespacedValuesAsUri)
                {
                    return PrimitiveType((int)BuiltInType.NodeId, SchemaType.String,
                        "opcuaNodeId");
                }

                return new JsonSchema
                {
                    Title = nameof(BuiltInType.NodeId),
                    Id = GetDataTypeId(BuiltInType.NodeId),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["IdentifierType"] = GetSchemaForBuiltInType(BuiltInType.Byte),
                        ["Id"] = new JsonSchema(),
                        //
                        // For reversible encoding this field is a JSON number
                        // with the NamespaceIndex. The field is omitted if the
                        // NamespaceIndex is 0.
                        //
                        // For non-reversible encoding this field is the JSON
                        // string containing the NamespaceUri associated with
                        // the NamespaceIndex unless the NamespaceIndex is 0.
                        // If the NamespaceIndex is 0 the field is omitted.
                        //
                        ["Namespace"] = GetSchemaForBuiltInType(
                            _reversibleEncoding ? BuiltInType.UInt32 : BuiltInType.String)
                    },
                    Required = new[] { "IdentifierType" },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema ExpandedNodeIdSchema
        {
            get
            {
                if (_encodeNamespacedValuesAsUri)
                {
                    return PrimitiveType((int)BuiltInType.ExpandedNodeId, SchemaType.String,
                        "opcuaExpandedNodeId");
                }
                return new JsonSchema
                {
                    Title = nameof(BuiltInType.ExpandedNodeId),
                    Id = GetDataTypeId(BuiltInType.ExpandedNodeId),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["IdentifierType"] = GetSchemaForBuiltInType(BuiltInType.Byte),
                        ["Id"] = new JsonSchema(),
                        //
                        // For reversible encoding this field is a JSON number
                        // with the NamespaceIndex. The field is omitted if the
                        // NamespaceIndex is 0.
                        //
                        // For non-reversible encoding this field is the JSON
                        // string containing the NamespaceUri associated with
                        // the NamespaceIndex unless the NamespaceIndex is 0.
                        // If the NamespaceIndex is 0 the field is omitted.
                        //
                        ["Namespace"] = GetSchemaForBuiltInType(
                            _reversibleEncoding ? BuiltInType.UInt32 : BuiltInType.String),
                        //
                        // For reversible encoding this field is a JSON string
                        // with the NamespaceUri if the NamespaceUri is specified.
                        // Otherwise, it is a JSON number with the NamespaceIndex.
                        // The field is omitted if the NamespaceIndex is 0.
                        //
                        // For non-reversible encoding this field is the JSON string
                        // containing the NamespaceUri or the NamespaceUri associated
                        // with the NamespaceIndex unless the NamespaceIndex is 0
                        // or 1. If the NamespaceIndex is 0 the field is omitted.
                        //
                        ["ServerUri"] = GetSchemaForBuiltInType(
                            _reversibleEncoding ? BuiltInType.UInt16 : BuiltInType.String)
                    },
                    Required = new[] { "IdentifierType" },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        private JsonSchema DataValueSchema
        {
            get
            {
                return new JsonSchema
                {
                    Title = nameof(BuiltInType.DataValue),
                    Id = GetDataTypeId(BuiltInType.DataValue),
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["Value"] = GetSchemaForBuiltInType(BuiltInType.Variant),
                        ["Status"] = GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        ["SourceTimestamp"] = GetSchemaForBuiltInType(BuiltInType.DateTime),
                        ["SourcePicoSeconds"] = GetSchemaForBuiltInType(BuiltInType.UInt16),
                        ["ServerTimestamp"] = GetSchemaForBuiltInType(BuiltInType.DateTime),
                        ["ServerPicoSeconds"] = GetSchemaForBuiltInType(BuiltInType.UInt16)
                    },
                    AdditionalProperties = new AdditionalProperties(false)
                };
            }
        }

        /// <summary>
        /// Create avro schema for json encoder
        /// </summary>
        /// <param name="reversibleEncoding"></param>
        /// <param name="useUriEncoding"></param>
        /// <param name="definitions"></param>
        public BuiltInJsonSchemas(bool reversibleEncoding,
            bool useUriEncoding, Dictionary<string, JsonSchema>? definitions)
        {
            Definitions = definitions ?? new();
            _reversibleEncoding = reversibleEncoding;
            _encodeNamespacedValuesAsUri = useUriEncoding;
        }

        /// <summary>
        /// Create encoding schema
        /// </summary>
        /// <param name="fieldContentMask"></param>
        /// <param name="definitions"></param>
        public BuiltInJsonSchemas(DataSetFieldContentMask fieldContentMask,
            Dictionary<string, JsonSchema>? definitions = null)
        {
            Definitions = definitions ?? new();
            _encodeNamespacedValuesAsUri = true;

            if ((fieldContentMask & DataSetFieldContentMask.RawData) != 0)
            {
                //
                // If the DataSetFieldContentMask results in a RawData
                // representation, the field value is a Variant encoded
                // using the non-reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6
                //
                _reversibleEncoding = false;
            }
            else if (fieldContentMask == 0)
            {
                //
                // If the DataSetFieldContentMask results in a Variant
                // representation, the field value is encoded as a Variant
                // encoded using the reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6.
                //
                _reversibleEncoding = true;
            }
            else
            {
                //
                // If the DataSetFieldContentMask results in a DataValue
                // representation, the field value is a DataValue encoded
                // using the non-reversible OPC UA JSON Data Encoding or
                // reversible depending on encoder configuration.
                //
                _reversibleEncoding = false;
            }
        }

        /// <summary>
        /// Get built in schema. See
        /// https://reference.opcfoundation.org/Core/Part6/v104/docs/5.1.2#_Ref131507956
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override JsonSchema GetSchemaForBuiltInType(BuiltInType builtInType,
            int rank = ValueRanks.Scalar)
        {
            if (!Definitions.TryGetValue(GetDataTypeName(builtInType), out var schema))
            {
                schema = Get((int)builtInType);
                if (schema.Id != null)
                {
                    Definitions.Add(GetDataTypeName(builtInType), schema);
                    schema = BuiltInReference(builtInType);
                }
            }
            if (rank >= ValueRanks.OneOrMoreDimensions)
            {
                return new JsonSchema
                {
                    Type = new[] { SchemaType.Array },
                    Items = new Items(schema)
                };
            }
            return schema;

            JsonSchema Get(int id) => id switch
            {
                1 => PrimitiveType(id, SchemaType.Boolean),
                2 => PrimitiveType(id, SchemaType.Integer, "int8"),
                3 => PrimitiveType(id, SchemaType.Integer, "byte"),
                4 => PrimitiveType(id, SchemaType.Integer, "int16"),
                5 => PrimitiveType(id, SchemaType.Integer, "uint16"),
                6 => PrimitiveType(id, SchemaType.Integer, "int32"),
                7 => PrimitiveType(id, SchemaType.Integer, "uint32"),

                // As per part 6 encoding, long is encoded as string
                8 => PrimitiveType(id, SchemaType.String, "int64"),
                9 => PrimitiveType(id, SchemaType.String, "uint64"),

                10 => PrimitiveType(id, SchemaType.Number, "float"),
                11 => PrimitiveType(id, SchemaType.Number, "double"),
                12 => PrimitiveType(id, SchemaType.String),
                13 => PrimitiveType(id, SchemaType.String, "date-time"),
                14 => PrimitiveType(id, SchemaType.String, "uuid"),
                15 => PrimitiveType(id, SchemaType.String, "byte"),
                16 => PrimitiveType(id, SchemaType.String, "xmlelement"),

                17 => NodeIdSchema,
                18 => ExpandedNodeIdSchema,
                19 => StatusCodeSchema,
                20 => QualifiedNameSchema,
                21 => LocalizedTextSchema,
                22 => ExtensionObjectSchema,
                23 => DataValueSchema,
                24 => VariantSchema,
                25 => DiagnosticInfoSchema,

                26 => PrimitiveType(id, SchemaType.Number),

                // Should this be string? As per json encoding, long is string
                27 => PrimitiveType(id, SchemaType.Integer),
                28 => PrimitiveType(id, SchemaType.Number, "unsigned"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        /// <inheritdoc/>
        public override JsonSchema GetSchemaForExtendableType(string name, string ns,
            string dataTypeId, JsonSchema bodyType)
        {
            return GetSchemaForBuiltInType(BuiltInType.ExtensionObject);
        }

        /// <inheritdoc/>
        public override JsonSchema GetSchemaForDataSetField(string name, bool asDataValue,
            JsonSchema valueSchema)
        {
            return GetSchemaForBuiltInType(BuiltInType.DataValue);
        }

        /// <summary>
        /// Get data typeid
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static UriOrFragment GetDataTypeId(BuiltInType builtInType)
        {
            return new UriOrFragment(Namespaces.OpcUa + "#" + GetDataTypeName(builtInType));
        }

        /// <summary>
        /// Get data typeid
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static string GetDataTypeName(BuiltInType builtInType)
        {
            return "i=" + (int)builtInType;
        }

        /// <summary>
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        internal static JsonSchema PrimitiveType(int builtInType,
            SchemaType type, string? format = null)
        {
            return new JsonSchema
            {
                Type = new[] { type },
                Title = ((BuiltInType)builtInType).ToString(),
                Format = format
            };
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static JsonSchema BuiltInReference(BuiltInType builtInType)
        {
            return new JsonSchema
            {
                Reference = GetDataTypeId(builtInType)
            };
        }

        private readonly bool _reversibleEncoding;
        private readonly bool _encodeNamespacedValuesAsUri;
    }
}
