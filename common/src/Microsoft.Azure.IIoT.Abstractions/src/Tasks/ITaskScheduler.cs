// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable task scheduler implementations
    /// </summary>
    public interface ITaskScheduler {

        /// <summary>
        /// Task factory
        /// </summary>
        TaskFactory Factory { get; }

        /// <summary>
        /// Create debug dump
        /// </summary>
        void Dump(Action<Task> logger);
    }
}
