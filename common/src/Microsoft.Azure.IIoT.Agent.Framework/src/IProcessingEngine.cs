// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RunAsync(ProcessMode processMode,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns the current state of a job
        /// </summary>
        /// <returns></returns>
        Task<VariantValue> GetCurrentJobState();

        /// <summary>
        /// Returns the diagnostic info of a job
        /// </summary>
        /// <returns></returns>
        JobDiagnosticInfoModel GetDiagnosticInfo();

        /// <summary>
        /// Switch processing mode
        /// </summary>
        /// <param name="processMode"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task SwitchProcessMode(ProcessMode processMode,
            DateTime? timestamp);

        /// <summary>
        /// Reconfigure Trigger and take over the exsting resources
        /// </summary>
        void ReconfigureTrigger(object config);
    }
}