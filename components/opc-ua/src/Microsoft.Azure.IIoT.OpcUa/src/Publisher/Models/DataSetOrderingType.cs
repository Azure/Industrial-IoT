// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Ordering model
    /// </summary>
    public enum DataSetOrderingType {

        /// <summary>
        /// Ascending writer id
        /// </summary>
        AscendingWriterId = 1,

        /// <summary>
        /// Single
        /// </summary>
        AscendingWriterIdSingle = 2,
    }
}
