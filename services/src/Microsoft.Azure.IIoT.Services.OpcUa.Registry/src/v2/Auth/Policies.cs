// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines registry api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to add and delete
        /// </summary>
        public const string CanManage =
            nameof(CanManage);

        /// <summary>
        /// Allowed to query or list
        /// </summary>
        public const string CanQuery =
            nameof(CanQuery);

        /// <summary>
        /// Allowed to update items
        /// </summary>
        public const string CanChange =
            nameof(CanChange);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanManage;
            yield return CanQuery;
            yield return CanChange;
        }
    }
}
