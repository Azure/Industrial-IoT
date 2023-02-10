// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin {
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IProcessIdentity {

        /// <summary>
        /// ID
        /// </summary>
        public string Id => "OPC_TWIN";

        /// <summary>
        /// Process id
        /// </summary>
        public string ProcessId => System.Guid.NewGuid().ToString();

        /// <summary>
        /// Name of service
        /// </summary>
        public string Name => "Opc-Twin-Service";

        /// <summary>
        /// Description of service
        /// </summary>
        public string Description => "Azure Industrial IoT OPC UA Twin Service";

        /// <inheritdoc/>
        public string SiteId { get; }
    }
}
