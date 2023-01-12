// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using Newtonsoft.Json;
    using System;

    /// <summary>Base class for payload types.</summary>
    public class BaseEventTypePayload {
        /// <summary>Gets or sets event id.</summary>
        /// <example>"V1_DoorOpen(294)" represented as JSON base-64 encoded string "VjFfRG9vck9wZW4oMjk0KQ==".</example>
        [JsonProperty("EventId/Value")]
        public DataValueObject<byte[]> EventId { get; set; }

        /// <summary>Gets or sets message.</summary>
        /// <example>"The system cycle '29813' has started."</example>
        [JsonProperty("Message/Value")]
        public DataValueObject<string> Message { get; set; }

        /// <summary>Gets or sets the severity.</summary>
        /// <example>900</example>
        [JsonProperty("Severity/Value")]
        public DataValueObject<int> Severity { get; set; }

        /// <summary>Gets or sets the source name.</summary>
        /// <example>"VendingMachine1"</example>
        [JsonProperty("SourceName/Value")]
        public DataValueObject<string> SourceName { get; set; }

        /// <summary>Gets or sets the source node.</summary>
        /// <example>"http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1"</example>
        [JsonProperty("SourceNode/Value")]
        public DataValueObject<string> SourceNode { get; set; }

        /// <summary>Gets or sets the event type.</summary>
        /// <example>"i=10751"</example>
        [JsonProperty("EventType/Value")]
        public DataValueObject<string> EventType { get; set; }

        /// <summary>Gets or sets the receive time.</summary>
        [JsonProperty("ReceiveTime/Value")]
        public DataValueObject<DateTime?> ReceiveTime { get; set; }

        /// <summary>Gets or sets the local time.</summary>
        [JsonProperty("LocalTime/Value")]
        public DataValueObject<DateTime?> LocalTime { get; set; }
    }
}