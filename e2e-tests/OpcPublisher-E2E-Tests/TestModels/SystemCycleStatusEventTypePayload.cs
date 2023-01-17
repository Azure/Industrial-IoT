// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using Newtonsoft.Json;

    /// <summary>Payload for simple events.</summary>
    public class SystemCycleStatusEventTypePayload : SystemEventTypePayload {
        /// <summary>Gets or sets the cycle id.</summary>
        /// <example>"29813"</example>
        [JsonProperty("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId")]
        public DataValueObject<string> CycleId { get; set; }

        /// <summary>Gets or sets the current step.</summary>
        [JsonProperty("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep")]
        public DataValueObject<SimpleEventsStep> CurrentStep { get; set; }
    }
}