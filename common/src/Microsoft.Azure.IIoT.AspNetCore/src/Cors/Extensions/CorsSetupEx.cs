// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Cors {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

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
