// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Microsoft.AspNetCore.Builder;
    using System;

    /// <summary>
    /// Transport extensions
    /// </summary>
    public static class TransportEx {

        /// <summary>
        /// Use https middleware previously added via di
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOpcUaTransport(
            this IApplicationBuilder app) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            // Order important, want to handle websockets first.
            app = app.UseMiddleware<WebSocketMiddleware>();
            app = app.UseMiddleware<HttpMiddleware>();
            return app;
        }
    }
}
