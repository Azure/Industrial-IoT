// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher processing engine configuration
    /// </summary>
    [DataContract]
    public record class EngineConfigurationModel {

        /// <summary>
        /// Number of subscription notifications that
		/// when exceeded trigger the sending of a new
		/// message.
        /// </summary>
        [DataMember(Name = "batchSize", Order = 0,
            EmitDefaultValue = false)]
        public int? BatchSize { get; set; }

        /// <summary>
        /// Interval for to trigger sending message when
		/// batch is not yet filled to configured batch size.
        /// </summary>
        [DataMember(Name = "batchTriggerInterval", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <summary>
        /// Maximum message size
        /// </summary>
        [DataMember(Name = "maxMessageSize", Order = 3,
            EmitDefaultValue = false)]
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// Define the maximum number of messages in outgress buffer,
        /// Default: 4096 messages with 256KB ends up in 1 GB
        /// memory consumed.
        /// </summary>
        [DataMember(Name = "maxOutgressMessages", Order = 4,
            EmitDefaultValue = false)]
        public int? MaxOutgressMessages { get; set; }

        /// <summary>
        /// Enforce strict standards compliant encoding
        /// for pub sub messages
        /// </summary>
        [DataMember(Name = "useStandardsCompliantEncoding", Order = 6,
            EmitDefaultValue = false)]
        public bool UseStandardsCompliantEncoding { get; set; }

        /// <summary>
        /// Default queue name
        /// </summary>
        [DataMember(Name = "defaultMetaDataQueueName", Order = 7,
            EmitDefaultValue = false)]
        public string DefaultMetaDataQueueName { get; set; }

        /// <summary>
        /// Default max messages per publish operation
        /// </summary>
        [DataMember(Name = "defaultMaxMessagesPerPublish", Order = 8,
            EmitDefaultValue = false)]
        public uint? DefaultMaxMessagesPerPublish { get; set; }
    }
}
