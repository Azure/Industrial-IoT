// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor {
    using Microsoft.Azure.IIoT.Storage.Datalake;
    using System;

    /// <summary>
    /// Eventprocessor host configuration
    /// </summary>
    public interface IEventProcessorHostConfig : IBlobConfig {

        /// <summary>
        /// Receive batch size
        /// </summary>
        int ReceiveBatchSize { get; }

        /// <summary>
        /// Receive timeout
        /// </summary>
        TimeSpan ReceiveTimeout { get; }

        /// <summary>
        /// Whether to read from end or start.
        /// </summary>
        bool InitialReadFromEnd { get; }

        /// <summary>
        /// And lease container name.
        /// </summary>
        string LeaseContainerName { get; }
    }
}
