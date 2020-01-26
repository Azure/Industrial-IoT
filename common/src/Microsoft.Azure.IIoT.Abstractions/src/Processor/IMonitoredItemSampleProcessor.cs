// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Processor {
    using Microsoft.Azure.IIoT.Processor.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher sample processing
    /// </summary>
    public interface IMonitoredItemSampleProcessor {

        /// <summary>
        /// Handle individual messages
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        Task HandleSampleAsync(MonitoredItemSampleModel sample);
    }
}
