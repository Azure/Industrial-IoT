// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable
namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Context to pass data between test cases
    /// </summary>
    public class IIoTPlatformTestContext {

        /// <summary>
        /// Save the identifier of OPC server endpoints
        /// </summary>
        public string? OpcUaEndpointId { get; set; }

        /// <summary>
        /// IoT Hub registry manager
        /// </summary>
        public RegistryManager registryManager { get; set; }
    }
}
