// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System;

    /// <summary>Base class for payload types.</summary>
    public class BaseEventTypePayload {
        /// <summary>Gets or sets event id.</summary>
        /// <example>"V1_DoorOpen(294)" represented as JSON base-64 encoded string "VjFfRG9vck9wZW4oMjk0KQ==".</example>
        public byte[] EventId { get; set; }

        /// <summary>Gets or sets message.</summary>
        /// <example>"The system cycle '29813' has started."</example>
        public string Message { get; set; }

        /// <summary>Gets or sets the severity.</summary>
        /// <example>900</example>
        public int Severity { get; set; }

        /// <summary>Gets or sets the source name.</summary>
        /// <example>"VendingMachine1"</example>
        public string SourceName { get; set; }

        /// <summary>Gets or sets the source node.</summary>
        /// <example>"http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1"</example>
        public string SourceNode { get; set; }

        /// <summary>Gets or sets the event type.</summary>
        /// <example>"i=10751"</example>
        public string EventType { get; set; }

        /// <summary>Gets or sets the receive time.</summary>
        public DateTime? ReceiveTime { get; set; }

        /// <summary>Gets or sets the local time.</summary>
        public DateTime? LocalTime { get; set; }
    }
}