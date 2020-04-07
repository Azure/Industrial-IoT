// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Deletes data at times
    /// </summary>
    [DataContract]
    public class DeleteValuesAtTimesDetailsApiModel {

        /// <summary>
        /// The timestamps to delete
        /// </summary>
        [DataMember(Name = "reqTimes", Order = 0)]
        [Required]
        public DateTime[] ReqTimes { get; set; }
    }
}
