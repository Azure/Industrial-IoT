// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using System.Threading.Tasks;

    public interface ITaskScheduler {

        /// <summary>
        /// Task factory
        /// </summary>
        TaskFactory Factory { get; }

        /// <summary>
        /// Create debug dump
        /// </summary>
        void Dump(ILogger logger);
    }
}