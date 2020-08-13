// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using System;

    /// <summary>
    /// Engine configuration
    /// </summary>
    public interface IEngineConfiguration {

        /// <summary>
        /// Batch size
        /// </summary>
        int? BatchSize { get; }

        /// <summary>
        /// Batch Trigger Interval
        /// </summary>
        TimeSpan? BatchTriggerInterval { get; }

        /// <summary>
        /// Maximum mesage size for the encoded messages
        /// typically the IoT Hub's mas D2C message size
        /// </summary>
        int? MaxMessageSize { get; }

        /// <summary>
        /// Diagnostics interval
        /// </summary>
        TimeSpan? DiagnosticsInterval { get; }

        /// <summary>
        /// Maximum size of egress message queue
        /// </summary>
        int? MaxEgressMessageQueue { get; }
    }
}