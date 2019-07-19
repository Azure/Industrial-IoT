// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2 {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Auth;
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.AspNetCore.Authorization;

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

            options.AddPolicy(Policies.CanBrowse, policy =>
                policy.RequireAuthenticatedUser());

            if (!useRoleBasedAccess) {
                options.AddPolicy(Policies.CanControl, policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy(Policies.CanPublish, policy =>
                    policy.RequireAuthenticatedUser());
            }
            else {
                options.AddPolicy(Policies.CanControl, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(ControlRights));
                options.AddPolicy(Policies.CanPublish, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(PublishRights));
            }
        }

        /// <summary>
        /// Admin has admin role or execute and can do all system functions
        /// </summary>
        public static bool AdminRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Publisher either has Admin or Sign role, or has execute claim
        /// </summary>
        public static bool PublishRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Admin) ||
                context.User.IsInRole(Roles.Sign) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Control has Sign, Admin or Writer role, or has execute claim
        /// </summary>
        public static bool ControlRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Write) ||
                context.User.IsInRole(Roles.Admin) ||
                context.User.IsInRole(Roles.Sign) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }
    }
}
