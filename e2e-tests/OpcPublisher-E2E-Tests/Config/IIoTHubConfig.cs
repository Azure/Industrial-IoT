// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Config {

    /// <summary>
    /// IoT Hub configuration
    /// </summary>
    public interface IIoTHubConfig {

        /// <summary>
        /// IoT Hub connection string
        /// </summary>
        string IoTHubConnectionString { get; }

        /// <summary>
        /// The connection string of the EventHub-compatible endpoint of the IoTHub
        /// </summary>
        string IoTHubEventHubConnectionString { get; }
    }
}
