// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published items
    /// </summary>
    [DataContract]
    public record class PublishedEventItemsModel {

        /// <summary>
        /// Published data variables
        /// </summary>
        [DataMember(Name = "publishedData", Order = 0)]
        public List<PublishedDataSetEventModel> PublishedData { get; set; }
    }
}