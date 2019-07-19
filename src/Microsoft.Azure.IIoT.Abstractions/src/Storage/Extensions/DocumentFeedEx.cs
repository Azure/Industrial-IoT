// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Feed extensions
    /// </summary>
    public static class DocumentFeedEx {

        /// <summary>
        /// Read all results from feed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="feed"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> AllAsync<T>(this IResultFeed<T> feed,
            CancellationToken ct = default) {
            var results = new List<T>();
            while (feed.HasMore()) {
                var result = await feed.ReadAsync(ct);
                results.AddRange(result);
            }
            return results;
        }
    }
}
