// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Send events
    /// </summary>
    public interface IEventClient {

        /// <summary>
        /// Send event
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        Task SendAsync(byte[] data, string contentType);

        /// <summary>
        /// Send batch of events
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        Task SendAsync(IEnumerable<byte[]> batch,
            string contentType);
    }
}
