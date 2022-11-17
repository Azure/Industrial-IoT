// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.RateLimiting {
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Rate limiting setup extensions
    /// </summary>
    public static class RateLimitingSetupEx {

        /// <summary>
        /// Configure app to use rate limiting middleware
        /// </summary>
        /// <param name="app"></param>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app) {
            return app.UseMiddleware<RateLimitingSetup>();
        }
    }
}
