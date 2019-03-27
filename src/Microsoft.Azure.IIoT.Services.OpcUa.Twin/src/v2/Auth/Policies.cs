// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines twin service api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to read and browse
        /// </summary>
        public const string CanBrowse =
            nameof(CanBrowse);

        /// <summary>
        /// Allowed to write or execute
        /// </summary>
        public const string CanControl =
            nameof(CanControl);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string CanPublish =
            nameof(CanPublish);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanBrowse;
            yield return CanControl;
            yield return CanPublish;
        }
    }
}
