// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestModels
{
    using Newtonsoft.Json;
    using System;

    /// <summary>Base class for payload types.</summary>
    public class BaseEventTypePayload
    {
        /// <summary>Gets or sets event id.</summary>
        /// <example>"V1_DoorOpen(294)" represented as JSON base-64 encoded string "VjFfRG9vck9wZW4oMjk0KQ==".</example>
        [JsonProperty(nameof(EventId))]
        public DataValueObject<byte[]> EventId { get; set; }

        /// <summary>Gets or sets message.</summary>
        /// <example>"The system cycle '29813' has started."</example>
        [JsonProperty(nameof(Message))]
        public DataValueObject<string> Message { get; set; }

        /// <summary>Gets or sets the severity.</summary>
        /// <example>900</example>
        [JsonProperty(nameof(Severity))]
        public DataValueObject<int> Severity { get; set; }

        /// <summary>Gets or sets the source name.</summary>
        /// <example>"VendingMachine1"</example>
        [JsonProperty(nameof(SourceName))]
        public DataValueObject<string> SourceName { get; set; }

        /// <summary>Gets or sets the source node.</summary>
        /// <example>"http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1"</example>
        [JsonProperty(nameof(SourceNode))]
        public DataValueObject<string> SourceNode { get; set; }

        /// <summary>Gets or sets the event type.</summary>
        /// <example>"i=10751"</example>
        [JsonProperty(nameof(EventType))]
        public DataValueObject<string> EventType { get; set; }

        /// <summary>Gets or sets the receive time.</summary>
        [JsonProperty(nameof(ReceiveTime))]
        public DataValueObject<DateTime?> ReceiveTime { get; set; }

        /// <summary>Gets or sets the local time.</summary>
        [JsonProperty(nameof(LocalTime))]
        public DataValueObject<DateTime?> LocalTime { get; set; }
    }
}