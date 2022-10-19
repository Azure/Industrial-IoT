// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore {

    /// <summary>
    /// Common runtime environment variables for AspNetCore configuration.
    /// </summary>
    public static class AspNetCoreVariable {

        /// <summary>
        /// Determines whethere processing of forwarded headers should be enabled or not.
        /// </summary>
        public const string ASPNETCORE_FORWARDEDHEADERS_ENABLED =
            "ASPNETCORE_FORWARDEDHEADERS_ENABLED";

        /// <summary>
        /// Determines limit on number of entries in the forwarded headers.
        /// </summary>
        public const string ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT =
            "ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT";

        /// <summary>
        /// Set the max number of concurrent requests to the api
        /// </summary>
        public const string ASPNETCORE_RATELIMITING_MAX_CONCURRENT_REQUESTS =
            "ASPNETCORE_RATELIMITING_MAX_CONCURRENT_REQUESTS";

        /// <summary>
        /// Set the path excepted by rate limiting
        /// </summary>
        public const string ASPNETCORE_RATELIMITING_PATHEXCEPTION =
            "ASPNETCORE_RATELIMITING_PATHEXCEPTION";
    }
}
