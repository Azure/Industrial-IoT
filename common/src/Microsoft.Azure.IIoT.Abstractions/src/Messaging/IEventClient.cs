// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
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
        /// <param name="eventSchema"></param>
        /// <param name="contentEncoding"></param>
        /// <returns></returns>
        Task SendEventAsync(byte[] data, string contentType,
            string eventSchema, string contentEncoding);

        /// <summary>
        /// Send batch of events
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="contentType"></param>
        /// <param name="eventSchema"></param>
        /// <param name="contentEncoding"></param>
        /// <returns></returns>
        Task SendEventAsync(IEnumerable<byte[]> batch,
            string contentType, string eventSchema,
            string contentEncoding);
    }
}
