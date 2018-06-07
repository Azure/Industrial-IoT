// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Auth {
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Filter service to service requests and check authentication
    /// </summary>
    public class AuthMiddleware {

        /// <summary>
        /// Create middleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public AuthMiddleware(RequestDelegate next, IAuthConfig config, ILogger logger) {
            _next = next;
            _logger = logger;
            _authRequired = config.AuthRequired;

            if (!_authRequired) {
                _logger.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
                _logger.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
                _logger.Warn("### AUTHENTICATION IS DISABLED! ###", () => { });
            }
        }

        /// <summary>
        /// Check whether user is authenticated, if no authentication is required and not
        /// internal request, fail the request.  Else proceed.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context) {

            // We can do this because of there's only a single authentication scheme
            var user = context.User;
            if (!_authRequired || (user?.Identity?.IsAuthenticated ?? false)) {
                // Call the next delegate/middleware in the pipeline
                return _next(context);
            }

            //
            // User requests are marked with this header by the reverse proxy.
            // This means it is a service to service request running in the
            // private network, so we skip the auth required for user requests
            //
            // TODO: Use this to serialize and deserialize the token model
            //
            if (!context.Request.Headers.ContainsKey("X-Source")) {
                _logger.Debug("Skipping auth for service to service request", () => { });
                return _next(context);
            }

            _logger.Warn("Authentication required", () => { });
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["Content-Type"] = "application/json";
            return context.Response.WriteAsync(@"{""Error"":""Authentication required""}");
        }

        private readonly bool _authRequired;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
    }
}
