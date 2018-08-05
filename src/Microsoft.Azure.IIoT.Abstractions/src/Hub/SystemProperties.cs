// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// IoT hub Message system property names
    /// </summary>
    public static class SystemProperties {

        public const string MessageId = "message-id";
        public const string LockToken = "iothub-messagelocktoken";
        public const string SequenceNumber = "sequence-number";
        public const string To = "to";
        public const string EnqueuedTime = "iothub-enqueuedtime";
        public const string ExpiryTimeUtc = "absolute-expiry-time";
        public const string CorrelationId = "correlation-id";
        public const string DeliveryCount = "iothub-deliverycount";
        public const string UserId = "user-id";
        public const string Operation = "iothub-operation";
        public const string Ack = "iothub-ack";
        public const string OutputName = "iothub-outputname";
        public const string InputName = "iothub-inputname";
        public const string MessageSchema = "iothub-message-schema";
        public const string CreationTimeUtc = "iothub-creation-time-utc";
        public const string ContentEncoding = "iothub-content-encoding";
        public const string ContentType = "iothub-content-type";
        public const string ConnectionDeviceId = "iothub-connection-device-id";
        public const string ConnectionModuleId = "iothub-connection-module-id";
        public const string DiagId = "iothub-diag-id";
        public const string DiagCorrelationContext = "diag-correlation-context";

    }
}
