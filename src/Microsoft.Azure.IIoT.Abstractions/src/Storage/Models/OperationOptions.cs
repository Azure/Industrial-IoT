// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Operation options
    /// </summary>
    public class OperationOptions {

        /// <summary>
        /// Set consistency level
        /// </summary>
        public OperationConsistency? Consistency { get; set; }

        /// <summary>
        /// Partition
        /// </summary>
        public string PartitionKey { get; set; }
    }
}
