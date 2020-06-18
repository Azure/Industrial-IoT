// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IProcessIdentity {

        /// <summary>
        /// ID
        /// </summary>
        public string ServiceId => "ENGINEERING_TOOL";

        /// <summary>
        /// Process id
        /// </summary>
        public string Id => System.Guid.NewGuid().ToString();

        /// <summary>
        /// Name of service
        /// </summary>
        public string Name => "Engineering-Tool";

        /// <summary>
        /// Description of service
        /// </summary>
        public string Description => "Azure Industrial IoT Engineering Tool";
    }
}
