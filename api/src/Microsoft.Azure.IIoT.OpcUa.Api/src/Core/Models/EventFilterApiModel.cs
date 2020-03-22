// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Event filter
    /// </summary>
    [DataContract]
    public class EventFilterApiModel {

        /// <summary>
        /// Select statements
        /// </summary>
        [DataMember(Name = "selectClauses")]
        public List<SimpleAttributeOperandApiModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [DataMember(Name = "whereClause")]
        public ContentFilterApiModel WhereClause { get; set; }
    }
}