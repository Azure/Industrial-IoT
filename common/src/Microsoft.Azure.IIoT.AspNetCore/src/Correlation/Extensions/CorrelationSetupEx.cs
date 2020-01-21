// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Correlation {
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Correlation setup extensions
    /// </summary>
    public static class CorrelationSetupEx {

        /// <summary>
        /// Configure app to use correlation middleware
        /// </summary>
        /// <param name="app"></param>
        public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app) {
            return app.UseMiddleware<CorrelationSetup>();
        }
    }
}
