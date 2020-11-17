// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Runtime {

    using IIoTPlatform_E2E_Tests.Config;
    using Microsoft.Extensions.Configuration;
    using System;

    class IoTHubConfig : ConfigBase, IIoTHubConfig {

        /// <inheritdoc/>
        public string IoTHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING,
            () => { throw new Exception("IoT Hub connection string is not provided."); });

        public IoTHubConfig(IConfiguration configuration) :
            base(configuration) { }
    }
}
