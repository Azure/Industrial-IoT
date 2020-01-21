// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Deletes data at times
    /// </summary>
    public class DeleteValuesAtTimesDetailsApiModel {

        /// <summary>
        /// The timestamps to delete
        /// </summary>
        [JsonProperty(PropertyName = "reqTimes")]
        [Required]
        public DateTime[] ReqTimes { get; set; }
    }
}
