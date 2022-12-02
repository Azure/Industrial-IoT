// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class PendingAlarmMessages<T> where T : BaseEventTypePayload {
        [JsonProperty("i=2253")]
        public List<T> PendingMessages { get; set; }
    }
}