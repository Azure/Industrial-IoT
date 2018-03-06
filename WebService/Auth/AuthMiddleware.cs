// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Auth {
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
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
        public AuthMiddleware(RequestDelegate next, IClientAuthConfig config, ILogger logger) {
            _next = next;
            _logger = logger;
            _authRequired = config.AuthRequired;

            // This will show in development mode, or in case auth is turned off
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
            // TODO ~devis: this is a temporary solution for public preview only
            // TODO ~devis: remove this approach and use the service to service authentication
            // https://github.com/Azure/pcs-auth-dotnet/issues/18
            // https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/issues/11
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
