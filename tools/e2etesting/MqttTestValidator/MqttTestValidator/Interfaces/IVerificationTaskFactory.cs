// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Interfaces {
    using MqttTestValidator.Models;

    public interface IVerificationTaskFactory {
        /// <summary>
        /// Creates a new Verification Task
        /// </summary>
        IVerificationTask CreateVerificationTask(MqttVerificationRequest verificationRequest);
    }
}
