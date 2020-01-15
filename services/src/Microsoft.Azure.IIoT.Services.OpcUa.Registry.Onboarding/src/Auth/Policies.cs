// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines registry api policies.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Allowed to bulk add
        /// </summary>
        public const string CanOnboard =
            nameof(CanOnboard);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return CanOnboard;
        }
    }
}
