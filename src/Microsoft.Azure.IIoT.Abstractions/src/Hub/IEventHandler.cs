// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles events
    /// </summary>
    public interface IEventHandler {

        /// <summary>
        /// Handle message
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="properties"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        Task HandleAsync(byte[] eventData,
            IDictionary<string, string> properties, Func<Task> checkpoint);

        /// <summary>
        /// Message batch completed
        /// </summary>
        /// <returns></returns>
        Task OnBatchCompleteAsync();
    }
}
