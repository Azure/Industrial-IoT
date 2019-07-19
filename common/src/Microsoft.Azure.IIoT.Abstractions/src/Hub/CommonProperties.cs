// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// Common IIoT IoT Hub properties
    /// </summary>
    public static class CommonProperties {

        /// <summary>
        /// Device id of sender
        /// </summary>
        public const string kDeviceId = "$$DeviceId";

        /// <summary>
        /// Module id of sender
        /// </summary>
        public const string kModuleId = "$$ModuleId";

        /// <summary>
        /// Content type of message
        /// </summary>
        public const string kContentType = "$$ContentType";

        /// <summary>
        /// Content encoding of message
        /// </summary>
        public const string kContentEncoding = "$$ContentEncoding";

        /// <summary>
        /// Message creation time at sender
        /// </summary>
        public const string kCreationTimeUtc = "$$CreationTimeUtc";
    }
}
