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

        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Test for built in type
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        public static bool IsBuiltInType(this Schema schema,
            out BuiltInType builtInType)
        {
            if (schema is NamedSchema ns &&
                ns.SchemaName.Namespace == SchemaUtils.NamespaceZeroName &&
                Enum.TryParse(ns.Name, out builtInType))
            {
                return true;
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
    }
}
