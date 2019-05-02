// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;

    /// <summary>
    /// AuthorizationOptions extension
    /// </summary>
    public static class AuthorizationOptionsEx
    {

        /// <summary>
        /// Add v1 policies to options
        /// </summary>
        /// <param name="config"></param>
        /// <param name="servicesConfig"></param>
        /// <param name="options"></param>
        public static void AddV1Policies(this AuthorizationOptions options,
            IAuthConfig config, IServicesConfig servicesConfig)
        {

            options.AddPolicy(Policies.CanRead, policy =>
                policy.RequireAuthenticatedUser());
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

        /// <summary>
        /// Admin either has the admin role, or has execute claim
        /// </summary>
        public static bool AdminRights(AuthorizationHandlerContext context)
        {
            return
                context.User.IsInRole(Roles.Administrator) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Approver either has the Sign role, or has execute claim
        /// </summary>
        public static bool ApproverRights(AuthorizationHandlerContext context)
        {
            return
                context.User.IsInRole(Roles.Approver) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

        /// <summary>
        /// Writer either has the Sign, Admin or Writer role, or has execute claim
        /// </summary>
        public static bool WriterRights(AuthorizationHandlerContext context)
        {
            return
                context.User.IsInRole(Roles.Writer) ||
                context.User.IsInRole(Roles.Administrator) ||
                context.User.IsInRole(Roles.Approver) ||
                context.User.HasClaim(c => c.Type == Claims.Execute);
        }

    }
}
