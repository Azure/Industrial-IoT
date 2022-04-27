namespace MqttTestValidator.BusinessLogic {
    using MqttTestValidator.Interfaces;
    using MqttTestValidator.Models;
    using System;

    internal class VerificationTaskFactory : IVerificationTaskFactory {
        private readonly ILogger<IVerificationTask> _logger;
        public VerificationTaskFactory(ILogger<IVerificationTask> logger)
        {
            _logger = logger;
        }

        private static ulong _instanceCounter = 0;
        public IVerificationTask CreateVerificationTask(MqttVerificationRequest verificationRequest) {
            _instanceCounter++;
            return new VerificationTask(
                _instanceCounter, 
                verificationRequest.MqttBroker,
                verificationRequest.MqttPort,
                verificationRequest.MqttTopic,
                verificationRequest.StartupTime,
                verificationRequest.TimeToObserve, 
                _logger);
        }
    }
}
