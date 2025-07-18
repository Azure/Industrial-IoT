// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Text;

    /// <summary>
    /// Qualified name extensions
    /// </summary>
    public static class QualifiedNameEx
    {
        /// <summary>
        /// Returns a uri that identifies the qualified name uniquely.
        /// </summary>
        /// <param name="qn"></param>
        /// <param name="context"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        public static string AsString(this QualifiedName qn, IServiceMessageContext context,
            NamespaceFormat namespaceFormat)
        {
            if (qn == null || qn == QualifiedName.Null)
            {
                return string.Empty;
            }

            var qnName = qn.Name ?? string.Empty;
            var buffer = new StringBuilder();
            if (namespaceFormat == NamespaceFormat.ExpandedWithNamespace0
                || qn.NamespaceIndex != 0
                || qnName.Contains(':', StringComparison.Ordinal))
            {
                switch (namespaceFormat)
                {
                    default:
                        var nsUri = context.NamespaceUris.GetString(qn.NamespaceIndex);
                        if (!string.IsNullOrEmpty(nsUri))
                        {
                            buffer.Append(nsUri);
                            // Append name as fragment
                            if (!string.IsNullOrEmpty(qnName))
                            {
                                buffer.Append('#');
                            }
                        }
                        break;
                    case NamespaceFormat.ExpandedWithNamespace0:
                    case NamespaceFormat.Expanded:
                        var nsUri2 = context.NamespaceUris.GetString(qn.NamespaceIndex);
                        if (!string.IsNullOrEmpty(nsUri2))
                        {
                            buffer.Append("nsu=").Append(nsUri2).Append(';');
                        }
                        break;
                    case NamespaceFormat.Index:
                        buffer.Append(qn.NamespaceIndex).Append(':');
                        break;
                }
            }
            buffer.Append(qnName.UrlEncode());
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a qualified name from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static QualifiedName ToQualifiedName(this string? value, IServiceMessageContext context)
        {
            if (string.IsNullOrEmpty(value))
            {
                return QualifiedName.Null;
            }
            string? nsUri = null;

            // Try to parse the index format
            var parts = value.Split(':');
            if (ushort.TryParse(parts[0], out var nsIndex))
            {
                value = value[(parts[0].Length + 1)..];
                return new QualifiedName(
                    string.IsNullOrEmpty(value) ? null : value.UrlDecode(),
                    nsIndex);
            }

            // Try to parse the expanded format
            if (value.StartsWith("nsu=", StringComparison.Ordinal))
            {
                parts = value.Split(';');
                value = value[(parts[0].Length + 1)..];
                return new QualifiedName(
                    string.IsNullOrEmpty(value) ? null : value.UrlDecode(),
                    context.NamespaceUris.GetIndexOrAppend(parts[0][4..]));
            }

            // Try to parse as uri with fragment
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (string.IsNullOrEmpty(uri.Fragment))
                {
                    value = string.Empty;
                }
                else
                {
                    value = uri.Fragment.TrimStart('#');
                }
                nsUri = uri.NoQueryAndFragment().AbsoluteUri;
            }
            else
            {
                // Not a real namespace uri - split and decode
                parts = value.Split('#');
                if (parts.Length == 2)
                {
                    nsUri = parts[0];
                    value = parts[1];
                }
            }
            if (nsUri != null)
            {
                return new QualifiedName(
                    string.IsNullOrEmpty(value) ? null : value.UrlDecode(),
                    context.NamespaceUris.GetIndexOrAppend(nsUri));
            }
            try
            {
                return QualifiedName.Parse(value.UrlDecode());
            }
            catch
            {
                // Give up
                return new QualifiedName(value);
            }
        }
    }
}
