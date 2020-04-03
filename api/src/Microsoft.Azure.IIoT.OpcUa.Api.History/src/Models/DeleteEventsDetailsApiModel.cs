// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The events to delete
    /// </summary>
    [DataContract]
    public class DeleteEventsDetailsApiModel {

        /// <summary>
        /// Events to delete
        /// </summary>
        [DataMember(Name = "eventIds", Order = 0)]
        [Required]
        public List<byte[]> EventIds { get; set; }
    }
}
