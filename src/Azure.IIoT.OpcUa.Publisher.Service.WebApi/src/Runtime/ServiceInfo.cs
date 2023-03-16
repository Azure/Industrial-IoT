// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Furly.Extensions.Hosting;

    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IProcessIdentity
    {
        /// <inheritdoc/>
        public string Id => System.Guid.NewGuid().ToString();

        /// <inheritdoc/>
        public string Name => "Opc-Publisher-Service";

        /// <inheritdoc/>
        public string Description => "Azure Industrial IoT OPC UA Publisher Service";
    }
}
