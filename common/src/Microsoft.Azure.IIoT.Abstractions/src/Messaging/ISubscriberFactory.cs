// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading.Tasks;

    /// <summary>
    /// Factory to create and start subscribers
    /// </summary>
    public interface ISubscriberFactory {

        /// <summary>
        /// Create subscriber
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<ICallbackRegistration> CreateAsync(string userId = null);
    }

}
