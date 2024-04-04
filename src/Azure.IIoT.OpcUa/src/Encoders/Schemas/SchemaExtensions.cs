// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Json.Schema;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
            if (!definitions.ContainsKey(id.ToString()))
            {
                definitions.Add(id.ToString(), schema(id));
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
        /// <returns></returns>
        public static JsonSchema AsUnion(this IReadOnlyList<JsonSchema> schemas)
        {
            return new JsonSchema
            {
                Type = schemas.Select(s => s.SafeGetType()).Distinct().ToArray(),
                OneOf = schemas.ToList()
            };
        }

        /// <summary>
        /// Make array from schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JsonSchema AsArray(this JsonSchema schema)
        {
            return new JsonSchema
            {
                Type = new[] { SchemaType.Array },
                Items = new Items(schema)
            };
        }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indented"></param>
        /// <returns></returns>
        public static string ToJsonString(this JsonSchema schema, bool indented = false)
        {
            using var writer = new StringWriter();
            SchemaWriter.WriteSchema(writer, schema, indented
                ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
            return writer.ToString();
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
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this NodeId nodeId,
            ServiceMessageContext context)
        {
            return new UriOrFragment(nodeId.AsString(context, NamespaceFormat.Uri));
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
            return new UriOrFragment(nodeId.AsString(context, NamespaceFormat.Uri));
        }
    }
}
