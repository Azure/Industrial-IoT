// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Auth {
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.AspNetCore.Authorization;
    using System;

    /// <summary>
    /// Defines opcvault api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to query applications and cert requests
        /// </summary>
        public const string CanRead =
            nameof(CanRead);

        /// <summary>
        /// Allowed to create, update and delete applications and cert requests
        /// </summary>
        public const string CanWrite =
            nameof(CanWrite);

        /// <summary>
        /// Allowed to approve and sign or to reject cert requests
        /// </summary>
        public const string CanSign =
            nameof(CanSign);

        /// <summary>
        /// Allowed to manage applications and cert requests
        /// </summary>
        public const string CanManage =
            nameof(CanManage);

        /// <summary>
        /// Get rights for policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        internal static Func<AuthorizationHandlerContext, bool> RoleMapping(string policy) {
            switch (policy) {
                case CanWrite:
                    return context =>
                        context.User.IsInRole(Roles.Write) ||
                        context.User.IsInRole(Roles.Admin) ||
                        context.User.IsInRole(Roles.Sign) ||
                        context.User.HasClaim(c => c.Type == Claims.Execute);
                case CanSign:
                    return context =>
                        context.User.IsInRole(Roles.Sign) ||
                        context.User.HasClaim(c => c.Type == Claims.Execute);
                case CanManage:
                    return context =>
                        context.User.IsInRole(Roles.Admin) ||
                        context.User.HasClaim(c => c.Type == Claims.Execute);
                default:
                    return null;
            }
        }
    }
}
