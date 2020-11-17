// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Config {

    /// <summary>
    /// IoT Hub configuration
    /// </summary>
    interface IIoTHubConfig {

        /// <summary>
        /// IoT Hub connection string
        /// </summary>
        string IoTHubConnectionString { get; }
    }
}
