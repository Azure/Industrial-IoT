// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi
{
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IProcessIdentity
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id => "OPC_PUBLISHER";

        /// <summary>
        /// Process id
        /// </summary>
        public string ProcessId => System.Guid.NewGuid().ToString();

        /// <summary>
        /// Name of service
        /// </summary>
        public string Name => "Opc-Publisher-Service";

        /// <summary>
        /// Description of service
        /// </summary>
        public string Description => "Azure Industrial IoT OPC UA Publisher Service";

        /// <inheritdoc/>
        public string SiteId { get; }
    }
}
