// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Modification information
    /// </summary>
    [DataContract]
    public class ModificationInfoApiModel {

        /// <summary>
        /// Modification time
        /// </summary>
        [DataMember(Name = "modificationTime",
            EmitDefaultValue = false)]
        public DateTime? ModificationTime { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        [DataMember(Name = "updateType",
            EmitDefaultValue = false)]
        public HistoryUpdateOperation? UpdateType { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        [DataMember(Name = "userName",
            EmitDefaultValue = false)]
        public string UserName { get; set; }
    }
}
