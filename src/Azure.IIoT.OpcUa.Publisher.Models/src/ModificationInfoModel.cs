// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Modification information
    /// </summary>
    [DataContract]
    public sealed record class ModificationInfoModel
    {
        /// <summary>
        /// Modification time
        /// </summary>
        [DataMember(Name = "modificationTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? ModificationTime { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        [DataMember(Name = "updateType", Order = 1,
            EmitDefaultValue = false)]
        public HistoryUpdateOperation? UpdateType { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        [DataMember(Name = "userName", Order = 2,
            EmitDefaultValue = false)]
        public string? UserName { get; set; }
    }
}
