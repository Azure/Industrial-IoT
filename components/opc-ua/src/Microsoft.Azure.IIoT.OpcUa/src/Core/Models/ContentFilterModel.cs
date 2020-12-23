// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Content filter
    /// </summary>
    public class ContentFilterModel {

        /// <summary>
        /// The flat list of elements in the filter AST
        /// </summary>
        public List<ContentFilterElementModel> Elements { get; set; }
    }
}