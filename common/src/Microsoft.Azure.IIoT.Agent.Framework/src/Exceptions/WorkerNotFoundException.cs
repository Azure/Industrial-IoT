// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Worker not found
    /// </summary>
    public class WorkerSupervisorNotFoundException : ResourceNotFoundException {

        /// <inheritdoc/>
        public WorkerSupervisorNotFoundException(string workerSupervisorId) :
            base($"Workersupervisor with id '{workerSupervisorId}' could not be found.") {
        }
    }
}