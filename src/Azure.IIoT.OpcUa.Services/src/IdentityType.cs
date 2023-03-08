// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services
{
    /// <summary>
    /// Industrial IoT identity types
    /// </summary>
    public static class IdentityType
    {
        /// <summary>
        /// Gateway identity
        /// </summary>
        public const string Gateway = "iiotedge";

        /// <summary>
        /// Publisher module identity
        /// </summary>
        public const string Publisher = "publisher_v2";

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
