// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System.Text;

    /// <summary>
    /// Http response extensions
    /// </summary>
    public static class HttpResponseEx {

        /// <summary>
        /// Response content
        /// </summary>
        public static string GetContentAsString(this IHttpResponse response,
            Encoding encoding) {
            return encoding.GetString(response.Content);
        }

        /// <summary>
        /// Response content
        /// </summary>
        public static string GetContentAsString(this IHttpResponse response) {
            return GetContentAsString(response, Encoding.UTF8);
        }

        /// <summary>
        /// Validate response
        /// </summary>
        /// <param name="response"></param>
        public static void Validate(this IHttpResponse response) {
            response.StatusCode.Validate(response.GetContentAsString());
        }

        /// <summary>
        /// True if request resulted in error
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsError(this IHttpResponse response) {
            return response.StatusCode.IsError();
        }
    }
}
