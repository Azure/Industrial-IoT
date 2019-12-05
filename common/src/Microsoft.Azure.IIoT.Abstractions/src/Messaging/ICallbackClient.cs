// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading.Tasks;

    /// <summary>
    /// Factory to get callback registrations
    /// </summary>
    public interface ICallbackClient {

        /// <summary>
        /// Get callback registration interface for user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<ICallbackRegistrar> GetRegistrarAsync(string userId = null);
    }
}
