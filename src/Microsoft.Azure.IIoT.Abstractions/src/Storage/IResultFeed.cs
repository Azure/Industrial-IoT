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
    /// List of documents
    /// </summary>
    public interface IResultFeed<T> : IDisposable {

        /// <summary>
        /// Get continuation token to continue read later
        /// </summary>
        string ContinuationToken { get; }

        /// <summary>
        /// Returns whether there is more data in the feed
        /// </summary>
        /// <returns></returns>
        bool HasMore();

        /// <summary>
        /// Read results from feed
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T>> ReadAsync(
            CancellationToken ct = default);
    }
}
