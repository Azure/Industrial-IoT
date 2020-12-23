// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Maximum worker reacher
    /// </summary>
    public class MaxWorkersReachedException : ResourceExhaustionException {

        /// <inheritdoc/>
        public MaxWorkersReachedException(int configParallelJobs) :
            base($"The max number of workers ({configParallelJobs}) has been reached.") {
        }
    }
}