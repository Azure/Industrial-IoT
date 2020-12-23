// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles raw events
    /// </summary>
    public interface IEventProcessingHandler : IHandler {

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="properties"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        Task HandleAsync(byte[] eventData,
            IDictionary<string, string> properties, Func<Task> checkpoint);

        /// <summary>
        /// Event batch completed
        /// </summary>
        /// <returns></returns>
        Task OnBatchCompleteAsync();
    }
}
