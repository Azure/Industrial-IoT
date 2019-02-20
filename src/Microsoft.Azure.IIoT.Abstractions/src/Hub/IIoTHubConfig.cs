// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IIoTHubConfig {

        /// <summary>
        /// Connection string
        /// </summary>
        string IoTHubConnString { get; }

        /// <summary>
        /// IoT Hub Resource identifier
        /// </summary>
        string IoTHubResourceId { get; }
    }
}
