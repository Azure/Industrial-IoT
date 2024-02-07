// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connection services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConnectionServices<T>
    {
        /// <summary>
        /// Test connection
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TestConnectionResponseModel> TestConnectionAsync(T endpoint,
            TestConnectionRequestModel request, CancellationToken ct = default);
    }
}
