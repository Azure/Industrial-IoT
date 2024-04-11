// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Schema utils
    /// </summary>
    internal static partial class SchemaUtils
    {
        /// <summary>
        /// Namespace zero
        /// </summary>
        public const string NamespaceZeroName = "org.opcfoundation.UA";

        /// <summary>
        /// Publisher namespace
        /// </summary>
        public const string PublisherNamespace = "org.github.microsoft.opc.publisher";

        /// <summary>
        /// Publisher namespace uri
        /// </summary>
        public const string PublisherNamespaceUri = "http://github.org/microsoft/opcpublisher";

        /// <summary>
        /// Safely Convert a uri to a namespace
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static string NamespaceUriToNamespace(string ns)
        {
            if (Uri.TryCreate(ns, new UriCreationOptions
            {
                DangerousDisablePathAndQueryCanonicalization = false
            }, out var result))
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                return result.Host.Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .Reverse()
                    .Where(c => c != "www")
                    .Select(s => s.ToLowerInvariant())
                    .Concat(result.AbsolutePath.Split('/',
                        StringSplitOptions.RemoveEmptyEntries))
                    .Select(Escape)
                    .Aggregate((a, b) => $"{a}.{b}");
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
            else
            {
                return ns.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Escape)
                    .Aggregate((a, b) => $"{a}.{b}");
            }
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
            if (avroNamespace == NamespaceZeroName)
            {
                namespaceUri = Namespaces.OpcUa;
                index = 0;
                return true;
            }
            for (var i = 1u; i < namespaces.Count; i++)
            {
                namespaceUri = namespaces.GetString(i);
                var converted = NamespaceUriToNamespace(namespaceUri);
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
        /// Create namespace
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="escape"></param>
        /// <returns></returns>
        public static (string Namespace, string Id) SplitNodeId(string? nodeId,
            ServiceMessageContext context, bool escape)
        {
            var id = nodeId.ToExpandedNodeId(context);
            string ns;
            if (id.NamespaceIndex == 0 && id.NamespaceUri == null)
            {
                ns = escape ? NamespaceZeroName : Namespaces.OpcUa;
            }
            else
            {
                ns = id.NamespaceUri;
                if (escape)
                {
                    ns = NamespaceUriToNamespace(ns);
                }
            }
            var c = escape ? '_' : '=';
            var name = id.IdType switch
            {
                IdType.Opaque => "b",
                IdType.Guid => "g",
                IdType.String => "s",
                _ => "i"
            } + c + id.Identifier;
            if (escape)
            {
                name = Escape(name);
            }
            return (ns, name);
        }

        /// <summary>
        /// Create namespace
        /// </summary>
        /// <param name="qualifiedName"></param>
        /// <param name="context"></param>
        /// <param name="outerNamespace"></param>
        /// <returns></returns>
        public static string SplitQualifiedName(string qualifiedName,
            ServiceMessageContext context, string? outerNamespace = null)
        {
            var qn = qualifiedName.ToQualifiedName(context);
            string avroStyleNamespace;
            if (qn.NamespaceIndex == 0)
            {
                avroStyleNamespace = NamespaceZeroName;
            }
            else
            {
                var uri = context.NamespaceUris.GetString(qn.NamespaceIndex);
                avroStyleNamespace = NamespaceUriToNamespace(uri);
            }
            var name = Escape(qn.Name);
            if (!string.Equals(outerNamespace, avroStyleNamespace,
                StringComparison.OrdinalIgnoreCase))
            {
                // Qualify if the name is in a different namespace
                name = $"{avroStyleNamespace}.{name}";
            }
            return name;
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
