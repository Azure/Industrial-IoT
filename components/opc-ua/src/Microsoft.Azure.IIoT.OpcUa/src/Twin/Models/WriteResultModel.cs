// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute write
    /// </summary>
    public class WriteResultModel {

        /// <summary>
        /// All results of attribute writes
        /// </summary>
        public List<AttributeWriteResultModel> Results { set; get; }
    }
}
