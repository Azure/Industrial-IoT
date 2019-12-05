// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;

    /// <summary>
    /// A description for the EventFilter DataType.
    /// </summary>
    public class EventFilterModel {

        /// <summary>
        /// Select clause
        /// </summary>
        public List<SimpleAttributeOperandModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause.
        /// </summary>
        public ContentFilterModel WhereClause { get; set; }
    }
}