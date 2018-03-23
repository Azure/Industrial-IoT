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

    public static class OpcUaEx {

        /// <summary>
        /// Creates an expanded node id from node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public static ExpandedNodeId ToExpandedNodeId(this NodeId nodeId,
            NamespaceTable namespaces) {
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
        public static string AsString(this NodeId nodeId, ServiceMessageContext context) =>
            nodeId.ToExpandedNodeId(context.NamespaceUris).AsString(context);

        /// <summary>
        /// Returns a node uri from an expanded node id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string AsString(this ExpandedNodeId nodeId, ServiceMessageContext context) {
            if (NodeId.IsNull(nodeId)) {
                return null;
            }
            var buffer = new StringBuilder();
            var nsUri = nodeId.NamespaceUri;
            if (string.IsNullOrEmpty(nsUri) && nodeId.NamespaceIndex != 0) {
                nsUri = context.NamespaceUris.GetString(nodeId.NamespaceIndex);
                if (string.IsNullOrEmpty(nsUri)) {
                    nsUri = null;
                }
            }
            if (nsUri != null) {
                buffer.Append(nsUri);
                // Append node id as fragment
                buffer.Append("#");
            }
            FormatIdentifier(buffer, nodeId.IdType, nodeId.Identifier);
            // Append server as optional query param
            if (nodeId.ServerIndex != 0 && context.ServerUris != null) {
                var srvUri = context.ServerUris.GetString(nodeId.ServerIndex);
                if (!string.IsNullOrEmpty(srvUri)) {
                    // Pack server in front of identifier
                    buffer.Append("&srv=");
                    // srvUri = Uri.EscapeDataString(srvUri);
                    buffer.Append(srvUri);
                }
            }
            return buffer.ToString();
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
        /// Returns datatype for system type
        /// </summary>
        public static uint GetDataType(Type systemType) {
            while (systemType.IsArray) {
                systemType = systemType.GetElementType();
            }
            if (systemType.IsEnum) {
                return DataTypes.Enumeration;
            }
            if (systemType == typeof(bool)) {
                return DataTypes.Boolean;
            }
            if (systemType == typeof(sbyte)) {
                return DataTypes.SByte;
            }
            if (systemType == typeof(byte)) {
                return DataTypes.Byte;
            }
            if (systemType == typeof(short)) {
                return DataTypes.Int16;
            }
            if (systemType == typeof(ushort)) {
                return DataTypes.UInt16;
            }
            if (systemType == typeof(int)) {
                return DataTypes.Int32;
            }
            if (systemType == typeof(uint)) {
                return DataTypes.UInt32;
            }
            if (systemType == typeof(long)) {
                return DataTypes.Int64;
            }
            if (systemType == typeof(ulong)) {
                return DataTypes.UInt64;
            }
            if (systemType == typeof(float)) {
                return DataTypes.Float;
            }
            if (systemType == typeof(double)) {
                return DataTypes.Double;
            }
            if (systemType == typeof(string)) {
                return DataTypes.String;
            }
            if (systemType == typeof(DateTime)) {
                return DataTypes.DateTime;
            }
            if (systemType == typeof(Uuid)) {
                return DataTypes.Guid;
            }
            if (systemType == typeof(byte[])) {
                return DataTypes.ByteString;
            }
            if (systemType == typeof(XmlElement)) {
                return DataTypes.XmlElement;
            }
            if (systemType == typeof(NodeId)) {
                return DataTypes.NodeId;
            }
            if (systemType == typeof(ExpandedNodeId)) {
                return DataTypes.ExpandedNodeId;
            }
            if (systemType == typeof(StatusCode)) {
                return DataTypes.StatusCode;
            }
            if (systemType == typeof(DiagnosticInfo)) {
                return DataTypes.DiagnosticInfo;
            }
            if (systemType == typeof(QualifiedName)) {
                return DataTypes.QualifiedName;
            }
            if (systemType == typeof(LocalizedText)) {
                return DataTypes.LocalizedText;
            }
            if (systemType == typeof(DataValue)) {
                return DataTypes.DataValue;
            }
            if (systemType == typeof(ExtensionObject)) {
                return DataTypes.Structure;
            }
            if (systemType == typeof(DateTime)) {
                return DataTypes.UtcTime;
            }
            return DataTypes.BaseDataType;
        }

        /// <summary>
        /// Unpack with a default value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this DataValue dataValue, T defaultValue = default(T)) {
            if (dataValue == null) {
                return defaultValue;
            }
            if (StatusCode.IsNotGood(dataValue.StatusCode)) {
                return defaultValue;
            }
            var value = dataValue.Value;
            while (typeof(T).IsEnum) {
                try {
                    return (T)Enum.ToObject(typeof(T), value);
                }
                catch {
                    break;
                }
            }
            while (!typeof(T).IsInstanceOfType(value) && value is IConvertible convertible) {
                try {
                    return (T)Convert.ChangeType(convertible, typeof(T));
                }
                catch {
                    break;
                }
            }
            try {
                return (T)value;
            }
            catch {
                return defaultValue;
            }
        }

        /// <summary>
        /// Unpack with a default value
        /// </summary>
        /// <param name="dataValue"></param>
        /// <param name="defaultValue"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Get(this DataValue dataValue, object defaultValue, Type type) {
            if (dataValue == null) {
                return defaultValue;
            }
            if (StatusCode.IsNotGood(dataValue.StatusCode)) {
                return defaultValue;
            }
            var value = dataValue.Value;
            while (type.IsEnum) {
                try {
                    return Enum.ToObject(type, value);
                }
                catch {
                    break;
                }
            }
            while (!type.IsInstanceOfType(value) && value is IConvertible convertible) {
                try {
                    return Convert.ChangeType(convertible, type);
                }
                catch {
                    break;
                }
            }
            // TODO: try cast function...
            return defaultValue;
        }


        /// <summary>
        /// Format an identifier into a string buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="type"></param>
        /// <param name="identifier"></param>
        private static void FormatIdentifier(StringBuilder buffer, IdType type, object identifier) {
            switch (type) {
                case IdType.Numeric:
                    buffer.Append("i=");
                    if (identifier == null) {
                        buffer.Append("0");
                        break;
                    }
                    buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", identifier);
                    break;
                case IdType.String:
                    buffer.Append("s=");
                    buffer.Append(identifier.ToString().UrlEncode());
                    break;
                case IdType.Guid:
                    buffer.Append("g=");
                    if (identifier == null) {
                        buffer.Append(Guid.Empty);
                        break;
                    }
                    buffer.Append(((Guid)identifier).ToString("D"));
                    break;
                case IdType.Opaque:
                    buffer.Append("b=");
                    if (identifier == null) {
                        break;
                    }
                    buffer.AppendFormat(CultureInfo.InvariantCulture,
                        "{0}", Convert.ToBase64String((byte[])identifier));
                    break;
                default:
                    throw new FormatException($"Id type {type} is unknown!");
            }
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

            var and = value.IndexOf('&');
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
            if (text.Length > 1) {
                if (text[1] == '=' ||
                    text[1] == '_') {
                    try {
                        switch (text[0]) {
                            case 'i':
                                return Convert.ToUInt32(
                                    text.Substring(2), CultureInfo.InvariantCulture);
                            case 'b':
                                return Convert.FromBase64String(
                                    text.Substring(2));
                            case 'g':
                                return Guid.Parse(
                                    text.Substring(2));
                            case 's':
                                return text.Substring(2);
                        }
                    }
                    catch(FormatException) { }
                }
            }
            return text;
        }
    }
}
