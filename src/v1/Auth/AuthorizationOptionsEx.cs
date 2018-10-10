// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
    using Microsoft.Azure.IIoT.Services.Auth;

    /// <summary>
    /// AuthorizationOptions extension
    /// </summary>
    public static class AuthorizationOptionsEx {

        /// <summary>
        /// Add v1 policies to options
        /// </summary>
        /// <param name="config"></param>
        /// <param name="servicesConfig"></param>
        /// <param name="options"></param>
        public static void AddV1Policies(this AuthorizationOptions options,
            IAuthConfig config, IServicesConfig servicesConfig) {

            if (!config.AuthRequired) {
                options.AddNoOpPolicies(Policies.All());
                return;
            }

            // Otherwise, configure policies here to your liking
            options.AddPolicy(Policies.CanRead, policy =>
                policy.RequireAuthenticatedUser());
            options.AddPolicy(Policies.CanWrite, policy =>
                policy.RequireAuthenticatedUser());
            if (servicesConfig.AutoApprove)
            {
                options.AddPolicy(Policies.CanSign, policy =>
                    policy.RequireAuthenticatedUser());
            }
            else
            {
                options.AddPolicy(Policies.CanSign, policy =>
                    policy.RequireAuthenticatedUser()
                    .Require(AdminRights));
            }
            options.AddPolicy(Policies.CanManage, policy =>
                policy.RequireAuthenticatedUser()
                .Require(AdminRights));
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
