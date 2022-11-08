// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.RateLimiting {

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public interface IRateLimitingConfig {

        /// <summary>
        /// Max number of concurrent requests, 0 disables rate limiting
        /// </summary>
        int AspNetCoreRateLimitingMaxConcurrentRequests { get; }

        /// <summary>
        /// Path exception
        /// </summary>
        string AspNetCoreRateLimitingPathException { get; }
    }
}
