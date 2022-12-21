// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Event filter
    /// </summary>
    public class EventFilterModel {
        /// <summary>
        /// Select clauses
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<SimpleAttributeOperandModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ContentFilterModel WhereClause { get; set; }

        /// <summary>
        /// Simple event Type id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string TypeDefinitionId { get; set; }
    }
}