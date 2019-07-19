// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher server
    /// </summary>
    public interface IPublisherClient {

        /// <summary>
        /// Call method on publisher with json payload
        /// </summary>
        /// <param name="method"></param>
        /// <param name="request"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        Task<(ServiceResultModel, string)> CallMethodAsync(
            string method, string request,
            DiagnosticsModel diagnostics);
    }
}
