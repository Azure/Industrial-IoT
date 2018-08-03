// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Auth
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Azure.IIoT.Services.Auth;

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
            options.AddPolicy(Policies.CanBrowse, policy =>
                policy.RequireAuthenticatedUser());
            options.AddPolicy(Policies.CanControl, policy =>
                policy.RequireAuthenticatedUser().Require(AdminRights));
            options.AddPolicy(Policies.CanPublish, policy =>
                policy.RequireAuthenticatedUser().Require(AdminRights));
        }

        /// <summary>
        /// Admin either has the admin role, or has execute claim
        /// </summary>
        public static bool AdminRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }
    }
}
