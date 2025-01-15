// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using global::Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Avro schema extensions
    /// </summary>
    internal static class AvroSchema
    {
        /// <summary>
        /// Null schema
        /// </summary>
        public static Schema Null { get; } = PrimitiveSchema.NewInstance("null");

        /// <summary>
        /// Set data type
        /// </summary>
        /// <param name="dataTypeId"></param>
        /// <returns></returns>
        public static PropertyMap Properties(string? dataTypeId)
        {
            return new PropertyMap()
                .AddProperty(kUaDataTypeIdKey, dataTypeId);
        }

        /// <summary>
        /// Get the property map
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static PropertyMap AddProperty(this PropertyMap properties,
            string key, string? value)
        {
            if (value != null)
            {
                // Need to add json strings
                properties.Add(key, "\"" + value + "\"");
            }
            return properties;
        }

        /// <summary>
        /// Get the data type id
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ExpandedNodeId GetDataTypeId(this Schema schema,
            IServiceMessageContext context)
        {
            var type = schema.GetProperty(kUaDataTypeIdKey);
            if (type == null &&
                schema is NamedSchema ns &&
                context.NamespaceUris.TryFindNamespace(ns.Namespace,
                    out var namespaceIndex, out var namespaceUri))
            {
                return new ExpandedNodeId(ns.Name,
                    (ushort)namespaceIndex, namespaceUri, 0);
            }
            if (type == null)
            {
                return ExpandedNodeId.Null;
            }
            return type.TrimQuotes().ToExpandedNodeId(context);
        }

        /// <summary>
        /// Create nullable
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Schema AsNullable(this Schema schema)
        {
            if (schema == Null)
            {
                return schema;
            }
            if (schema is UnionSchema u)
            {
                if (!u.Schemas.Contains(Null))
                {
                    u.Schemas.Insert(0, Null);
                }
                else
                {
                    Debug.Assert(u.Schemas[0] == Null);
                }
                return u;
            }
            return UnionSchema.Create(
            [
                Null,
                schema
            ]);
        }

        /// <summary>
        /// Returns the schema as formatted json
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string ToJson(this Schema schema,
            JsonSerializerOptions? options = null)
        {
            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, options ?? kIndented);
        }

        /// <summary>
        /// Is data value
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static bool IsDataValue(this Schema schema)
        {
            if (schema is not RecordSchema r)
            {
                return false;
            }
            return r.Fields.Count == 6 && r.Fields
                .Select(r => r.Name).SequenceEqual(new[]
                {
                    "Value",
                    "StatusCode",
                    "SourceTimestamp",
                    "SourcePicoseconds",
                    "ServerTimestamp",
                    "ServerPicoseconds"
                });
        }

        /// <summary>
        /// Test for built in type
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        public static bool IsBuiltInType(this Schema schema,
            out BuiltInType builtInType, out SchemaRank valueRank)
        {
            valueRank = SchemaRank.Scalar;
            if (schema is RecordSchema ns &&
                ns.SchemaName.Namespace == SchemaUtils.NamespaceZeroName)
            {
                var name = ns.Name;
                if (name.EndsWith(nameof(SchemaRank.Collection),
                    StringComparison.InvariantCulture))
                {
                    valueRank = SchemaRank.Collection;
                    name = name[..^nameof(SchemaRank.Collection).Length];
                }
                else if (name.EndsWith(nameof(SchemaRank.Matrix),
                    StringComparison.InvariantCulture))
                {
                    valueRank = SchemaRank.Matrix;
                    name = name[..^nameof(SchemaRank.Matrix).Length];
                }
                if (Enum.TryParse(name, out builtInType))
                {
                    return true;
                }
            }
            if (schema is ArraySchema a)
            {
                valueRank = SchemaRank.Collection;
                schema = a.ItemSchema;
            }
            if (schema is EnumSchema)
            {
                builtInType = BuiltInType.Enumeration;
                return true;
            }
            builtInType = BuiltInType.Null;
            return false;
        }

        /// <summary>
        /// Create array
        /// </summary>
        /// <param name="itemSchema"></param>
        /// <param name="isRoot"></param>
        /// <returns></returns>
        public static Schema AsArray(this Schema itemSchema, bool isRoot = false)
        {
            var schema = ArraySchema.Create(itemSchema, isRoot ? new PropertyMap
            {
                ["root"] = "true"
            } : null);
            if (isRoot)
            {
                schema.CreateRoot();
            }
            return schema;
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static Schema AsUnion(params Schema[] schemas)
        {
            return schemas.AsUnion(customProperties: null);
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="schemas"></param>
        /// <param name="customProperties"></param>
        /// <returns></returns>
        public static UnionSchema AsUnion(this IEnumerable<Schema> schemas,
            PropertyMap? customProperties = null)
        {
            var types = schemas.Distinct(
                Compare.Using<Schema>((a, b) => a?.Fullname == b?.Fullname)).ToList();
            return UnionSchema.Create(types, customProperties);
        }

        /// <summary>
        /// Create root schema for a schema or field at the root
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static RecordSchema CreateRoot(this Schema schema, string? fieldName = null)
        {
            return RecordSchema.Create(kRootSchemaName,
            [
                new (schema, fieldName ?? kRootFieldName, 0)
            ], kRootNamespace);
        }

        /// <summary>
        /// Check whether to skip the dummy root during validation
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static bool IsRoot(this Schema schema)
        {
            return schema is RecordSchema r && r.Name == kRootSchemaName &&
                r.Fields.Count == 1 && r.Namespace == kRootNamespace;
        }

        /// <summary>
        /// Unwrap a root place holder
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Schema Unwrap(this Schema schema)
        {
            if (schema.IsRoot())
            {
                var root = (RecordSchema)schema;
                if (root.Fields.Count == 1 &&
                    root.Fields[0].Name == kRootFieldName)
                {
                    root = root.Fields[0].Schema as RecordSchema;
                    if (root != null)
                    {
                        return root;
                    }
                }
            }
            return schema;
        }

        /// <summary>
        /// Create derived schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static PlaceHolder CreatePlaceHolder(string name,
            string ns)
        {
            return new PlaceHolder(Schema.Type.Record,
                new SchemaName(name, ns, null, null));
        }

        /// <summary>
        /// Derived schema
        /// </summary>
        public class PlaceHolder : NamedSchema
        {
            /// <inheritdoc/>
            public PlaceHolder(Type type, SchemaName name,
                IList<string>? aliases = null, PropertyMap? props = null,
                SchemaNames? names = null, string? doc = null)
                : base(type, name, GetSchemaNames(aliases, name),
                      props, names ?? new SchemaNames(), doc)
            {
            }

            internal static IList<SchemaName>? GetSchemaNames(
                IEnumerable<string>? aliases, SchemaName typeName)
            {
                if (aliases == null)
                {
                    return null;
                }
                return aliases.Select(alias => new SchemaName(
                    alias, typeName.Namespace, null, null)).ToList();
            }
        }

        private const string kRootFieldName = "Value";
        private const string kRootSchemaName = "Type";
        private const string kRootNamespace = "org.apache.avro";

        private const string kUaDataTypeIdKey = "uaDataTypeId";

        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };
    }
}
