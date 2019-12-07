// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Event filter
    /// </summary>
    public class EventFilterModel {

        /// <summary>
        /// Where clause
        /// </summary>
        public List<SimpleAttributeOperandModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        public ContentFilterModel WhereClause { get; set; }
    }
}