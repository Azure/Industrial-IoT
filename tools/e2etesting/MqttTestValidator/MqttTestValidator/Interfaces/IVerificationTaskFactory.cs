namespace MqttTestValidator.Interfaces {
    using MqttTestValidator.Models;

    internal interface IVerificationTaskFactory {
        /// <summary>
        /// Creates a new Verification Task
        /// </summary>
        IVerificationTask CreateVerificationTask(MqttVerificationRequest verificationRequest);
    }
}
