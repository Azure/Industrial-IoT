// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Azure.IIoT.AspNetCore;

    /// <summary>
    /// Cors setup extensions
    /// </summary>
    public static class CorsSetupEx {

        /// <summary>
        /// Configure app to use cors middleware.
        /// Note: Must be before UseMvc!
        /// </summary>
        /// <param name="app"></param>
        public static void EnableCors(this IApplicationBuilder app) {
            var setup = app.ApplicationServices.GetService<ICorsSetup>();
            if (setup != null) {
                setup.UseMiddleware(app);
            }
        }
    }
}
