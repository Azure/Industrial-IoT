// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Modification information
    /// </summary>
    public class ModificationInfoApiModel {

        /// <summary>
        /// Modification time
        /// </summary>
        [JsonProperty(PropertyName = "modificationTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ModificationTime { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        [JsonProperty(PropertyName = "updateType",
            NullValueHandling = NullValueHandling.Ignore)]
        public HistoryUpdateOperation? UpdateType { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        [JsonProperty(PropertyName = "userName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string UserName { get; set; }
    }
}
