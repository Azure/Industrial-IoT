// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Models {
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    public class MqttVerificationRequest {
        /// <summary>
        /// The IP or DNS name of MQTT V5 broker to connect to
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [DefaultValue("localhost")]
        public string MqttBroker { get; set; } = string.Empty;
        /// <summary>
        /// The Port of the MQTT V5 broker to connect to
        /// </summary>
        [Range(1, 65535)]
        [DefaultValue(1883)]
        public uint MqttPort { get; set; } = 1883;
        /// <summary>
        /// The topic to observe
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [DefaultValue("devices/#")]
        public string MqttTopic { get; set; } = string.Empty;
        /// <summary>
        /// Time to wait for messages in milliseconds
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(30_000)]
        public int TimeToObserve { get; set; } = 30_000;
        /// <summary>
        /// Time to wait befor listening to messages
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(5_000)]
        public int StartupTime { get; set; } = 5_000;
    }
}
