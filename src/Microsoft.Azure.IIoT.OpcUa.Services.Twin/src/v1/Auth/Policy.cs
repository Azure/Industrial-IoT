// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines various policies for user claims for the rest api.
    /// Policies are associated with roles, and users are assigned
    /// to roles.
    /// </summary>
    public static class Policy {

        /// <summary>
        /// Allowed to read and browse
        /// </summary>
        public const string Browse =
            nameof(Browse);

        /// <summary>
        /// Allowed to write or execute
        /// </summary>
        public const string Control =
            nameof(Control);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string Publish =
            nameof(Publish);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return Browse;
            yield return Control;
            yield return Publish;
        }
    }
}
