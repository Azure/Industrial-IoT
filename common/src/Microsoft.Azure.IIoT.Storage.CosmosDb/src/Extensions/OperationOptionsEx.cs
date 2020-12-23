// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Documents.Client {
    using Microsoft.Azure.IIoT.Storage;

    /// <summary>
    /// Operation options extension
    /// </summary>
    public static class OperationOptionsEx {

        /// <summary>
        /// Convert to request options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="partitioned"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        public static RequestOptions ToRequestOptions(this OperationOptions options,
            bool partitioned = true, string etag = null) {
            var pk = !partitioned || string.IsNullOrEmpty(options?.PartitionKey) ? null :
                new PartitionKey(options?.PartitionKey);
            var ac = string.IsNullOrEmpty(etag) ? null : new AccessCondition {
                Condition = etag,
                Type = AccessConditionType.IfMatch
            };
            return new RequestOptions {
                AccessCondition = ac,
                PartitionKey = pk,
                ConsistencyLevel = options?.Consistency.ToConsistencyLevel()
            };
        }
    }
}
