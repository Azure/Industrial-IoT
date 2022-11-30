// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class PendingAlarmMessages<T> where T : BaseEventTypePayload {
        [JsonProperty("i=2253")]
        public List<T> PendingMessages { get; set; }
    }

    public class PendingAlarmEventData<T> where T : BaseEventTypePayload {
        public bool IsPayloadCompressed { get; set; }

        public PendingAlarmMessages<T> Messages { get; set; }
    }

    /// <summary>Event data with IoT Hub message enqueued time metadata.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public class EventData<T> where T : BaseEventTypePayload {
        /// <summary>IoT Hub message enqueued time.</summary>
        public DateTime EnqueuedTime { get; set; }

        /// <summary>Message origin host.</summary>
        public string PublisherId { get; set; }

        /// <summary>Messages.</summary>
        public PubSubMessages<T> Messages { get; set; }
    }

    /// <summary>Message sent by Publisher to IoT Hub.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public class PubSubMessages<T> : Dictionary<string, T> where T : BaseEventTypePayload {
    }

    /// <summary>Container for payload.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public class PubSubMessage<T> {
        /// <summary>Message payload.</summary>
        public T Value { get; set; }

        /// <summary>Source timestamp.</summary>
        public DateTime SourceTimestamp { get; set; }
    }

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

        /// <summary>Gets or sets the time.</summary>
        public DateTime? Time { get; set; }

    }

    /// <summary>Base class for system events types.</summary>
    public class SystemEventTypePayload : BaseEventTypePayload {
    }

    /// <summary>Payload for conditions and alarms.</summary>
    public class ConditionTypePayload : BaseEventTypePayload {
        /// <summary>Gets or sets Condition Id.</summary>
        /// <example>"http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#i=1"</example>
        public string ConditionId { get; set; }

        /// <summary>Gets or sets comment source timestamp.</summary>
        [JsonProperty("Comment/SourceTimestamp")]
        public DateTime? CommentSourceTimestamp { get; set; }

        /// <summary>Gets or sets the condition name.</summary>
        /// <example>"VendingMachine1_DoorOpen"</example>
        public string ConditionName { get; set; }

        /// <summary>Gets or sets the enabled state.</summary>
        /// <example>"Enabled"</example>
        public string EnabledState { get; set; }

        /// <summary>Gets or sets the enabled state effective display name.</summary>
        /// <example>"Active | Unacknowledged"</example>
        [JsonProperty("EnabledState/EffectiveDisplayName")]
        public string EnabledStateEffectiveDisplayName { get; set; }

        /// <summary>Gets or sets the enabled state effective transition time.</summary>
        [JsonProperty("EnabledState/EffectiveTransitionTime")]
        public DateTime? EnabledStateEffectiveTransitionTime { get; set; }

        /// <summary>Gets or sets the enabled state Id.</summary>
        /// <example>true</example>
        [JsonProperty("EnabledState/Id")]
        public bool EnabledStateId { get; set; }

        /// <summary>Gets or sets the enabled state transition time.</summary>
        [JsonProperty("EnabledState/TransitionTime")]
        public DateTime? EnabledStateTransitionTime { get; set; }

        /// <summary>Gets or sets the event last severity.</summary>
        /// <example>500</example>
        public int LastSeverity { get; set; }

        /// <summary>Gets or sets the event last severity source timestamp.</summary>
        [JsonProperty("LastSeverity/SourceTimestamp")]
        public DateTime? LastSeveritySourceTimestamp { get; set; }

        /// <summary>Gets or sets the quality.</summary>
        public string Quality { get; set; }

        /// <summary>Gets or sets the quality source timestamp.</summary>
        [JsonProperty("Quality/SourceTimestamp")]
        public DateTime? QualitySourceTimestamp { get; set; }

        /// <summary>Gets or sets the retain flag.</summary>
        public bool Retain { get; set; }
    }

    /// <summary>Payload for simple events.</summary>
    public class SystemCycleStatusEventTypePayload : SystemEventTypePayload {
        /// <summary>Gets or sets the cycle id.</summary>
        /// <example>"29813"</example>
        [JsonProperty("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId")]
        public string CycleId { get; set; }

        /// <summary>Gets or sets the current step.</summary>
        [JsonProperty("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep")]
        public SimpleEventsStep CurrentStep { get; set; }
    }

    /// <summary>Simple event step.</summary>
    public class SimpleEventsStep {
        /// <summary>Gets or sets the step name.</summary>
        /// <example>"Step 1"</example>
        public string Name { get; set; }

        /// <summary>Gets or sets the step duration in milliseconds.</summary>
        /// <example>1000.0</example>
        public double Duration { get; set; }
    }
}