// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {

    public class HttpRequestOptions {

        /// <summary>
        /// Request timeout
        /// </summary>
        public int Timeout { get; set; } = 30000;
    }
}
