// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using System;

    /// <summary>
    /// Twin identity
    /// </summary>
    public sealed class TwinIdentity : IProcessIdentity {

        /// <inheritdoc/>
        public string ProcessId => _factory.DeviceId;
        /// <inheritdoc/>
        public string SiteId => null;
        /// <inheritdoc/>
        public string Id => _factory.ModuleId;
        /// <inheritdoc/>
        public string Name => "OPC Twin";

        /// <summary>
        /// Create identity
        /// </summary>
        /// <param name="factory"></param>
        public TwinIdentity(IClientFactory factory) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly IClientFactory _factory;
    }
}
