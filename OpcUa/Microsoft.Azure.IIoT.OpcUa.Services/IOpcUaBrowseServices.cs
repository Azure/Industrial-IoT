// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse services via endpoint model
    /// </summary>
    public interface IOpcUaBrowseServices<T> {

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResultModel> NodeBrowseAsync(T endpoint,
            BrowseRequestModel request);
    }
}