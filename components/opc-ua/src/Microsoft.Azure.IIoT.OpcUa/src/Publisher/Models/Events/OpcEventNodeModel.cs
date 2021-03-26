// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an event entry in the configuration.
    /// </summary>
    [DataContract]
    public class OpcEventNodeModel : OpcBaseNodeModel {
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
        /// Settings for pending alarms
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public PendingAlarmsOptionsModel PendingAlarms { get; set; }
    }
}
