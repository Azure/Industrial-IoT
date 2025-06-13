// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    /// <summary>
    /// Publisher related message schemas
    /// </summary>
    public static class MessageSchemaTypes
    {
        /// <summary>
        /// Monitored item message
        /// </summary>
        public const string MonitoredItemMessageJson =
            "application/x-monitored-item-json-v1";

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
        /// Avro network message
        /// </summary>
        public const string NetworkMessageAvro =
            "application/x-network-message-avro-v0";

        /// <summary>
        /// Message contains discovery events
        /// </summary>
        public const string DiscoveryEvents =
            "application/x-discovery-event-v2-json";

        /// <summary>
        /// Message contains discovery progress messages
        /// </summary>
        public const string DiscoveryMessage =
            "application/x-discovery-message-v2-json";

        /// <summary>
        /// Runtime state message
        /// </summary>
        public const string RuntimeStateMessage =
            "application/x-runtimestate-message-v2-json";

        /// <summary>
        /// Writer group diagnostics message
        /// </summary>
        public const string WriterGroupDiagnosticsMessage =
            "application/x-writergroup-diagnostics-message-v2-json";
    }
}
