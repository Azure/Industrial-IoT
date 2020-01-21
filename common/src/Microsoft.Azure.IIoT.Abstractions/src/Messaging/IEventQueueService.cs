// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading.Tasks;

    /// <summary>
    /// Event queue service
    /// </summary>
    public interface IEventQueueService {

        /// <summary>
        /// Create client to queue path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<IEventQueueClient> OpenAsync(string path = null);
    }
}
