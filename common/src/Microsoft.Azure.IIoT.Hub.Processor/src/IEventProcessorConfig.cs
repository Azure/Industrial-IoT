// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor {
    using Microsoft.Azure.IIoT.Storage.Datalake;
    using System;

    /// <summary>
    /// Eventprocessor configuration
    /// </summary>
    public interface IEventProcessorConfig : IBlobConfig {

        /// <summary>
        /// Set checkpoint interval. null = never.
        /// </summary>
        TimeSpan? CheckpointInterval { get; }

        /// <summary>
        /// Skip all events older than. null = never.
        /// </summary>
        TimeSpan? SkipEventsOlderThan { get; }
    }
}
