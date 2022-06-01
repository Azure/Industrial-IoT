// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides sql capability
    /// </summary>
    public interface ISqlClient : IDisposable {

        /// <summary>
        /// Query items
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="pageSize"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        IResultFeed<IDocumentInfo<T>> Query<T>(string queryString,
            IDictionary<string, object> parameters = null,
            int? pageSize = null, string partitionKey = null);

        /// <summary>
        /// Continue a previously run query using continuation token
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryString"></param>
        /// <param name="continuationToken"></param>
        /// <param name="parameters"></param>
        /// <param name="pageSize"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        IResultFeed<IDocumentInfo<T>> Continue<T>(string queryString,
            string continuationToken,IDictionary<string, object> parameters = null,
            int? pageSize = null,string partitionKey = null);

        /// <summary>
        /// Drop all items that match the query
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="partitionKey"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DropAsync(string queryString,
            IDictionary<string, object> parameters = null,
            string partitionKey = null,
            CancellationToken ct = default);
    }
}
