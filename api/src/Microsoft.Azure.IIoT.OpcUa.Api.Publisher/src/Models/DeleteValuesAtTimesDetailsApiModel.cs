// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

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
