// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles events from source
    /// </summary>
    public interface IEventHandler {

        /// <summary>
        /// Event content type
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="deviceId"></param>
        /// <param name="payload"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, Func<Task> checkpoint);

        /// <summary>
        /// Called when batch is completed
        /// </summary>
        /// <returns></returns>
        Task OnBatchCompleteAsync();
    }
}
