// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System;

    /// <summary>Event data with IoT Hub message enqueued time metadata.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public class EventData<T> where T : BaseEventTypePayload {
        /// <summary>IoT Hub message enqueued time.</summary>
        public DateTime EnqueuedTime { get; set; }

        /// <summary>Message origin host.</summary>
        public string PublisherId { get; set; }

        /// <summary>Payload</summary>
        public T Payload { get; set; }
    }
}