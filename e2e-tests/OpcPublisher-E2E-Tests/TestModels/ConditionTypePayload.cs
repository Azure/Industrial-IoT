// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System;
    using Newtonsoft.Json;

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

        /// <summary>Gets or sets the time.</summary>
        public DateTime? Time { get; set; }
    }
}