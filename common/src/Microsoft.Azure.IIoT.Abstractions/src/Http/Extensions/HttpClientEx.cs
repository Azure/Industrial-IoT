// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Http client extensions
    /// </summary>
    public static class HttpClientEx {

        /// <summary>
        /// New request from string
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        public static IHttpRequest NewRequest(this IHttpClient client,
            string uri, string resourceId = null) {
            return client.NewRequest(new Uri(uri), resourceId);
        }

        /// <summary>
        /// Get using uri
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        public static Task<IHttpResponse> GetAsync(this IHttpClient client,
            string uri, string resourceId = null) {
            return client.GetAsync(client.NewRequest(uri, resourceId));
        }

        /// <summary>
        /// Get using uri
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        public static Task<IHttpResponse> GetAsync(this IHttpClient client,
            Uri uri, string resourceId = null) {
            return client.GetAsync(client.NewRequest(uri, resourceId));
        }

        /// <summary>
        /// Delete using uri
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        public static Task<IHttpResponse> DeleteAsync(this IHttpClient client,
            string uri, string resourceId = null) {
            return client.DeleteAsync(client.NewRequest(uri, resourceId));
        }

        /// <summary>
        /// Delete using uri
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        public static Task<IHttpResponse> DeleteAsync(this IHttpClient client,
            Uri uri, string resourceId = null) {
            return client.DeleteAsync(client.NewRequest(uri, resourceId));
        }
    }
}
