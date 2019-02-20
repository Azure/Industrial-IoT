// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor {
    using Microsoft.Azure.IIoT.Storage.Blob;
    using System;

    /// <summary>
    /// Eventprocessor configuration
    /// </summary>
    public interface IEventProcessorConfig : IStorageConfig {

        /// <summary>
        /// Receive batch size
        /// </summary>
        int ReceiveBatchSize { get; }

        /// <summary>
        /// Receive timeout
        /// </summary>
        TimeSpan ReceiveTimeout { get; }

        /// <summary>
        /// And lease container name. If null, use other means.
        /// </summary>
        string LeaseContainerName { get; }
    }
}
