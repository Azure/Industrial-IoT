// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {

    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Configuration of defaults for job definition generation logic for OPC Publisher module.
    /// </summary>
    public class PublishServicesConfig : ConfigBase, IPublishServicesConfig {
        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kDefaultBatchTriggerInterval = "Publisher:DefaultBatchTriggerInterval";
        private const string kDefaultBatchSize = "Publisher:DefaultBatchSize";
        private const string kDefaultMaxEgressMessageQueue = "Publisher:DefaultMaxEgressMessageQueue";
        private const string kDefaultMessagingMode = "Publisher:DefaultMessagingMode";
        private const string kDefaultMessageEncoding = "Publisher:DefaultMessageEncoding";

        /// <summary>
        /// Default configuration values
        /// </summary>
        private readonly TimeSpan _defaultBatchTriggerInterval = TimeSpan.FromMilliseconds(500);
        private readonly int _DefaultBatchSize = 50;
        private readonly int _DefaultMaxEgressMessageQueue = 4096; // Default (4096 * 256 KB = 1 GB).
        private readonly string _DefaultMessagingMode = MessagingMode.Samples.ToString();
        private readonly string _DefaultMessageEncoding = MessageEncoding.Json.ToString();

        /// <inheritdoc/>
        public TimeSpan DefaultBatchTriggerInterval {
            get {
                var batchInterval = GetDurationOrDefault(kDefaultBatchTriggerInterval,
                    () => GetDurationOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_JOB_BATCH_INTERVAL,
                    () => _defaultBatchTriggerInterval));

                if (batchInterval.TotalMilliseconds >= 100 && batchInterval.TotalMilliseconds <= 3600000) {
                    return batchInterval;
                }
                else {
                    _logger?.Warning($"DefaultBatchTriggerInterval: Provided value {batchInterval} should be >= 100 and <= 3600000. Defaulting to {_defaultBatchTriggerInterval.TotalMilliseconds}.");
                }

                return _defaultBatchTriggerInterval;
            }
        }

        /// <inheritdoc/>
        public int DefaultBatchSize {
            get {
                var batchSize = GetIntOrDefault(kDefaultBatchSize,
                    () => GetIntOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_JOB_BATCH_SIZE,
                    () => _DefaultBatchSize));

                if (batchSize > 1 && batchSize <= 1000) {
                    return batchSize;
                }
                else {
                    _logger?.Warning($"DefaultBatchSize: Provided value {batchSize} should be > 1 and <= 1000. Defaulting to {_DefaultBatchSize}.");
                }

                return _DefaultBatchSize;
            }
        }

        /// <inheritdoc/>
        public int DefaultMaxEgressMessageQueue {
            get {
                var queueSize = GetIntOrDefault(kDefaultMaxEgressMessageQueue,
                    () => GetIntOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_MAX_EGRESS_MESSAGE_QUEUE,
                    () => -1));

                if (queueSize == -1) {
                    // Fallback to deprecated option,
                    // use PCS_DEFAULT_PUBLISH_MAX_EGRESS_MESSAGE_QUEUE instead.
                    queueSize = GetIntOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES,
                        () => _DefaultMaxEgressMessageQueue);
                }

                if (queueSize > 1 && queueSize <= 25000) {
                    return queueSize;
                }
                else {
                    _logger?.Warning($"DefaultMaxEgressMessageQueue: Provided value {queueSize} should be > 1 and <= 25000. Defaulting to {_DefaultMaxEgressMessageQueue}.");
                }

                return _DefaultMaxEgressMessageQueue;
            }
        }

        /// <inheritdoc/>
        public MessagingMode DefaultMessagingMode {
            get {
                var modeStr = GetStringOrDefault(kDefaultMessagingMode,
                    () => GetStringOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_MESSAGING_MODE,
                    () => _DefaultMessagingMode));

                var mode = (MessagingMode)Enum.Parse(typeof(MessagingMode), modeStr);
                return mode;
            }
        }

        /// <inheritdoc/>
        public MessageEncoding DefaultMessageEncoding {
            get {
                var encodingStr = GetStringOrDefault(kDefaultMessageEncoding,
                    () => GetStringOrDefault(PcsVariable.PCS_DEFAULT_PUBLISH_MESSAGE_ENCODING,
                    () => _DefaultMessageEncoding));

                var encoding = (MessageEncoding)Enum.Parse(typeof(MessageEncoding), encodingStr);
                return encoding;
            }
        }

        /// <summary>
        /// Create PublishServicesConfig
        /// </summary>
        /// <param name="configuration"></param>
        public PublishServicesConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
