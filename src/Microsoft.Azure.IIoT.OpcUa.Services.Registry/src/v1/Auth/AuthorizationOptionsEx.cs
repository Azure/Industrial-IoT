// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1 {
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Auth;
    using Microsoft.Azure.IIoT.Services.Auth;
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
                options.AddNoOpPolicies(Policies.All());
                return;
            }

            // Otherwise, configure policies here to your liking
            options.AddPolicy(Policies.CanQuery, policy =>
                policy.RequireAuthenticatedUser());
            options.AddPolicy(Policies.CanManage, policy =>
                policy.RequireAuthenticatedUser().Require(AdminRights));
            options.AddPolicy(Policies.CanChange, policy =>
                policy.RequireAuthenticatedUser().Require(AdminRights));
        }

        /// <summary>
        /// can do all system functions
        /// </summary>
        public static bool AdminRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }
    }
}
