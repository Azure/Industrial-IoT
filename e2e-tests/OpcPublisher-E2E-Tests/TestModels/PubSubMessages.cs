// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System.Collections.Generic;

    /// <summary>Message sent by Publisher to IoT Hub.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public class PubSubMessages<T> : Dictionary<string, T> where T : BaseEventTypePayload {
    }
}