// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job processing engine
    /// </summary>
    public interface IProcessingEngine {

        /// <summary>
        /// Identifier of the engine
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Engine running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Run engine
        /// </summary>
        /// <param name="processMode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RunAsync(ProcessMode processMode, CancellationToken ct);

        /// <summary>
        /// Returns the diagnostic info of a job
        /// </summary>
        /// <returns></returns>
        JobDiagnosticInfoModel GetDiagnosticInfo();

        /// <summary>
        /// Reconfigure engine
        /// </summary>
        ValueTask ReconfigureAsync(object config, CancellationToken ct);
    }
}