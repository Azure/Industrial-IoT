// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Opc.Ua;
    using System;
    using System.Text;

    /// <summary>
    /// Qualified name extensions
    /// </summary>
    public static class QualifiedNameEx {

        /// <summary>
        /// Returns a uri that identifies the qualified name uniquely.
        /// </summary>
        /// <param name="qn"></param>
        /// <param name="context"></param>
        /// <param name="noRelativeUriAllowed"></param>
        /// <returns></returns>
        public static string AsString(this QualifiedName qn, ServiceMessageContext context,
            bool noRelativeUriAllowed = false) {
            if (qn == null || qn == QualifiedName.Null) {
                return string.Empty;
            }
            var buffer = new StringBuilder();
            if (qn.NamespaceIndex != 0 || noRelativeUriAllowed) {
                var nsUri = context.NamespaceUris.GetString(qn.NamespaceIndex);
                if (!string.IsNullOrEmpty(nsUri)) {
                    buffer.Append(nsUri);
                    if (!string.IsNullOrEmpty(qn.Name)) {
                        // Append name as fragment
                        buffer.Append("#");
                    }
                }
            }
            buffer.Append(qn.Name?.UrlEncode() ?? string.Empty);
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a qualified name from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static QualifiedName ToQualifiedName(this string value, ServiceMessageContext context) {
            if (string.IsNullOrEmpty(value)) {
                return QualifiedName.Null;
            }
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) {
                if (string.IsNullOrEmpty(uri.Fragment)) {
                    value = string.Empty;
                }
                else {
                    value = uri.Fragment.TrimStart('#');
                }
                var nsUri = uri.NoQueryAndFragment().AbsoluteUri;
                return new QualifiedName(string.IsNullOrEmpty(value) ? null : value.UrlDecode(),
                    context.NamespaceUris.GetIndexOrAppend(nsUri));
            }
            try {
                return QualifiedName.Parse(value.UrlDecode());
            }
            catch {
                // Give up
                return new QualifiedName(value);
            }
        }
    }
}
