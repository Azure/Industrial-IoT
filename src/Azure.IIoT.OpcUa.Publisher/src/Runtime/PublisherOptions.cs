// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public sealed class PublisherOptions
    {
        /// <summary>
        /// Site of the publisher
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// Configuration file
        /// </summary>
        public string PublishedNodesFile { get; set; }

        /// <summary>
        /// Max number of nodes per publish endpoint
        /// </summary>
        public int MaxNodesPerPublishedEndpoint { get; set; }

        /// <summary>
        /// Messaging profile to use
        /// </summary>
        public MessagingProfile MessagingProfile { get; set; }

        /// <summary>
        /// Batch size
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Batch Trigger Interval
        /// </summary>
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <summary>
        /// Maximum mesage size for the encoded messages
        /// typically the IoT Hub's mas D2C message size
        /// </summary>
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// Diagnostics interval
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// Define the maximum number of messages in outgress buffer,
        /// Default: 4096 messages with 256KB ends up in 1 GB memory consumed.
        /// </summary>
        public int? MaxOutgressMessages { get; set; }

        /// <summary>
        /// Flag to use reversible encoding for messages
        /// </summary>
        public bool? UseStandardsCompliantEncoding { get; set; }

        /// <summary>
        /// Default metadata queue name
        /// </summary>
        public string DefaultMetaDataQueueName { get; set; }

        /// <summary>
        /// Default Max messages per publish
        /// </summary>
        public uint? DefaultMaxMessagesPerPublish { get; set; }

        /// <summary>
        /// Configuration flag for enabling/disabling
        /// runtime state reporting.
        /// </summary>
        public bool? EnableRuntimeStateReporting { get; set; }

        /// <summary>
        /// The routing info to add to the runtime state
        /// events.
        /// </summary>
        public string RuntimeStateRoutingInfo { get; set; }

        /// <summary>
        /// Scale test option
        /// </summary>
        public int? ScaleTestCount { get; set; }
    }
}
