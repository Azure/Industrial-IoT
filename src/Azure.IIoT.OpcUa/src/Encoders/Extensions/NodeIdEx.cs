// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Node id extensions
    /// </summary>
    public static class NodeIdEx
    {
        /// <summary>
        /// Creates an expanded node id from node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ExpandedNodeId ToExpandedNodeId(this NodeId nodeId,
            NamespaceTable? namespaces)
        {
            if (NodeId.IsNull(nodeId))
            {
                return ExpandedNodeId.Null;
            }
            string? ns = null;
            if (nodeId.NamespaceIndex > 0)
            {
                ArgumentNullException.ThrowIfNull(namespaces);
                ns = namespaces.GetString(nodeId.NamespaceIndex);
            }
            return new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex, ns, 0);
        }

        /// <summary>
        /// Creates an expanded node id from node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="serverIndex"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ExpandedNodeId ToExpandedNodeId(this NodeId nodeId,
            uint serverIndex, NamespaceTable namespaces)
        {
            if (NodeId.IsNull(nodeId))
            {
                return ExpandedNodeId.Null;
            }
            if (nodeId.NamespaceIndex > 0 && namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }
            return new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex,
                nodeId.NamespaceIndex > 0 ? namespaces.GetString(nodeId.NamespaceIndex) :
                    null, serverIndex);
        }

        /// <summary>
        /// Convert an expanded node id to a node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaces"></param>
        /// <param name="allowUnknownNamespace"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static NodeId ToNodeId(this ExpandedNodeId? nodeId, NamespaceTable namespaces,
            bool allowUnknownNamespace = false)
        {
            if (nodeId?.IsNull != false)
            {
                return NodeId.Null;
            }
            if (nodeId.NamespaceIndex > 0 && namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }
            int index = nodeId.NamespaceIndex;
            if (!string.IsNullOrEmpty(nodeId.NamespaceUri))
            {
                index = namespaces.GetIndex(nodeId.NamespaceUri);
                if (index < 0)
                {
                    if (!allowUnknownNamespace)
                    {
                        throw new ArgumentException(
                            $"Namespace '{nodeId.NamespaceUri}' was not found in NamespaceTable.",
                            nameof(nodeId));
                    }
                    index = 0;
                }
            }
            return new NodeId(nodeId.Identifier, (ushort)index);
        }

        /// <summary>
        /// Returns a uri that identifies the node id uniquely.  If the server
        /// uri information is provided, and the it contains a server name at
        /// index 0, the node id will be formatted as an expanded node id uri
        /// (see below).  Otherwise, the  resource is the namespace and not the
        /// server.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        public static string? AsString(this NodeId nodeId, IServiceMessageContext context,
            NamespaceFormat namespaceFormat)
        {
            if (NodeId.IsNull(nodeId))
            {
                return null;
            }
            return nodeId
                .ToExpandedNodeId(context.NamespaceUris)
                .AsString(context, namespaceFormat);
        }

        /// <summary>
        /// Returns a node uri from an expanded node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        public static string? AsString(this ExpandedNodeId nodeId, IServiceMessageContext context,
            NamespaceFormat namespaceFormat)
        {
            if (NodeId.IsNull(nodeId))
            {
                return null;
            }

            var nsUri = nodeId.NamespaceUri;
            if (string.IsNullOrEmpty(nsUri) && nodeId.NamespaceIndex != 0)
            {
                nsUri = context.NamespaceUris.GetString(nodeId.NamespaceIndex);
                if (string.IsNullOrEmpty(nsUri))
                {
                    nsUri = null;
                }
            }
            string? srvUri = null;
            if (nodeId.ServerIndex != 0 && context.ServerUris != null)
            {
                srvUri = context.ServerUris.GetString(nodeId.ServerIndex);
                if (string.IsNullOrEmpty(srvUri))
                {
                    srvUri = null;
                }
            }
            switch (namespaceFormat)
            {
                default:
                    if (nsUri != null && !Uri.IsWellFormedUriString(nsUri, UriKind.Absolute))
                    {
                        // Fall back to nsu= format - but strip indexes
                        return FormatNodeIdExpanded(nsUri, 0u, nodeId.IdType, nodeId.Identifier);
                    }
                    return FormatNodeIdUri(nsUri, srvUri, nodeId.IdType, nodeId.Identifier);
                case NamespaceFormat.Expanded:
                    return FormatNodeIdExpanded(nsUri, nodeId.ServerIndex, nodeId.IdType,
                        nodeId.Identifier);
                case NamespaceFormat.Index:
                    return new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex, null,
                        nodeId.ServerIndex).ToString();
            }
        }

        /// <summary>
        /// Returns a node from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NodeId ToNodeId(this string? value, IServiceMessageContext context)
        {
            if (value == null)
            {
                return NodeId.Null;
            }
            var parts = value.Split(';');
            if (parts.Any(s => s.StartsWith("ns=", StringComparison.CurrentCulture)))
            {
                return NodeId.Parse(value);
            }
            if (parts.Any(s => s.StartsWith("nsu=", StringComparison.CurrentCulture)))
            {
                return ExpandedNodeId.Parse(value).ToNodeId(context.NamespaceUris);
            }
            var identifier = ParseNodeIdUri(value, out var nsUri, out var srvUri);
            return new NodeId(identifier, context.NamespaceUris.GetIndexOrAppend(nsUri));
        }

        /// <summary>
        /// Returns an expanded node id from a node uri.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ExpandedNodeId ToExpandedNodeId(this string? value, IServiceMessageContext context)
        {
            if (value == null)
            {
                return ExpandedNodeId.Null;
            }
            var parts = value.Split(';');
            if (parts.Any(s =>
                s.StartsWith("ns=", StringComparison.CurrentCulture) ||
                s.StartsWith("nsu=", StringComparison.CurrentCulture)))
            {
                return ExpandedNodeId.Parse(value);
            }
            var identifier = ParseNodeIdUri(value, out var nsUri, out var srvUri);

            // Allocate entry in context if does not exist
            var nsIndex = context.NamespaceUris.GetIndexOrAppend(nsUri);
            if (!string.IsNullOrEmpty(srvUri))
            {
                return new ExpandedNodeId(identifier, 0, nsUri == Namespaces.OpcUa ? null : nsUri,
                    context.ServerUris.GetIndexOrAppend(srvUri));
            }
            return new ExpandedNodeId(identifier, 0, nsUri == Namespaces.OpcUa ? null : nsUri, 0);
        }

        /// <summary>
        /// Format node id components into a uri string
        /// </summary>
        /// <param name="nsUri"></param>
        /// <param name="srvUri"></param>
        /// <param name="idType"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static string FormatNodeIdUri(string? nsUri, string? srvUri,
            IdType idType, object? identifier)
        {
            var buffer = new StringBuilder();
            if (nsUri != null)
            {
                // Append node id as fragment
                buffer = buffer.Append(nsUri)
                    .Append('#');
            }
            switch (idType)
            {
                case IdType.Numeric:
                    if (srvUri == null && nsUri == null &&
                        TryGetDataTypeName(identifier, out var typeName))
                    {
                        // For readability use data type name here if possible
                        return typeName;
                    }
                    buffer = buffer.Append("i=");
                    if (identifier == null)
                    {
                        buffer.Append('0'); // null
                        break;
                    }
                    buffer = buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", identifier);
                    break;
                case IdType.String:
                    buffer = buffer.Append("s=");
                    if (identifier == null)
                    {
                        break; // null
                    }
                    buffer = buffer.Append(identifier.ToString()?.UrlEncode());
                    break;
                case IdType.Guid:
                    buffer = buffer.Append("g=");
                    if (identifier == null)
                    {
                        buffer.Append(Guid.Empty); // null
                        break;
                    }
                    buffer = buffer.Append(((Guid)identifier).ToString("D").UrlEncode());
                    break;
                case IdType.Opaque:
                    buffer = buffer.Append("b=");
                    if (identifier == null)
                    {
                        break; // null
                    }
                    buffer = buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", ((byte[])identifier).ToBase64String().UrlEncode());
                    break;
                default:
                    throw new FormatException($"Node id type {idType} is unknown!");
            }
            if (srvUri != null)
            {
                // Pack server in front of identifier
                buffer = buffer.Append("&srv=")
                    .Append(srvUri);
            }
            return buffer.ToString();
        }

        private static string FormatNodeIdExpanded(string? nsUri, uint serverIndex,
            IdType idType, object? identifier)
        {
            var buffer = new StringBuilder();
            if (serverIndex != 0)
            {
                buffer = buffer
                    .Append("svr=")
                    .Append(serverIndex)
                    .Append(';');
            }
            if (!string.IsNullOrEmpty(nsUri))
            {
                buffer = buffer
                    .Append("nsu=")
                    .Append(nsUri.Replace(";", "%3b", StringComparison.Ordinal))
                    .Append(';');
            }
            switch (idType)
            {
                case IdType.Numeric:
                    buffer = buffer.Append("i=")
                        .Append(identifier == null ? 0 :
                        (uint)identifier);
                    break;
                case IdType.Guid:
                    buffer = buffer.Append("g=")
                        .Append(identifier == null ? Guid.Empty :
                        (Guid)identifier);
                    break;
                case IdType.Opaque:
                    buffer = buffer.Append("b=")
                        .Append(identifier == null ? string.Empty :
                        Convert.ToBase64String((byte[])identifier));
                    break;
                default:
                    buffer = buffer.Append("s=")
                        .Append(identifier == null ? string.Empty :
                        identifier.ToString());
                    break;
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Parses a node uri and returns components.  The format of the uri is
        /// <para>namespaceuri(?srv=serverurn)#idtype_idasstring</para>.  Avoid url
        /// encoding due to the problem of storage encoding again.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="nsUri"></param>
        /// <param name="srvUri"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static object? ParseNodeIdUri(string value, out string nsUri, out string? srvUri)
        {
            // Get resource uri
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                // Not a absolute uri, try to mitigate a potentially nonstandard namespace string
                const string sepPattern = @"(.+)#([isgb]{1}\=.*)";
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
                var match = Regex.Match(value, sepPattern);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
                if (match.Success)
                {
                    nsUri = match.Groups[1].Value;
                    value = match.Groups[2].Value;
                }
                else
                {
                    nsUri = Namespaces.OpcUa;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(uri.Fragment))
                {
                    throw new FormatException("Bad fragment - should contain identifier.");
                }

                var idStart = value.IndexOf('#', StringComparison.Ordinal);

                nsUri = idStart >= 0 ? value[..idStart] : uri.NoQueryAndFragment().AbsoluteUri;
                value = uri.Fragment.TrimStart('#');
            }

            var and = value?.IndexOf('&', StringComparison.Ordinal) ?? -1;
            if (and != -1)
            {
                var remainder = value![and..];
                // See if the query contains the server identfier
                if (remainder.StartsWith("&srv=", StringComparison.Ordinal))
                {
                    // The uri denotes an id in a namespace on a server
                    srvUri = remainder[5..];
                }
                else
                {
                    throw new FormatException($"{value} does not contain ?srv=");
                }
                return ParseIdentifier(value[..and]);
            }
            srvUri = null;
            return ParseIdentifier(value);
        }

        /// <summary>
        /// Parse identfier from string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static object? ParseIdentifier(string? text)
        {
            if (text == null)
            {
                return null;
            }
            if (text.Length > 1 && text[1] is '=' or '_')
            {
                try
                {
                    return ParseIdentifier(text[0], text[2..]);
                }
                catch (FormatException)
                {
                }
            }
            // Try to retrieve data type identifier from text
            if (TryGetDataTypeId(text, out var id))
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Parse identfier from string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static object ParseIdentifier(char type, string text)
        {
            switch (type)
            {
                case 'i':
                    try
                    {
                        return Convert.ToUInt32(text.UrlDecode(),
                            CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return Convert.ToUInt32(text,
                            CultureInfo.InvariantCulture);
                    }
                case 'b':
                    try
                    {
                        return Convert.FromBase64String(text.UrlDecode());
                    }
                    catch
                    {
                        return Convert.FromBase64String(text);
                    }
                case 'g':
                    if (!Guid.TryParse(text.UrlDecode(), out var guid))
                    {
                        return Guid.Parse(text);
                    }
                    return guid;
                case 's':
                    return text.UrlDecode();
            }
            throw new FormatException($"{type} is not a known node id type");
        }

        /// <summary>
        /// Returns a data type id for the name
        /// </summary>
        /// <param name="text"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static bool TryGetDataTypeId(string text, out uint id)
        {
            if (Enum.TryParse<BuiltInType>(text, true, out var type))
            {
                id = (uint)type;
                return true;
            }
            if (TypeMaps.DataTypes.Value.TryGetIdentifier(text, out id))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns data type name for identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool TryGetDataTypeName(object? identifier, [NotNullWhen(true)] out string? name)
        {
            name = null;
            try
            {
                if (identifier is not uint uid)
                {
                    return false;
                }
                if (uid <= int.MaxValue)
                {
                    var id = (int)uid;
                    if (Enum.IsDefined(typeof(BuiltInType), id))
                    {
                        name = Enum.GetName(typeof(BuiltInType), id);
                        if (StringComparer.OrdinalIgnoreCase.Equals(name, nameof(BuiltInType.Null)))
                        {
                            name = null;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(name))
                {
                    return true;
                }
                if (TypeMaps.DataTypes.Value.TryGetBrowseName(uid, out name) &&
                    StringComparer.OrdinalIgnoreCase.Equals(name, nameof(BuiltInType.Null)))
                {
                    name = null;
                }
                return !string.IsNullOrEmpty(name);
            }
            catch
            {
                return false;
            }
        }
    }
}
