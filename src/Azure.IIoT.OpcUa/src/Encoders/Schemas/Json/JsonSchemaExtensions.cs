// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Extensions
    /// </summary>
    internal static class JsonSchemaExtensions
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
            var name = ToFragment(id);

            if (!definitions.ContainsKey(name))
            {
                definitions.Add(name, schema(id));
            }
            return new JsonSchema
            {
                Reference = new UriOrFragment(name)
            };
        }

        /// <summary>
        /// Convert to fragment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ToFragment(this UriOrFragment id)
        {
            if (id.Namespace == null)
            {
                return id.Fragment;
            }
            return SchemaUtils.NamespaceUriToNamespace(id.Namespace)
                + "." + SchemaUtils.Escape(id.Fragment);
        }

        /// <summary>
        /// Make union type from schemas
        /// </summary>
        /// <param name="schemas"></param>
        /// <param name="definitions"></param>
        /// <param name="title"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JsonSchema AsUnion(this IEnumerable<JsonSchema> schemas,
            Dictionary<string, JsonSchema> definitions, string? title = null,
            UriOrFragment? id = null)
        {
            var s = schemas.ToList();
            if (s.Count == 0)
            {
                throw new ArgumentException("Union must have at least one schema",
                    nameof(schemas));
            }
            if (id == null)
            {
                return Create(definitions, title, s, null);
            }
            return definitions.Reference(id, id => Create(definitions, title, s, id));

            static JsonSchema Create(Dictionary<string, JsonSchema> definitions,
                string? title, List<JsonSchema> s, UriOrFragment? id)
            {
                return new JsonSchema
                {
                    Id = id,
                    Title = title,
                    Types = s
                        .Select(s => Resolve(s, definitions).Type)
                        .Distinct()
                        .ToArray(),
                    OneOf = s
                };
            }
        }

        /// <summary>
        /// Make array from schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static JsonSchema AsArray(this JsonSchema schema, string? title = null)
        {
            return new JsonSchema
            {
                Title = title,
                Type = SchemaType.Array,
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
                Debug.Assert(schema.Type != SchemaType.None);
                return schema;
            }
            if (schema.Reference.Namespace == null)
            {
                return definitions[schema.Reference.Fragment];
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
            return JsonSchemaWriter.SerializeAsString(schema, indented);
        }

        /// <summary>
        /// Get namespace
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string GetNamespaceUri(this SchemaOptions options)
        {
            return options.Namespace ?? SchemaUtils.PublisherNamespaceUri;
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
            return new UriOrFragment(fragment, options.GetNamespaceUri());
        }

        /// <summary>
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this NodeId nodeId,
            IServiceMessageContext context)
        {
            return nodeId.AsString(context, NamespaceFormat.Uri).GetSchemaId(context);
        }

        /// <summary>
        /// Create identifier of a schema
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this string? nodeId,
            IServiceMessageContext context)
        {
            var (ns, n) = SchemaUtils.SplitNodeId(nodeId, context, false);
            return new UriOrFragment(n, ns);
        }

        /// <summary>
        /// Get schema id from a node with a display name as name
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UriOrFragment GetSchemaId(this string? nodeId, string name,
            IServiceMessageContext context)
        {
            var qn = name.ToQualifiedName(context);
            var ns = qn.NamespaceIndex != 0 ?
                context.NamespaceUris.GetString(qn.NamespaceIndex) :
                SchemaUtils.SplitNodeId(nodeId, context, false).Namespace;
            return new UriOrFragment(qn.Name, ns);
        }
    }
}
