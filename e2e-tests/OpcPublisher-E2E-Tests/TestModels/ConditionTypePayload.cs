// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestModels
{
    using Newtonsoft.Json;
    using System;

    /// <summary>Payload for conditions and alarms.</summary>
    public class ConditionTypePayload : BaseEventTypePayload
    {
        /// <summary>Gets or sets Condition Id.</summary>
        /// <example>"http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#i=1"</example>
        [JsonProperty(nameof(ConditionId))]
        public DataValueObject<string> ConditionId { get; set; }

        /// <summary>Gets or sets comment source timestamp.</summary>
        [JsonProperty(nameof(Comment))]
        public DataValueObject<string> Comment { get; set; }

        /// <summary>Gets or sets the condition name.</summary>
        /// <example>"VendingMachine1_DoorOpen"</example>
        [JsonProperty(nameof(ConditionName))]
        public DataValueObject<string> ConditionName { get; set; }

        /// <summary>Gets or sets the enabled state.</summary>
        /// <example>"Enabled"</example>
        [JsonProperty(nameof(EnabledState))]
        public DataValueObject<string> EnabledState { get; set; }

        /// <summary>Gets or sets the enabled state effective display name.</summary>
        /// <example>"Active | Unacknowledged"</example>
        [JsonProperty("EnabledState/EffectiveDisplayName")]
        public DataValueObject<string> EnabledStateEffectiveDisplayName { get; set; }

        /// <summary>Gets or sets the enabled state effective transition time.</summary>
        [JsonProperty("EnabledState/EffectiveTransitionTime")]
        public DataValueObject<DateTime?> EnabledStateEffectiveTransitionTime { get; set; }

        /// <summary>Gets or sets the enabled state Id.</summary>
        /// <example>true</example>
        [JsonProperty("EnabledState/Id")]
        public DataValueObject<bool?> EnabledStateId { get; set; }

        /// <summary>Gets or sets the enabled state transition time.</summary>
        [JsonProperty("EnabledState/TransitionTime")]
        public DataValueObject<DateTime?> EnabledStateTransitionTime { get; set; }

        /// <summary>Gets or sets the event last severity.</summary>
        /// <example>500</example>
        [JsonProperty(nameof(LastSeverity))]
        public DataValueObject<int?> LastSeverity { get; set; }

        /// <summary>Gets or sets the quality.</summary>
        [JsonProperty(nameof(Quality))]
        public DataValueObject<string> Quality { get; set; }

        /// <summary>Gets or sets the retain flag.</summary>
        [JsonProperty(nameof(Retain))]
        public DataValueObject<bool?> Retain { get; set; }

        /// <summary>Gets or sets the time.</summary>
        [JsonProperty(nameof(Time))]
        public DataValueObject<DateTime?> Time { get; set; }
    }
}
