// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Web;

    /// <summary>
    /// Uri extensions
    /// </summary>
    public static class UriEx {

        /// <summary>
        /// Returns a query and fragmentless uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri NoQueryAndFragment(this Uri uri) =>
            new UriBuilder(uri) { Fragment = null, Query = null }.Uri;

        /// <summary>
        /// Returns just scheme and host
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri GetRoot(this Uri uri) => new UriBuilder {
            Scheme = uri.Scheme, Host = uri.Host, Port = uri.Port }.Uri;

        /// <summary>
        /// Changes scheme
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
        public static Uri ChangeScheme(this Uri uri, string scheme) =>
            new UriBuilder(uri) { Scheme = scheme }.Uri;

        /// <summary>
        /// Replace host name with the one in the discovery url
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="host"></param>
        public static Uri ChangeHost(this Uri uri, string host) =>
            new UriBuilder(uri) { Host = host }.Uri;

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
