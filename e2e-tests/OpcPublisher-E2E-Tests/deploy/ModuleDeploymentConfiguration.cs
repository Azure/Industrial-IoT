// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Deploy {
    using TestExtensions;

    /// <summary>
    /// Configuration that corresponds to a module deployment.
    /// </summary>
    public abstract class ModuleDeploymentConfiguration : DeploymentConfiguration {

        /// <summary>
        /// Name of the module that is being deployed by this configuration.
        /// </summary>
        public abstract string ModuleName { get; }

        /// <summary>
        /// Constructor that delegates to the base DeploymentConfiguration.
        /// </summary>
        /// <param name="context"></param>
        public ModuleDeploymentConfiguration(IIoTPlatformTestContext context)
            : base(context) { }
    }
}
