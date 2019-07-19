// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Documents.Client {
    using Microsoft.Azure.IIoT.Storage;
    using System;

    /// <summary>
    /// Operation options extension
    /// </summary>
    public static class OperationConsistencyEx {

        /// <summary>
        /// Convert to consistency level
        /// </summary>
        /// <param name="consistency"></param>
        /// <returns></returns>
        public static ConsistencyLevel? ToConsistencyLevel(this OperationConsistency? consistency) {
            if (consistency == null) {
                return ConsistencyLevel.Session; // Default to session
            }
            switch (consistency.Value) {
                case OperationConsistency.Strong:
                    return ConsistencyLevel.Strong;
                case OperationConsistency.Session:
                    return ConsistencyLevel.Session;
                case OperationConsistency.Low:
                    return ConsistencyLevel.Eventual;
                case OperationConsistency.Bounded:
                    return ConsistencyLevel.BoundedStaleness;
                default:
                    throw new ArgumentException("Unknown consistency level passed");
            }
        }
    }
}
