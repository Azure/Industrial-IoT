// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    public class PendingConditionEventData<T> where T : BaseEventTypePayload {
        public bool IsPayloadCompressed { get; set; }

        public T Payload { get; set; }
    }
}