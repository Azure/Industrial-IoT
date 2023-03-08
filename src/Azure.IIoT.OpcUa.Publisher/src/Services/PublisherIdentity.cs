// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Furly.Azure.IoT.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;

    /// <summary>
    /// Publisher identity
    /// </summary>
    public sealed class PublisherIdentity : IProcessInfo
    {
        /// <inheritdoc/>
        public string ProcessId => _moduleIdentity.DeviceId;
        /// <inheritdoc/>
        public string SiteId => _config?.Site;
        /// <inheritdoc/>
        public string Id => _moduleIdentity.ModuleId;
        /// <inheritdoc/>
        public string Name => "OPC Publisher";
        /// <inheritdoc/>
        public string Description => "Connect OPC UA servers to Azure.";

        /// <summary>
        /// Create identity
        /// </summary>
        /// <param name="moduleIdentity"></param>
        /// <param name="config"></param>
        public PublisherIdentity(IIoTEdgeDeviceIdentity moduleIdentity,
            IPublisherConfiguration config = null)
        {
            _moduleIdentity = moduleIdentity ??
                throw new ArgumentNullException(nameof(moduleIdentity));
            _config = config;
        }

        private readonly IIoTEdgeDeviceIdentity _moduleIdentity;
        private readonly IPublisherConfiguration _config;
    }
}
