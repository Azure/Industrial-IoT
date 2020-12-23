// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;

    /// <summary>
    /// Http request options
    /// </summary>
    public class HttpRequestOptions {

        /// <summary>
        /// Request timeout
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}
