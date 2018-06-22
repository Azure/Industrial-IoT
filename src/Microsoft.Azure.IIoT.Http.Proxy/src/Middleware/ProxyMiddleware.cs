// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Proxy {
    using Microsoft.Azure.IIoT.Services.Http.Proxy;
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    public class ProxyMiddleware {

        /// <summary>
        /// Create middleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="proxy"></param>
        public ProxyMiddleware(RequestDelegate next, IProxy proxy) {
            _next = next;
            _proxy = proxy;
        }

        /// <summary>
        /// Handle request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context) =>
            _proxy.ForwardAsync(context.Request, context.Response);

        private readonly RequestDelegate _next;
        private readonly IProxy _proxy;
    }
}
