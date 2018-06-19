// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1 {
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Auth;
    using Microsoft.Azure.IIoT.Web.Auth;
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// AuthorizationOptions extension
    /// </summary>
    public static class AuthorizationOptionsEx {

        /// <summary>
        /// Add v1 policies to options
        /// </summary>
        /// <param name="config"></param>
        /// <param name="options"></param>
        public static void AddV1Policies(this AuthorizationOptions options,
            IAuthConfig config) {

            if (!config.AuthRequired) {
                foreach (var p in Policy.All()) {
                    options.AddPolicy(p,
                        policy => policy.RequireAssertion(_ => true));
                }
                return;
            }

            // Otherwise, configure policies here to your liking
            options.AddPolicy(Policy.Query, policy =>
                policy.RequireAuthenticatedUser());
            options.AddPolicy(Policy.Manage, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Role.Admin));
            options.AddPolicy(Policy.Change, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Role.Operator));
        }
    }
}
