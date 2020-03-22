// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Events {
    using Microsoft.Azure.IIoT.Services.OpcUa.Events.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
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

            options.AddPolicy(Policies.CanRead, policy =>
                policy.RequireAuthenticatedUser());

            if (!useRoleBasedAccess) {
                options.AddPolicy(Policies.CanWrite, policy =>
                    policy.RequireAuthenticatedUser());
            }
            else {
                options.AddPolicy(Policies.CanWrite, policy =>
                    policy.RequireAuthenticatedUser()
                        .Require(WriteRights));
            }
        }

        /// <summary>
        /// Control has Sign, Admin or Writer role, or has execute claim
        /// </summary>
        public static bool WriteRights(AuthorizationHandlerContext context) {
            return
                context.User.IsInRole(Roles.Write) ||
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }
    }
}
