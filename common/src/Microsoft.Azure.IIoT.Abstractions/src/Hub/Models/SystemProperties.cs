// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// IoT hub Message system property names
    /// </summary>
    public static class SystemProperties {

        /// <summary>
        /// Message schema
        /// </summary>
        public const string MessageSchema = "iothub-message-schema";

        /// <summary>
        /// Device id
        /// </summary>
        public const string ConnectionDeviceId = "iothub-connection-device-id";

        /// <summary>
        /// Module id
        /// </summary>
        public const string ConnectionModuleId = "iothub-connection-module-id";

        /// <summary>
        /// Device id
        /// </summary>
        public const string DeviceId = "deviceId";

        /// <summary>
        /// Module id
        /// </summary>
        public const string ModuleId = "moduleId";
    }
}
