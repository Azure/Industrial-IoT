// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Events.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines configuration service api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to read
        /// </summary>
        public const string CanRead =
            nameof(CanRead);

        /// <summary>
        /// Allowed to update or delete
        /// </summary>
        public const string CanWrite =
            nameof(CanWrite);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanRead;
            yield return CanWrite;
        }
    }
}
