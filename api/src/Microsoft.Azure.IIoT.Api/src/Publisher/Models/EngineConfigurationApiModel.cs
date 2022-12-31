// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher processing engine configuration
    /// </summary>
    [DataContract]
    public class EngineConfigurationApiModel {

        /// <summary>
        /// Buffer size
        /// </summary>
        [DataMember(Name = "batchSize", Order = 0,
            EmitDefaultValue = false)]
        public int? BatchSize { get; set; }

        /// <summary>
        /// Interval for diagnostic messages
        /// </summary>
        [DataMember(Name = "batchTriggerInterval", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <summary>
        /// Interval for diagnostic messages
        /// </summary>
        [DataMember(Name = "diagnosticsInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// Buffer size
        /// </summary>
        [DataMember(Name = "maxMessageSize", Order = 3,
            EmitDefaultValue = false)]
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// Max outgress message queue size
        /// </summary>
        [DataMember(Name = "maxOutgressMessages", Order = 4,
            EmitDefaultValue = false)]
        public int? MaxOutgressMessages { get; set; }

        /// <summary>
        /// Flag to determine if a telemetry routing info is enabled.
        /// </summary>
        [DataMember(Name = "enableRoutingInfo", Order = 5,
            EmitDefaultValue = false)]
        public bool EnableRoutingInfo { get; set; }

        /// <summary>
        /// Force strict UA compliant encoding for pub sub messages
        /// </summary>
        [DataMember(Name = "useStandardsCompliantEncoding", Order = 6,
            EmitDefaultValue = false)]
        public bool UseStandardsCompliantEncoding { get; set; }
    }
}
