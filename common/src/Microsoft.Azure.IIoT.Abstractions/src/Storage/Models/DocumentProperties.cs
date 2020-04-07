// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Common document properties
    /// </summary>
    public static class DocumentProperties {

        /// <summary>
        /// Identifier property
        /// </summary>
        public const string IdKey = "id";

        /// <summary>
        /// Partition key property
        /// </summary>
        public const string PartitionKey = "__pk";

        /// <summary>
        /// Time to live key property
        /// </summary>
        public const string TtlKey = "ttl";
    }
}
