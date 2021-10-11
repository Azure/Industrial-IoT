// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Businesslogic {

    /// <summary>
    /// Publisher related message schemas
    /// </summary>
    public static class MessageSchemaTypes {

        /// <summary>
        /// Samples message type json encoded
        /// </summary>
        public const string MonitoredItemMessageJson =
            "application/x-monitored-item-json-v1";

        /// <summary>
        /// Samples message type binary encoded
        /// </summary>
        public const string MonitoredItemMessageBinary =
            "application/x-monitored-item-uabinary-v1";

        /// <summary>
        /// OPC UA PubSub Json network message (json)
        /// </summary>
        public const string NetworkMessageJson =
            "application/x-network-message-json-v1";

        /// <summary>
        /// OPC UA PubSub Uadp network message (binary)
        /// </summary>
        public const string NetworkMessageUadp =
            "application/x-network-message-uadp-v1";
    }
}
