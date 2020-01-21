// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.Runtime {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : DiagnosticsConfig, IModuleConfig {

        /// <inheritdoc/>
        public string EdgeHubConnectionString => _module.EdgeHubConnectionString;
        /// <inheritdoc/>
        public bool BypassCertVerification => _module.BypassCertVerification;
        /// <inheritdoc/>
        public TransportOption Transport => _module.Transport;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {
            _module = new ModuleConfig(configuration);
        }

        private readonly ModuleConfig _module;
    }
}
