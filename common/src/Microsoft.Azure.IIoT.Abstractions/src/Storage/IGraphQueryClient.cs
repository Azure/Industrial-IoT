// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;

    /// <summary>
    /// Graph query client interface
    /// </summary>
    public interface IGraphQueryClient : IDisposable {

        /// <summary>
        /// Executes a graph query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        IResultFeed<T> Submit<T>(string query,
            int? pageSize = null, string partitionKey = null);
    }
}
