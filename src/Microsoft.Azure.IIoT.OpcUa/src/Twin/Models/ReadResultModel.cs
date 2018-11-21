// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute reads
    /// </summary>
    public class ReadResultModel {

        /// <summary>
        /// All results of attribute reads
        /// </summary>
        public List<AttributeReadResultModel> Results { set; get; }
    }
}
