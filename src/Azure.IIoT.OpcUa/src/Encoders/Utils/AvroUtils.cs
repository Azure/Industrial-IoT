// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Utils
{
    using global::Avro;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper functions for avro
    /// </summary>
    internal static partial class AvroUtils
    {
        /// <summary>
        /// Namespace zero
        /// </summary>
        public const string NamespaceZeroName = "org.opcfoundation.ua";

        /// <summary>
        /// Null schema
        /// </summary>
        public static Schema Null { get; } = PrimitiveSchema.NewInstance("null");

        /// <summary>
        /// Safely Convert a uri to a namespace
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static string NamespaceUriToNamespace(string ns)
        {
            if (!Uri.TryCreate(ns, new UriCreationOptions
            {
                DangerousDisablePathAndQueryCanonicalization = false
            }, out var result))
            {
                return ns.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Escape)
                    .Aggregate((a, b) => $"{a}.{b}");
            }
            else
            {
                return result.Host.Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .Reverse()
                    .Where(c => c != "www")
                    .Concat(result.AbsolutePath.Split('/',
                        StringSplitOptions.RemoveEmptyEntries))
                    .Select(Escape)
                    .Aggregate((a, b) => $"{a}.{b}");
            }
        }

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
            return schema == AvroUtils.Null ? schema :
                UnionSchema.Create(new List<Schema>
                {
                    AvroUtils.Null,
                    schema
                });
        }

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
                ns.SchemaName.Namespace == AvroUtils.NamespaceZeroName &&
                Enum.TryParse<BuiltInType>(ns.Name, out builtInType))
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
                TryFindNamespace(context.NamespaceUris, ns.Namespace,
                    out var namespaceIndex, out var namespaceUri))
            {
                return new ExpandedNodeId(ns.Name,
                    (ushort)namespaceIndex, namespaceUri, 0);
            }
            return type.ToExpandedNodeId(context);
        }

        /// <summary>
        /// Find index in namespace table
        /// </summary>
        /// <param name="namespaces"></param>
        /// <param name="avroNamespace"></param>
        /// <param name="index"></param>
        /// <param name="namespaceUri"></param>
        /// <returns></returns>
        public static bool TryFindNamespace(this NamespaceTable namespaces,
            string avroNamespace, out uint index, out string? namespaceUri)
        {
            if (avroNamespace == AvroUtils.NamespaceZeroName)
            {
                namespaceUri = Opc.Ua.Namespaces.OpcUa;
                index = 0;
                return true;
            }
            for (var i = 1u; i < namespaces.Count; i++)
            {
                namespaceUri = namespaces.GetString(i);
                var converted = AvroUtils.NamespaceUriToNamespace(namespaceUri);
                if (converted == avroNamespace)
                {
                    index = i;
                    return true;
                }
            }
            index = 0;
            namespaceUri = null;
            return false;
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
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Escape(string name)
        {
            return Escape(name, false);
        }

        /// <summary>
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static string Escape(string name, bool remove)
        {
            return EscapeAvroRegex().Replace(name.Replace('/', '_'),
                match => remove ? string.Empty : $"__{(int)match.Value[0]}");
        }

        [GeneratedRegex("[^a-zA-Z0-9_]")]
        private static partial Regex EscapeAvroRegex();
    }
}
