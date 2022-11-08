// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.RateLimiting.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public class RateLimitingConfig : ConfigBase, IRateLimitingConfig {

        private const string kAspNetCore_RateLimiting_MaxConcurrentRequests = "AspNetCore:RateLimiting:MaxConcurrentRequests";
        private const string kAspNetCore_RateLimiting_PathException = "AspNetCore:RateLimiting:PathException";
        private const int kAspNetCore_RateLimiting_MaxConcurrentRequests_Default = 400;

        /// <inheritdoc/>
        public int AspNetCoreRateLimitingMaxConcurrentRequests =>
            GetIntOrDefault(kAspNetCore_RateLimiting_MaxConcurrentRequests,
                () => GetIntOrDefault(AspNetCoreVariable.ASPNETCORE_RATELIMITING_MAX_CONCURRENT_REQUESTS,
                () => GetIntOrDefault("MAX_CONCURRENT_REQUESTS",
                () => kAspNetCore_RateLimiting_MaxConcurrentRequests_Default)));

        /// <inheritdoc/>
        public string AspNetCoreRateLimitingPathException =>
            GetStringOrDefault(kAspNetCore_RateLimiting_PathException,
                () => GetStringOrDefault(AspNetCoreVariable.ASPNETCORE_RATELIMITING_PATHEXCEPTION,
                () => null));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public RateLimitingConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
