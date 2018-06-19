// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines various policies for user claims for the rest api.
    /// Policies are associated with roles, and users are assigned
    /// to roles.
    /// </summary>
    public static class Policy {

        /// <summary>
        /// Allowed to add new endpoint
        /// </summary>
        public const string Manage =
            nameof(Manage);

        /// <summary>
        /// Allowed to query or list
        /// </summary>
        public const string Query =
            nameof(Query);

        /// <summary>
        /// Allowed to write
        /// </summary>
        public const string Change =
            nameof(Change);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return Manage;
            yield return Query;
            yield return Change;
        }
    }
}
