// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Middleware to create service principal for service to service
    /// requests.  <see cref="InternalAuthHandler"/> for more info.
    /// </summary>
    public class InternalAuthMiddleware {

        /// <summary>
        /// Create middleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        public InternalAuthMiddleware(RequestDelegate next, ILogger logger) {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Check whether user is authenticated, if no authentication then
        /// check header and create principal
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context) {

            // We can do this because of there's only a single authentication scheme
            if (!(context.User?.Identity?.IsAuthenticated ?? false)) {

                if (context.Request.Headers.TryGetValue(HttpHeader.SourceId,
                        out var name)) {
                    context.Request.Headers.TryGetValue(HttpHeader.Roles,
                        out var roles);

                    // Create service principle identity
                    context.User = new ServicePrincipal(name[0], roles);
                }
            }
            // Call the next delegate/middleware in the pipeline
            return _next(context);
        }

        /// <summary>
        /// Helper principal class
        /// </summary>
        private class ServicePrincipal : ClaimsPrincipal {

            /// <inheritdoc/>
            public override IIdentity Identity => _identity;
            /// <inheritdoc/>
            public override bool IsInRole(string role) => true;

            /// <summary>
            /// Create principal
            /// </summary>
            /// <param name="name"></param>
            /// <param name="roles"></param>
            public ServicePrincipal(string name, IEnumerable<string> roles) {
                _identity = new ServiceIdentity(name, GetClaims(roles));
                AddIdentity(_identity);
            }

            /// <summary>
            /// Get default claims
            /// </summary>
            /// <param name="roles"></param>
            /// <returns></returns>
            private static IEnumerable<Claim> GetClaims(IEnumerable<string> roles) {
                yield return new Claim(ClaimTypes.Authentication,
                    "Bearer");
                yield return new Claim(ClaimTypes.Name,
                    "Service Principal");
                yield return new Claim(ClaimTypes.Role,
                    JToken.FromObject(roles.ToArray()).ToString());
            }

            /// <summary>
            /// Helper service identity
            /// </summary>
            private class ServiceIdentity : ClaimsIdentity {

                /// <inheritdoc/>
                public override string AuthenticationType => "Bearer";
                /// <inheritdoc/>
                public override bool IsAuthenticated => true;
                /// <inheritdoc/>
                public override string Name => _name;

                /// <summary>
                /// Create identity
                /// </summary>
                /// <param name="name"></param>
                /// <param name="claims"></param>
                public ServiceIdentity(string name, IEnumerable<Claim> claims) :
                    base(claims) {
                    _name = name;
                }

                private readonly string _name;
            }

            private readonly ServiceIdentity _identity;
        }

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
    }
}
