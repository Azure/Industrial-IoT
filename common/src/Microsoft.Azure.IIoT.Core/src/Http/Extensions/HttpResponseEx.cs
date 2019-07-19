// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using Newtonsoft.Json;
    using System.Text;

    /// <summary>
    /// Http response extensions
    /// </summary>
    public static class HttpResponseEx {

        /// <summary>
        /// Get response content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T GetContent<T>(this IHttpResponse response,
            JsonSerializerSettings settings = null) {
            var json = response.GetContentAsString(Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(json,
                settings ?? JsonConvertEx.DefaultSettings);
        }
    }
}
