// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core {

    /// <summary>
    /// Publisher related message schemas
    /// </summary>
    public static class MessageSchemaTypes {

        /// <summary>
        /// Monitored item message
        /// </summary>
        public const string MonitoredItemMessageJson =
            "application/x-monitored-item-json-v1";

        /// <summary>
        /// Monitored item message binary encoded
        /// </summary>
        public const string MonitoredItemMessageBinary =
            "application/x-monitored-item-uabinary-v1";

        /// <summary>
        /// Moniored Item Message Model using json encoding
        /// </summary>
        public const string MonitoredItemMessageModelJson =
            "application/x-monitored-itemsample-model-json-v1";

        /// <summary>
        /// Json network message
        /// </summary>
        public const string NetworkMessageJson =
            "application/x-network-message-json-v1";

        /// <summary>
        /// Uadp network message
        /// </summary>
        public const string NetworkMessageUadp =
            "application/x-network-message-uadp-v1";

        /// <summary>
        /// Network Message Model using json encoding
        /// </summary>
        public const string NetworkMessageModelJson =
            "application/x-network-message-model-json-v1";
    }
}
