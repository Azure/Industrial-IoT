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
        public const string DeviceId = "$$DeviceId";

        /// <summary>
        /// Module id of sender
        /// </summary>
        public const string ModuleId = "$$ModuleId";

        /// <summary>
        /// Event schema of message
        /// </summary>
        public const string EventSchemaType = "$$ContentType";

        /// <summary>
        /// Content encoding of message
        /// </summary>
        public const string ContentEncoding = "$$ContentEncoding";
    }
}
