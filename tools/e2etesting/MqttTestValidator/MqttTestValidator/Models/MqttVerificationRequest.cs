namespace MqttTestValidator.Models {
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.InteropServices;

    public class MqttVerificationRequest {
        /// <summary>
        /// The IP or DNS name of MQTT V5 broker to connect to
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string MqttBroker { get; set; } = string.Empty;
        /// <summary>
        /// The Port of the MQTT V5 broker to connect to
        /// </summary>
        [Range(1, 65535)]   
        public uint MqttPort { get; set; } = 1883;
        /// <summary>
        /// The topic to observe
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string MqttTopic { get; set; } = string.Empty;
        /// <summary>
        /// Timespan to wait for messages
        /// </summary>
        public TimeSpan TimeToObserve { get; set; } = TimeSpan.FromSeconds(30);
        /// <summary>
        /// Time to wait befor listening to messages
        /// </summary>
        public TimeSpan StartupTime { get; set; } = TimeSpan.FromSeconds(5);
    }
}
