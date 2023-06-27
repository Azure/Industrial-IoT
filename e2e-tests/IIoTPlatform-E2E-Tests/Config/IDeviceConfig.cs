// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Config
{
    /// <summary>
    /// IoT Edge device configuration
    /// </summary>
    public interface IDeviceConfig
    {
        /// <summary>
        /// IoT Edge device id
        /// </summary>
        string DeviceId { get; }
    }
}
