// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Json.Schema;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extensions
    /// </summary>
    internal static class SchemaExtensions
    {
        /// <summary>
        /// Create a reference
        /// </summary>
        /// <param name="definitions"></param>
        /// <param name="id"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JsonSchema Reference(this Dictionary<string, JsonSchema> definitions,
            UriOrFragment id, Func<UriOrFragment, JsonSchema> schema)
        {
            if (!definitions.ContainsKey(id.ToString()!))
            {
                definitions.Add(id.ToString()!, schema(id));
            }
            return new JsonSchema
            {
                Reference = id
            };
        }

        /// <summary>
        /// Make union type from schemas
        /// </summary>
        /// <param name="schemas"></param>
        /// <param name="definitions"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JsonSchema AsUnion(this IEnumerable<JsonSchema> schemas,
            Dictionary<string, JsonSchema> definitions, string? title = null)
        {
            var s = schemas.ToList();
            if (s.Count == 0)
            {
                throw new ArgumentException("Union must have at least one schema",
                    nameof(schemas));
            }
            return new JsonSchema
            {
                Title = title,
                Types = s.Select(s => Resolve(s, definitions)
                    .SafeGetType()).Distinct().ToArray(),
                OneOf = s
            };
        }

        /// <summary>
        /// Make array from schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static JsonSchema AsArray(this JsonSchema schema,
            string? title = null)
        {
            return new JsonSchema
            {
                Title = title,
                Types = new[] { SchemaType.Array },
                Items = new[] { schema }
            };
        }

        /// <summary>
        /// Make array from schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public static JsonSchema Resolve(this JsonSchema schema,
            Dictionary<string, JsonSchema> definitions)
        {
            if (schema.Reference == null)
            {
                Debug.Assert(schema.Types != null);
                return schema;
            }
            return definitions.Values.First(d => d.Id == schema.Reference);
        }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indented"></param>
        /// <returns></returns>
        public static string ToJsonString(this JsonSchema schema, bool indented = false)
        {
            return SchemaWriter.SerializeAsString(schema, indented);
        }

        /// <summary>
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this string nodeId,
            ServiceMessageContext context)
        {
            return nodeId.ToExpandedNodeId(context).GetSchemaId(context);
        }

        /// <summary>
        /// Create identifier of a schema in the namespace
        /// configured in the options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="fragment"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this SchemaOptions options,
            string fragment)
        {
            var ns = options.Namespace ?? Namespaces.OpcUaSdk;
            return new UriOrFragment(ns + "#" + fragment);
        }

        /// <summary>
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this NodeId nodeId,
            ServiceMessageContext context)
        {
            return nodeId.ToExpandedNodeId(context.NamespaceUris).GetSchemaId(context);
        }

        /// <summary>
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this ExpandedNodeId nodeId,
            ServiceMessageContext context)
        {
            if (string.IsNullOrEmpty(nodeId.NamespaceUri))
            {
                nodeId = new ExpandedNodeId(nodeId.Identifier, 0, Namespaces.OpcUa, 0);
            }
            return new UriOrFragment(nodeId.AsString(context, NamespaceFormat.Uri)!);
        }
    }
}
