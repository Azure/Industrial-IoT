// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Primitives {
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Http response extensions
    /// </summary>
    public static class HttpResponseEx {

        /// <summary>
        /// Add header value
        /// </summary>
        /// <param name="response"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddHeader(this HttpResponse response,
            string key, string value) {
            if (!response.Headers.ContainsKey(key)) {
                response.Headers[key] = value;
            }
            else {
                var headers = response.Headers[key].ToList();
                headers.Add(value);
                response.Headers[key] = new StringValues(headers.ToArray());
            }
        }

        /// <summary>
        /// Add header values
        /// </summary>
        /// <param name="response"></param>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public static void AddHeaders(this HttpResponse response,
            string key, IEnumerable<string> values) {
            foreach (var value in values) {
                response.AddHeader(key, value);
            }
        }

        /// <summary>
        /// Disable iis session affinity.
        /// See https://azure.microsoft.com/blog/disabling-arrs-instance-affinity-in-windows-azure-web-sites
        /// </summary>
        /// <param name="response"></param>
        public static void DisableSessionAffinity(this HttpResponse response) {
            const string kHeader = "Arr-Disable-Session-Affinity";
            if (!response.Headers.ContainsKey(kHeader)) {
                response.Headers.Add(kHeader, "True");
            }
        }
    }
}
