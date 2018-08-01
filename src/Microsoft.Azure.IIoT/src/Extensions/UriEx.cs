// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Web;

    public static class UriEx {

        /// <summary>
        /// Returns a query and fragmentless uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri NoQueryAndFragment(this Uri uri) =>
            new UriBuilder(uri) { Fragment = null, Query = null }.Uri;

        /// <summary>
        /// Returns a fragmentless uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri NoFragment(this Uri uri) =>
            new UriBuilder(uri) { Fragment = null }.Uri;

        /// <summary>
        /// Returns just scheme and host
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri GetRoot(this Uri uri) => new UriBuilder {
            Scheme = uri.Scheme, Host = uri.Host, Port = uri.Port }.Uri;

        /// <summary>
        /// Make qualified name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nsUri"></param>
        /// <returns></returns>
        public static Uri AsQualifiedName(this string name, Uri nsUri) =>
            nsUri.Qualify(name);

        /// <summary>
        /// Gets the name and returns the base uri
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Uri GetQualifiedName(this Uri uri, out string name) {
            if (!string.IsNullOrEmpty(uri.Fragment)) {
                name = uri.Fragment.TrimStart('#').UrlDecode();
                return uri.NoFragment();
            }
            var builder = new UriBuilder(uri);
            var index = builder.Path.LastIndexOf('/');
            if (index == -1) {
                throw new InvalidOperationException("No ");
            }
            name = builder.Path.Substring(index + 1).UrlDecode();
            builder.Path = builder.Path.Substring(0, index + 1);
            return builder.Uri;
        }

        /// <summary>
        /// helper to format a uri
        /// </summary>
        /// <param name="nsUri"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string Qualify(this string nsUri, string property) {
            if (nsUri == null) {
                throw new ArgumentNullException(nameof(nsUri));
            }
            if (property == null) {
                return null;
            }
            if (nsUri.Length == 0) {
                return nsUri;
            }
            switch (nsUri[nsUri.Length - 1]) {
                case '/':
                case '#':
                    return nsUri + property.UrlEncode();
                default:
                    return nsUri + "/" + property.UrlEncode();
            }
        }

        /// <summary>
        /// Qualify a property using the uri
        /// </summary>
        /// <param name="nsUri"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static Uri Qualify(this Uri nsUri, string property) =>
            new Uri(Qualify(nsUri.AbsoluteUri, property));

        /// <summary>
        /// Encode a string for inclusion in url
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlEncode(this string value) =>
            HttpUtility.UrlEncode(value);

        /// <summary>
        /// Decode a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlDecode(this string value) =>
            HttpUtility.UrlDecode(value);
    }
}
