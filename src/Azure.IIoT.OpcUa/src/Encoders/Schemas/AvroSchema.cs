// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
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
        /// Get the property map
        /// </summary>
        /// <param name="dataTypeId"></param>
        /// <returns></returns>
        public static PropertyMap GetProperties(string dataTypeId)
        {
            // Need to add json strings
            return new PropertyMap
            {
                ["uaDataTypeId"] = "\"" + dataTypeId + "\""
            };
        }

        /// <summary>
        /// Create nullable
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Schema AsNullable(this Schema schema)
        {
            return schema == Null ? schema :
                UnionSchema.Create(new List<Schema>
                {
                    Null,
                    schema
                });
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
        /// Test for built in type
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        public static bool IsBuiltInType(this Schema schema,
            out BuiltInType builtInType, out int valueRank)
        {
            valueRank = ValueRanks.Scalar;
            if (schema is NamedSchema ns &&
                ns.SchemaName.Namespace == SchemaUtils.NamespaceZeroName)
            {
                var name = ns.Name;
                if (name.EndsWith("Collection", StringComparison.InvariantCulture))
                {
                    valueRank = ValueRanks.OneDimension;
                    name = name[..^10];
                }
                else if (name.EndsWith("Matrix", StringComparison.InvariantCulture))
                {
                    valueRank = ValueRanks.TwoDimensions;
                    name = name[..^6];
                }
                if (Enum.TryParse(name, out builtInType))
                {
                    return true;
                }
            }
            builtInType = BuiltInType.Null;
            return false;
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
            var type = schema.GetProperty("uaDataTypeId");
            if (type == null &&
                schema is NamedSchema ns &&
                context.NamespaceUris.TryFindNamespace(ns.Namespace,
                    out var namespaceIndex, out var namespaceUri))
            {
                return new ExpandedNodeId(ns.Name,
                    (ushort)namespaceIndex, namespaceUri, 0);
            }
            return type.ToExpandedNodeId(context);
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static Schema CreateUnion(params Schema[] schemas)
        {
            return CreateUnion(schemas, customProperties: null);
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="schemas"></param>
        /// <param name="customProperties"></param>
        /// <returns></returns>
        public static UnionSchema CreateUnion(IEnumerable<Schema> schemas,
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
            return RecordSchema.Create(kRootSchemaName, new List<Field>
            {
                new (schema, fieldName ?? kRootFieldName, 0)
            }, kRootNamespace);
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

        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };
    }
}
