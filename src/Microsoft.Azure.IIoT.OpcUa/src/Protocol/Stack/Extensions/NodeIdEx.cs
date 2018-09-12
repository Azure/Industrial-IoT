// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using Opc.Ua;

    /// <summary>
    /// Node id extensions
    /// </summary>
    public static class NodeIdEx {

        /// <summary>
        /// Creates an expanded node id from node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public static ExpandedNodeId ToExpandedNodeId(this NodeId nodeId,
            NamespaceTable namespaces) {
            if (NodeId.IsNull(nodeId)) {
                return ExpandedNodeId.Null;
            }
            if (nodeId.NamespaceIndex > 0 && namespaces == null) {
                throw new ArgumentNullException(nameof(namespaces));
            }
            return new ExpandedNodeId(nodeId.Identifier, nodeId.NamespaceIndex,
                nodeId.NamespaceIndex > 0 ? namespaces.GetString(nodeId.NamespaceIndex) :
                    null, 0);
        }

        /// <summary>
        /// Creates an expanded node id from node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="serverIndex"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public static ExpandedNodeId ToExpandedNodeId(this NodeId nodeId,
            uint serverIndex, NamespaceTable namespaces) {
            if (NodeId.IsNull(nodeId)) {
                return ExpandedNodeId.Null;
            }
            if (nodeId.NamespaceIndex > 0 && namespaces == null) {
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
        /// <returns></returns>
        public static NodeId ToNodeId(this ExpandedNodeId nodeId,
            NamespaceTable namespaces) {
            if (NodeId.IsNull(nodeId)) {
                return NodeId.Null;
            }
            if (nodeId.NamespaceIndex > 0 && namespaces == null) {
                throw new ArgumentNullException(nameof(namespaces));
            }
            int index = nodeId.NamespaceIndex;
            if (!string.IsNullOrEmpty(nodeId.NamespaceUri)) {
                index = namespaces.GetIndex(nodeId.NamespaceUri);
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
        /// <returns></returns>
        public static string AsString(this NodeId nodeId, ServiceMessageContext context) {
            if (nodeId == null) {
                nodeId = NodeId.Null;
            }
            return nodeId.ToExpandedNodeId(context?.NamespaceUris).AsString(context);
        }

        /// <summary>
        /// Returns a uri that identifies the qualified name uniquely.
        /// </summary>
        /// <param name="qn"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string AsString(this QualifiedName qn, ServiceMessageContext context) {
            if (qn == null) {
                return string.Empty;
            }
            var buffer = new StringBuilder();
            if (qn.NamespaceIndex != 0) {
                var nsUri = context.NamespaceUris.GetString(qn.NamespaceIndex);
                if (!string.IsNullOrEmpty(nsUri)) {
                    buffer.Append(nsUri);
                    // Append node id as fragment
                    buffer.Append("#");
                }
            }
            buffer.Append(qn.Name ?? string.Empty);
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a node uri from an expanded node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string AsString(this ExpandedNodeId nodeId, ServiceMessageContext context) {
            if (nodeId == null) {
                nodeId = ExpandedNodeId.Null;
            }
            var nsUri = nodeId.NamespaceUri;
            if (string.IsNullOrEmpty(nsUri) && nodeId.NamespaceIndex != 0) {
                nsUri = context.NamespaceUris.GetString(nodeId.NamespaceIndex);
                if (string.IsNullOrEmpty(nsUri)) {
                    nsUri = null;
                }
            }
            string srvUri = null;
            if (nodeId.ServerIndex != 0 && context.ServerUris != null) {
                srvUri = context.ServerUris.GetString(nodeId.ServerIndex);
                if (string.IsNullOrEmpty(srvUri)) {
                    srvUri = null;
                }
            }
            return FormatNodeIdUri(nsUri, srvUri, nodeId.IdType, nodeId.Identifier);
        }

        /// <summary>
        /// Returns a qualified name from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static QualifiedName ToQualifiedName(this string value, ServiceMessageContext context) {
            if (value == null) {
                return QualifiedName.Null;
            }
            try {
                var uri = new Uri(value);
                if (string.IsNullOrEmpty(uri.Fragment)) {
                    value = string.Empty;
                }
                else {
                    value = uri.Fragment.TrimStart('#');
                }
                var nsUri = uri.NoQueryAndFragment().AbsoluteUri;
                return new QualifiedName(value, context.NamespaceUris.GetIndexOrAppend(nsUri));
            }
            catch {
                try {
                    return QualifiedName.Parse(value);
                }
                catch {
                    // Give up
                    return new QualifiedName(value);
                }
            }
        }

        /// <summary>
        /// Returns a node from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NodeId ToNodeId(this string value, ServiceMessageContext context) {
            if (value == null) {
                return NodeId.Null;
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
        public static ExpandedNodeId ToExpandedNodeId(this string value, ServiceMessageContext context) {
            if (value == null) {
                return ExpandedNodeId.Null;
            }
            var identifier = ParseNodeIdUri(value, out var nsUri, out var srvUri);
            if (!string.IsNullOrEmpty(srvUri)) {
                // References a node id on a server
                if (nsUri == Namespaces.OpcUa) {
                    return new ExpandedNodeId(identifier, 0, null,
                        context.ServerUris.GetIndexOrAppend(srvUri));
                }
                return new ExpandedNodeId(identifier, context.NamespaceUris.GetIndexOrAppend(nsUri),
                    nsUri, context.ServerUris.GetIndexOrAppend(srvUri));
            }
            if (nsUri == Namespaces.OpcUa) {
                return new ExpandedNodeId(identifier, 0, null, 0);
            }
            return new ExpandedNodeId(identifier, context.NamespaceUris.GetIndexOrAppend(nsUri), nsUri, 0);
        }

        /// <summary>
        /// Format node id components into a uri string
        /// </summary>
        /// <param name="nsUri"></param>
        /// <param name="srvUri"></param>
        /// <param name="idType"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static string FormatNodeIdUri(string nsUri, string srvUri,
            IdType idType, object identifier) {
            var buffer = new StringBuilder();
            if (nsUri != null) {
                buffer.Append(nsUri);
                // Append node id as fragment
                buffer.Append("#");
            }
            switch (idType) {
                case IdType.Numeric:
                    if (srvUri == null && nsUri == null &&
                        GetDataTypeName(identifier, out var typeName)) {
                        // For readability use data type name here if possible
                        return typeName;
                    }
                    buffer.Append("i=");
                    if (identifier == null) {
                        buffer.Append("0"); // null
                        break;
                    }
                    buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", identifier);
                    break;
                case IdType.String:
                    buffer.Append("s=");
                    if (identifier == null) {
                        break; // null
                    }
                    buffer.Append(identifier.ToString().UrlEncode());
                    break;
                case IdType.Guid:
                    buffer.Append("g=");
                    if (identifier == null) {
                        buffer.Append(Guid.Empty); // null
                        break;
                    }
                    buffer.Append(((Guid)identifier).ToString("D"));
                    break;
                case IdType.Opaque:
                    buffer.Append("b=");
                    if (identifier == null) {
                        break; // null
                    }
                    buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", Convert.ToBase64String((byte[])identifier));
                    break;
                default:
                    throw new FormatException($"Nod id type {idType} is unknown!");
            }
            if (srvUri != null) {
                // Pack server in front of identifier
                buffer.Append("&srv=");
                // srvUri = Uri.EscapeDataString(srvUri);
                buffer.Append(srvUri);
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
        private static object ParseNodeIdUri(string value, out string nsUri, out string srvUri) {
            // Get resource uri
            try {
                var uri = new Uri(value);
                if (string.IsNullOrEmpty(uri.Fragment)) {
                    throw new FormatException($"Bad fragment - should contain identifier.");
                }
                nsUri = uri.NoQueryAndFragment().AbsoluteUri;
                value = uri.Fragment.TrimStart('#');
            }
            catch {
                // Not a uri
                nsUri = Namespaces.OpcUa;
            }

            var and = value?.IndexOf('&') ?? -1;
            if (and != -1) {
                var remainder = value.Substring(and);
                // See if the query contains the server identfier
                if (remainder.StartsWith("&srv=", StringComparison.Ordinal)) {
                    // The uri denotes an id in a namespace on a server
                    srvUri = remainder.Substring(5);
                }
                else {
                    throw new FormatException($"{value} does not contain ?srv=");
                }
                return ParseIdentifier(value.Substring(0, and));
            }
            srvUri = null;
            return ParseIdentifier(value);
        }

        /// <summary>
        /// Parse identfier from string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static object ParseIdentifier(string text) {
            if (text != null && text.Length > 1) {
                if (text[1] == '=' ||
                    text[1] == '_') {
                    try {
                        return ParseIdentifier(text[0], text.Substring(2));
                    }
                    catch (FormatException) {
                    }
                }
            }
            // Try to retrieve data type identifier from text - returns 0 if not a type.
            return DataTypes.GetIdentifier(text);
        }

        /// <summary>
        /// Parse identfier from string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object ParseIdentifier(char type, string text) {
            switch (type) {
                case 'i':
                    return Convert.ToUInt32(text, CultureInfo.InvariantCulture);
                case 'b':
                    return Convert.FromBase64String(text);
                case 'g':
                    return Guid.Parse(text);
                case 's':
                    return text.UrlDecode();
            }
            throw new FormatException($"{type} is not a known node id type");
        }

        /// <summary>
        /// Returns data type name for identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool GetDataTypeName(object identifier, out string name) {
            try {
                var id = Convert.ToInt32(identifier);
                name = DataTypes.GetBrowseName(id);
                return !string.IsNullOrEmpty(name);
            }
            catch {
                name = null;
                return false;
            }
        }
    }
}
