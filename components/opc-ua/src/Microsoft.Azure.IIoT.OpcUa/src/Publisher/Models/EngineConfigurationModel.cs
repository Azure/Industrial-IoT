// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Engine configuration
    /// </summary>
    public class EngineConfigurationModel {

        /// <summary>
        /// Batch buffer size
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Diagnostics setting
        /// </summary>
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <summary>
        /// IoT Hub Maximum message size
        /// </summary>
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// Diagnostics setting
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// Define the maximum number of messages in outgress buffer,
        /// Default: 4096 messages with 256KB ends up in 1 GB memory consumed.
        /// </summary>
        public int? MaxOutgressMessages { get; set; }

        /// <summary>
        /// Flag to determine if a telemetry routing info is required.
        /// </summary>
        public bool EnableRoutingInfo { get; set; }

        /// <summary>
        /// Enforce strict standards compliant encoding for pub sub messages
        /// </summary>
        public bool UseStandardsCompliantEncoding { get; set; }
    }
}
