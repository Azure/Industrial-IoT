// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Net;
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
        public static Uri NoQueryAndFragment(this Uri uri) {
            return new UriBuilder(uri) { Fragment = null, Query = null }.Uri;
        }

        /// <summary>
        /// Returns just scheme and host
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri GetRoot(this Uri uri) {
            return new UriBuilder {
                Scheme = uri.Scheme,
                Host = uri.Host,
                Port = uri.Port
            }.Uri;
        }

        /// <summary>
        /// Changes scheme
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
        public static Uri ChangeScheme(this Uri uri, string scheme) {
            return new UriBuilder(uri) { Scheme = scheme }.Uri;
        }

        /// <summary>
        /// Replace host name with the one in the discovery url
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="host"></param>
        public static Uri ChangeHost(this Uri uri, string host) {
            return new UriBuilder(uri) { Host = host }.Uri;
        }

        /// <summary>
        /// Encode a string for inclusion in url
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlEncode(this string value) {
            return HttpUtility.UrlEncode(value);
        }

        /// <summary>
        /// Decode a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlDecode(this string value) {
            return HttpUtility.UrlDecode(value);
        }

        /// <summary>
        /// Parse uds uri
        /// </summary>
        /// <param name="fileUri"></param>
        /// <param name="httpRequestUri"></param>
        public static string ParseUdsPath(this Uri fileUri, out Uri httpRequestUri) {
            var localPath = fileUri.LocalPath;
            // Find socket
            var builder = new UriBuilder(fileUri) {
                Scheme = "https",
                Host = Dns.GetHostName()
            };
            string fileDevice;
            string pathAndQuery;
            var index = localPath.IndexOf("sock", StringComparison.InvariantCultureIgnoreCase);
            if (index  != -1) {
                fileDevice = localPath.Substring(0, index + 4);
                pathAndQuery = localPath.Substring(index + 4);
            }
            else {
                // Find fake port delimiter
                index = localPath.IndexOf(':');
                if (index != -1) {
                    fileDevice = localPath.Substring(0, index);
                    pathAndQuery = localPath.Substring(index + 1);
                }
                else {
                    builder.Path = "/";
                    httpRequestUri = builder.Uri;
                    return localPath.TrimEnd('/');
                }
            }

            // Find first path character and strip off everything before...
            index = pathAndQuery.IndexOf('/');
            if (index > 0) {
                pathAndQuery = pathAndQuery.Substring(index, pathAndQuery.Length - index);
            }
            builder.Path = pathAndQuery;
            httpRequestUri = builder.Uri;
            return fileDevice;
        }
    }
}
