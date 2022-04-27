namespace MqttTestValidator.Interfaces {
    using MqttTestValidator.Models;

    internal interface IVerificationTask {
        ulong Id { get; set; }

        void Start();
        MqttVerificationDetailedResponse GetResult();
    }
}
