// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Runtime {
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Event processor configuration - wraps a configuration root
    /// </summary>
    public class EventProcessorConfig : StorageConfig, IEventProcessorHostConfig,
        IEventProcessorConfig {

        /// <summary>
        /// Event processor configuration
        /// </summary>
        private const string kReceiveBatchSizeKey = "ReceiveBatchSize";
        private const string kReceiveTimeoutKey = "ReceiveTimeout";
        private const string kLeaseContainerNameKey = "LeaseContainerName";
        private const string kInitialReadFromEnd = "InitialReadFromEnd";
        private const string kCheckpointIntervalKey = "CheckpointIntervalKey";

        /// <summary> Checkpoint storage </summary>
        public string LeaseContainerName => GetStringOrDefault(kLeaseContainerNameKey,
            () => null);
        /// <summary> Receive batch size </summary>
        public int ReceiveBatchSize => GetIntOrDefault(kReceiveBatchSizeKey,
            () => 999);
        /// <summary> Receive timeout </summary>
        public TimeSpan ReceiveTimeout => GetDurationOrDefault(kReceiveTimeoutKey,
            () => TimeSpan.FromSeconds(5));
        /// <summary> Read from end </summary>
        public bool InitialReadFromEnd => GetBoolOrDefault(kInitialReadFromEnd,
            () => false);
        /// <summary> Checkpoint timer </summary>
        public TimeSpan? CheckpointInterval => GetDurationOrDefault(kCheckpointIntervalKey,
            () => TimeSpan.FromSeconds(10));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public EventProcessorConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
