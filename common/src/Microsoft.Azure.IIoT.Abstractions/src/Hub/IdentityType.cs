// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// Industrial IoT identity types
    /// </summary>
    public static class IdentityType {

        /// <summary>
        /// Gateway identity
        /// </summary>
        public const string Gateway = "iiotedge";

        /// <summary>
        /// Gds module identity
        /// </summary>
        public const string Gds = "gds";

        /// <summary>
        /// Discoverer identity
        /// </summary>
        public const string Discoverer = "discoverer";

        /// <summary>
        /// Twin module identity
        /// </summary>
        public const string Supervisor = "supervisor";

        /// <summary>
        /// Publisher module identity
        /// </summary>
        public const string Publisher = "publisher";

        /// <summary>
        /// Endpoint identity
        /// </summary>
        public const string Endpoint = "Endpoint";

        /// <summary>
        /// Application identity
        /// </summary>
        public const string Application = "Application";
    }
}
