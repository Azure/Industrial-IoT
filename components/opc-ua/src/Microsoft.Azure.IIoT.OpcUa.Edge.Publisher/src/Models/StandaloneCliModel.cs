// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Model that represents the command line arguments in the format of the
    /// standalone publisher mode.
    /// </summary>
    public class StandaloneCliModel {

        /// <summary>
        /// The published nodes file.
        /// </summary>
        public string PublishedNodesFile { get; set; }

        /// <summary>
        /// The published nodes schema file.
        /// </summary>
        public string PublishedNodesSchemaFile { get; set; }

        /// <summary>
        /// The default interval for heartbeats if not set on
        /// node level.
        /// </summary>
        public TimeSpan? DefaultHeartbeatInterval { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The default flag whether to skip the first value if
        /// not set on node level.
        /// </summary>
        public bool DefaultSkipFirst { get; set; } = false;

        /// <summary>
        /// The default sampling interval.
        /// </summary>
        public TimeSpan? DefaultSamplingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        public TimeSpan? DefaultPublishingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Flag wether to grab the display name of nodes form
        /// the OPC UA Server.
        /// </summary>
        public bool FetchOpcNodeDisplayName { get; set; } = false;

        /// <summary>
        /// set the default queue size for monitored items
        /// </summary>
        public uint DefaultQueueSize { get; set; } = 1;

        /// <summary>
        /// The interval to show diagnostics information.
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The time to flush the log file to the disc.
        /// </summary>
        public TimeSpan? LogFileFlushTimeSpan { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The filename of the logfile.
        /// </summary>
        public string LogFilename { get; set; }

        /// <summary>
        /// The messaging mode for outgoing messages.
        /// </summary>
        public MessagingMode MessagingMode { get; set; } = MessagingMode.Samples;

        /// <summary>
        /// The messaging mode for outgoing messages.
        /// </summary>
        public MessageEncoding MessageEncoding { get; set; } = MessageEncoding.Json;

        /// <summary>
        /// Flag to demand full featured message creation from publisher
        /// </summary>
        public bool FullFeaturedMessage { get; set; } = false;

        /// <summary>
        /// The operation timeout.
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// The size of the message batching buffer
        /// </summary>
        public int? BatchSize { get; set; } = 50;

        /// <summary>
        /// The interval to trigger batching
        /// </summary>
        public TimeSpan? BatchTriggerInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The maximum size of the (IoT D2C ) message
        /// </summary>
        public int? MaxMessageSize { get; set; } = 0;

        /// <summary>
        /// force mass monitored item clones
        /// </summary>
        public int? ScaleTestCount { get; set; } = 1;

        /// <summary>
        /// Define the maximum amount of the IoT D2C messages
        /// </summary>
        public int? MaxOutgressMessages { get; set; } = 4096;

        /// <summary>
        /// Maximum number of nodes within a DataSet/Subscription. When more nodes are configured
        /// for a dataSetWriter, they will be added in a different DataSet/Subscription.
        /// </summary>
        public int? MaxNodesPerDataSet { get; set; } = 1000;

        /// <summary>
        /// Run in legacy compatibility mode
        /// </summary>
        public bool LegacyCompatibility { get; set; } = false;

        /// <summary>
        /// Configuration flag for enabling/disabling runtime state reporting.
        /// </summary>
        public bool EnableRuntimeStateReporting { get; set; } = false;
    }
}
