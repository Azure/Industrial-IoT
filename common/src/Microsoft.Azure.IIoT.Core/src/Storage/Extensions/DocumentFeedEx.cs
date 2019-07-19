// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// List of documents
    /// </summary>
    public static class DocumentFeedEx {

        /// <summary>
        /// Invoke callback for each element
        /// </summary>
        /// <param name="feed"></param>
        /// <param name="callback"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task ForEachAsync<T>(this IResultFeed<T> feed,
            Func<T, Task> callback,
            CancellationToken ct = default) {
            while (feed.HasMore()) {
                var results = await feed.ReadAsync(ct);
                foreach (var item in results) {
                    await callback(item);
                }
            }
        }
    }
}
