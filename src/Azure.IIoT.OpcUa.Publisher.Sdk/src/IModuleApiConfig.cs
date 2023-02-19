// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk {

    /// <summary>
    /// Configuration for IoT Edge module api
    /// </summary>
    public interface IModuleApiConfig {

        /// <summary>
        /// Edge device id
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id
        /// </summary>
        string ModuleId { get; }
    }
}
