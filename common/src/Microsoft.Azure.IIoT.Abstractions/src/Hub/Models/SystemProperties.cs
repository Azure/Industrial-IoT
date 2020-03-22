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
        /// Message id of message
        /// </summary>
        public const string MessageId = "message-id";

        /// <summary>
        /// Lock token
        /// </summary>
        public const string LockToken = "iothub-messagelocktoken";

        /// <summary>
        /// Sequence number of message
        /// </summary>
        public const string SequenceNumber = "sequence-number";

        /// <summary>
        /// Target
        /// </summary>
        public const string To = "to";

        /// <summary>
        /// Time enqueued
        /// </summary>
        public const string EnqueuedTime = "iothub-enqueuedtime";

        /// <summary>
        /// Expiration of message in queue
        /// </summary>
        public const string ExpiryTimeUtc = "absolute-expiry-time";

        /// <summary>
        /// Correlation id of message
        /// </summary>
        public const string CorrelationId = "correlation-id";

        /// <summary>
        /// Delivery count
        /// </summary>
        public const string DeliveryCount = "iothub-deliverycount";

        /// <summary>
        /// User id of sender
        /// </summary>
        public const string UserId = "user-id";

        /// <summary>
        /// Operation type
        /// </summary>
        public const string Operation = "iothub-operation";

        /// <summary>
        /// Ack flag
        /// </summary>
        public const string Ack = "iothub-ack";

        /// <summary>
        /// Output tag
        /// </summary>
        public const string OutputName = "iothub-outputname";

        /// <summary>
        /// Input tag
        /// </summary>
        public const string InputName = "iothub-inputname";

        /// <summary>
        /// Message schema
        /// </summary>
        public const string MessageSchema = "iothub-message-schema";

        /// <summary>
        /// Creation time of message
        /// </summary>
        public const string CreationTimeUtc = "iothub-creation-time-utc";

        /// <summary>
        /// Content encoding of message
        /// </summary>
        public const string ContentEncoding = "iothub-content-encoding";

        /// <summary>
        /// Content type of message
        /// </summary>
        public const string ContentType = "iothub-content-type";

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

        /// <summary>
        /// Diagnostics id
        /// </summary>
        public const string DiagId = "iothub-diag-id";

        /// <summary>
        /// Diagnostics context
        /// </summary>
        public const string DiagCorrelationContext = "diag-correlation-context";

        /// <summary>
        /// Interface id
        /// </summary>
        public const string InterfaceId = "iothub-interface-id";
    }
}
