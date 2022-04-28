// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.BusinessLogic {
    using MqttTestValidator.Interfaces;
    using MqttTestValidator.Models;

    internal class VerificationTaskFactory : IVerificationTaskFactory {
        private readonly ILogger<IVerificationTask> _logger;
        private static ulong _instanceCounter = 0;

        public VerificationTaskFactory(ILogger<IVerificationTask> logger) {
            _logger = logger;
        }

        /// <inheritdoc />
        public IVerificationTask CreateVerificationTask(MqttVerificationRequest verificationRequest) {
            _instanceCounter++;
            return new VerificationTask(
                _instanceCounter, 
                verificationRequest.MqttBroker,
                Convert.ToInt32(verificationRequest.MqttPort),
                verificationRequest.MqttTopic,
                TimeSpan.FromMilliseconds(verificationRequest.StartupTime),
                TimeSpan.FromMilliseconds(verificationRequest.TimeToObserve), 
                _logger);
        }
    }
}
