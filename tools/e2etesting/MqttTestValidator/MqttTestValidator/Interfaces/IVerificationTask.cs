// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Interfaces {
    using MqttTestValidator.Models;

    public interface IVerificationTask {
        /// <summary>
        /// Unique identifier of the verification task
        /// </summary>
        ulong Id { get; set; }
        /// <summary>
        /// Begin asynchronous verification
        /// </summary>
        void Start();
        /// <summary>
        /// Returns the verification results
        /// </summary>
        MqttVerificationDetailedResponse GetResult();
    }
}
