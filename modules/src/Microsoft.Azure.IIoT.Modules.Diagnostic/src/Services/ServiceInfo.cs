// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.Services {

    /// <summary>
    /// Twin Module information
    /// </summary>
    public static class ServiceInfo {

        /// <summary>
        /// Name of service
        /// </summary>
        public const string NAME = "Diagnostic";

        /// <summary>
        /// Number used for routing requests
        /// </summary>
        public const string NUMBER = "2";

        /// <summary>
        /// Full path used in the URL
        /// </summary>
        public const string PATH = "v" + NUMBER;

        /// <summary>
        /// Date when the API version has been published
        /// </summary>
        public const string DATE = "201909";

        /// <summary>
        /// Description of service
        /// </summary>
        public const string DESCRIPTION = "IoT Edge Diagnostic Module";
    }
}
