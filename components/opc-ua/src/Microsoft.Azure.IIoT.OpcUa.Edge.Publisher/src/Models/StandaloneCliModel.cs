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
        public bool DefaultSkipFirst { get; set; }

        /// <summary>
        /// The default flag whether to descard new items in queue
        /// </summary>
        public bool? DefaultDiscardNew { get; set; }

        /// <summary>
        /// The default sampling interval.
        /// </summary>
        public TimeSpan? DefaultSamplingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        public TimeSpan? DefaultPublishingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default metadata send interval.
        /// </summary>
        public TimeSpan? DefaultMetaDataSendInterval { get; set; }

        /// <summary>
        /// Whether to disable metadata sending
        /// </summary>
        public bool? DisableDataSetMetaData { get; set; }

        /// <summary>
        /// Default keyframe count
        /// </summary>
        public uint? DefaultKeyFrameCount { get; set; }

        /// <summary>
        /// Flag wether to grab the display name of nodes form
        /// the OPC UA Server.
        /// </summary>
        public bool FetchOpcNodeDisplayName { get; set; }

        /// <summary>
        /// set the default queue size for monitored items. If not
        /// set the default queue size will be configured (1 for
        /// data monitored items, and 0 for event monitoring).
        /// </summary>
        public uint? DefaultQueueSize { get; set; }

        /// <summary>
        /// set the default data change filter for monitored items. Default is
        /// status and value change triggering.
        /// </summary>
        public DataChangeTriggerType? DefaultDataChangeTrigger { get; set; }

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
        public MessagingMode MessagingMode {
            get {
                // Depends on fm and standards compliant encoding mode
                var messagingMode = _messagingMode ??
                    (UseStandardsCompliantEncoding ? MessagingMode.PubSub : MessagingMode.Samples);
                if (_fullFeaturedMessage) {
                    if (messagingMode == MessagingMode.PubSub) {
                        return MessagingMode.FullNetworkMessages;
                    }
                    if (messagingMode == MessagingMode.Samples) {
                        return MessagingMode.FullSamples;
                    }
                }
                return messagingMode;
            }
            set => _messagingMode = value;
        }

        /// <summary>
        /// Set flag to demand full featured message creation from publisher
        /// </summary>
        public void SetFullFeaturedMessage(bool value) {
            _fullFeaturedMessage = value;
        }

        /// <summary>
        /// The messaging mode for outgoing messages.
        /// </summary>
        public MessageEncoding MessageEncoding { get; set; } = MessageEncoding.Json;

        /// <summary>
        /// Number of messages that trigger a batch
        /// </summary>
        public int? BatchSize { get; set; } = 100;

        /// <summary>
        /// The interval to trigger publishing
        /// </summary>
        public TimeSpan? BatchTriggerInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Max messages packed into an outgoing message
        /// </summary>
        public uint? MaxMessagesPerPublish { get; set; }

        /// <summary>
        /// The maximum size of the (IoT D2C) message
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
        /// Flag to use standards compliant encoding for pub sub messages (default to false for backcompat)
        /// </summary>
        public bool UseStandardsCompliantEncoding { get; set; }

        /// <summary>
        /// Flag to determine if a telemetry header helper is enabled. (default to false)
        /// </summary>
        public bool EnableRoutingInfo { get; set; }

        /// <summary>
        /// Maximum number of nodes within a DataSet/Subscription. When more nodes are configured
        /// for a dataSetWriter, they will be added in a different DataSet/Subscription.
        /// </summary>
        public int MaxNodesPerDataSet { get; set; } = 1000;

        /// <summary>
        /// Run in 2.5.* compatibility mode
        /// </summary>
        public bool LegacyCompatibility { get; set; }

        /// <summary>
        /// Configuration flag for enabling/disabling runtime state reporting.
        /// </summary>
        public bool EnableRuntimeStateReporting { get; set; }

        private bool _fullFeaturedMessage;
        private MessagingMode? _messagingMode;
    }
}
