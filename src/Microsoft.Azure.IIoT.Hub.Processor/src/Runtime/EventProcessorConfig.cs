// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Runtime {
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Event processor configuration - wraps a configuration root
    /// </summary>
    public class EventProcessorConfig : StorageConfig, IEventProcessorConfig {

        /// <summary>
        /// Event processor configuration
        /// </summary>
        private const string kReceiveBatchSizeKey = "ReceiveBatchSize";
        private const string kReceiveTimeoutKey = "ReceiveTimeout";
        private const string kNamespaceKey = "StorageNamespace";
        private const string kLeaseContainerNameKey = "LeaseContainerName";

        /// <summary> Checkpoint storage </summary>
        public string LeaseContainerName => GetStringOrDefault(kLeaseContainerNameKey, null);
        /// <summary> Receive batch size </summary>
        public int ReceiveBatchSize =>
            GetIntOrDefault(kReceiveBatchSizeKey, 999);
        /// <summary> Receive timeout </summary>
        public TimeSpan ReceiveTimeout =>
            GetDurationOrDefault(kReceiveTimeoutKey, TimeSpan.FromSeconds(5));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public EventProcessorConfig(IConfigurationRoot configuration, string serviceId = "") :
            base(configuration) {
            _serviceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
