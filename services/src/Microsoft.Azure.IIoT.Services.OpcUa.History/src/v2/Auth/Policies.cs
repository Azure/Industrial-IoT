// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines Historian service api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to read and browse
        /// </summary>
        public const string CanRead =
            nameof(CanRead);

        /// <summary>
        /// Allowed to write or execute
        /// </summary>
        public const string CanUpdate =
            nameof(CanUpdate);

        /// <summary>
        /// Allowed to delete
        /// </summary>
        public const string CanDelete =
            nameof(CanDelete);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanRead;
            yield return CanUpdate;
            yield return CanDelete;
        }
    }
}
