// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <inheritdoc/>
    public class RegistryConfig : ConfigBase, IRegistryConfig {

        /// <summary>
        /// Registry configuration
        /// </summary>
        private const string kOpcRegistry_ApplicationsAutoApproveKey = "OpcVault:ApplicationsAutoApprove";

        /// <inheritdoc/>
        public bool ApplicationsAutoApprove => GetBoolOrDefault(kOpcRegistry_ApplicationsAutoApproveKey,
            GetBoolOrDefault("OPC_REGISTRY_AUTOAPPROVE", true));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public RegistryConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}
