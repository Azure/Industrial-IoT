// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Roles in play with opc ua explorer
    /// </summary>
    public static class Role {

        /// <summary>
        /// can browse and publish
        /// </summary>
        public const string Operator = nameof(Operator);

        /// <summary>
        /// can do all system functions
        /// </summary>
        public const string Admin = nameof(Admin);

        /// <summary>
        /// default OAuth role name in AAD
        /// </summary>
        public const string user_impersonation = nameof(user_impersonation);

        /// <summary>
        /// Return all roles
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return Operator;
            yield return Admin;
            yield return user_impersonation;
        }
    }
}
