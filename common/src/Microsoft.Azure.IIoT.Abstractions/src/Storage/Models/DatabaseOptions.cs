// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Database create options
    /// </summary>
    public class DatabaseOptions {

        /// <summary>
        /// Database Consistency
        /// </summary>
        public OperationConsistency? Consistency { get; set; }
    }
}
