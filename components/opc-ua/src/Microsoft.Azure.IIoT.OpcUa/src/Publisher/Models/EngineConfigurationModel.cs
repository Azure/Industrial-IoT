﻿// ------------------------------------------------------------
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
    }
}