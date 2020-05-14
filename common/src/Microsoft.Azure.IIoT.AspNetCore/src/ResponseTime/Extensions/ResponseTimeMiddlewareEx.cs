// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Monitoring {
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    ///Response time middleware extensions
    /// </summary>
    public static class ResponseTimeMiddlewareEx {

        /// <summary>
        /// Configure app to use response time middleware
        /// </summary>
        /// <param name="app"></param>
        public static IApplicationBuilder UseMonitoring(this IApplicationBuilder app) {
            return app.UseMiddleware<ResponseTimeMiddleware>();
        }
    }
}
