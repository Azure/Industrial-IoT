// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2 {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Auth;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Azure.IIoT.Services.Auth;

    /// <summary>
    /// AuthorizationOptions extension
    /// </summary>
    public static class AuthorizationOptionsEx {

        /// <summary>
        /// Add policies to options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="withAuthorization"></param>
        /// <param name="useRoleBasedAccess"></param>
        public static void AddPolicies(this AuthorizationOptions options,
            bool withAuthorization, bool useRoleBasedAccess) {

            if (!withAuthorization) {
                options.AddNoOpPolicies(Policies.All());
                return;
            }

            options.AddPolicy(Policies.CanRead, policy =>
                policy.RequireAuthenticatedUser());

            if (!useRoleBasedAccess) {
                options.AddPolicy(Policies.CanWrite, policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy(Policies.CanSign, policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy(Policies.CanManage, policy =>
                    policy.RequireAuthenticatedUser());
            }
            else {
                options.AddPolicy(Policies.CanWrite, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(WriterRights));
                options.AddPolicy(Policies.CanSign, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(ApproverRights));
                options.AddPolicy(Policies.CanManage, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(AdminRights));
            }
        }

        /// <summary>
        /// Admin either has the admin role, or has execute claim
        /// </summary>
        public static bool AdminRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Approver either has the Sign role, or has execute claim
        /// </summary>
        public static bool ApproverRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Sign) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Writer either has the Sign, Admin or Writer role, or has execute claim
        /// </summary>
        public static bool WriterRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Write) ||
                context.User.IsInRole(Roles.Admin) ||
                context.User.IsInRole(Roles.Sign) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }
    }
}
