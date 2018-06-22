// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Http {

    public static class HttpHandlerEx {

        /// <summary>
        /// Get root handler
        /// </summary>
        internal static HttpClientHandler GetRoot(this HttpMessageHandler handler) {
            while (true) {
                switch (handler) {
                    case DelegatingHandler del:
                        handler = del.InnerHandler;
                        break;
                    case HttpClientHandler cl:
                        return cl; ;
                    default:
                        return null;
                }
            }
        }
    }
}
