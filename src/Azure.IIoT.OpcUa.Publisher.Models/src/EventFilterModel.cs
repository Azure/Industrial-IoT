// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Event filter
    /// </summary>
    [DataContract]
    public sealed record class EventFilterModel
    {
        /// <summary>
        /// Select clauses
        /// </summary>
        [DataMember(Name = "selectClauses", Order = 0,
            EmitDefaultValue = false)]
        public IReadOnlyList<SimpleAttributeOperandModel>? SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [DataMember(Name = "whereClause", Order = 1,
            EmitDefaultValue = false)]
        public ContentFilterModel? WhereClause { get; set; }

        /// <summary>
        /// Simple event Type definition node id
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 3,
            EmitDefaultValue = false)]
        public string? TypeDefinitionId { get; set; }
    }
}
