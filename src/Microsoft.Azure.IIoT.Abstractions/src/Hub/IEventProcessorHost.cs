// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System.Threading.Tasks;

    /// <summary>
    /// Event processor
    /// </summary>
    public interface IEventProcessorHost {

        /// <summary>
        /// Start host
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop host
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
