// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Runtime {

    using IIoTPlatform_E2E_Tests.Config;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    class DeviceConfig : ConfigBase, IDeviceConfig {

        /// <inheritdoc/>
        public string DeviceId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_ID,
            () => { throw new Exception("IoT Edge device id is not provided."); });

        public DeviceConfig(IConfiguration configuration) :
            base(configuration) {}
    }
}
