// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.Common.Users.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines publisher service api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to view
        /// </summary>
        public const string CanRead =
            nameof(CanRead);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string CanManage =
            nameof(CanManage);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanRead;
            yield return CanManage;
        }
    }
}
