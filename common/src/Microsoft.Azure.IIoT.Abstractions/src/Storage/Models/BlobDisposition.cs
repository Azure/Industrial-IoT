// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Blob processor disposition
    /// </summary>
    public enum BlobDisposition {

        /// <summary>
        /// Nothing to do
        /// </summary>
        Nothing,

        /// <summary>
        /// Delete blob
        /// </summary>
        Delete,

        /// <summary>
        /// Reopen and call again
        /// </summary>
        Retry,
    }
}
