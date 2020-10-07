// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using System.Threading.Tasks;

    /// <summary>
    /// Worker supervisor
    /// </summary>
    public interface IWorkerSupervisor : IHostProcess {

        /// <summary>
        /// The amount of workers currently running
        /// </summary>
        int NumberOfWorkers { get; }
    }
}