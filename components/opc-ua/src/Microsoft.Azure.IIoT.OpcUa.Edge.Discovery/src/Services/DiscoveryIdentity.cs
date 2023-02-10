// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using System;

    /// <summary>
    /// Discovery identity
    /// </summary>
    public sealed class DiscoveryIdentity : IProcessIdentity {

        /// <inheritdoc/>
        public string ProcessId => _factory.DeviceId;
        /// <inheritdoc/>
        public string SiteId => null;
        /// <inheritdoc/>
        public string Id => _factory.ModuleId;
        /// <inheritdoc/>
        public string Name => "OPC Discovery";

        /// <summary>
        /// Create identity
        /// </summary>
        /// <param name="factory"></param>
        public DiscoveryIdentity(IClientFactory factory) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly IClientFactory _factory;
    }
}
