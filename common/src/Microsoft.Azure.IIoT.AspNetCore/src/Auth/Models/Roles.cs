// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {

    /// <summary>
    /// Common roles
    /// </summary>
    public static class Roles {

        /// <summary>
        /// Admin role name
        /// </summary>
        public const string Admin = nameof(Admin);

        /// <summary>
        /// Approver role name
        /// </summary>
        public const string Sign = nameof(Sign);

        /// <summary>
        /// Writer role name
        /// </summary>
        public const string Write = nameof(Write);
    }
}
