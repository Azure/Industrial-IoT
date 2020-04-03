// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Published items
    /// </summary>
    [DataContract]
    public class PublishedDataItemsApiModel {

        /// <summary>
        /// Published data variables
        /// </summary>
        [DataMember(Name = "publishedData", Order = 0)]
        public List<PublishedDataSetVariableApiModel> PublishedData { get; set; }
    }
}