// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {

    /// <summary>
    /// Configuration for task processor
    /// </summary>
    public interface ITaskProcessorConfig {

        /// <summary>
        /// Max instances of processors that should run.
        /// </summary>
        int MaxInstances { get; }

        /// <summary>
        /// Max queue size per processor
        /// </summary>
        int MaxQueueSize { get; }
    }
}
