// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher {
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
        /// Define the maximum number of messages in outgress buffer,
        /// Default: 4096 messages with 256KB ends up in 1 GB memory consumed.
        /// </summary>
        int? MaxOutgressMessages { get; }

        /// <summary>
        /// Flag to use reversible encoding for messages
        /// </summary>
        bool UseStandardsCompliantEncoding { get; }

        /// <summary>
        /// Default metadata queue name
        /// </summary>
        string DefaultMetaDataQueueName { get; }

        /// <summary>
        /// Default Max messages per publish
        /// </summary>
        uint? DefaultMaxMessagesPerPublish { get; }
    }
}
