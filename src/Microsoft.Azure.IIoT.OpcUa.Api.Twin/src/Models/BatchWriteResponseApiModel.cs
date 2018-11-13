// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute write
    /// </summary>
    public class BatchWriteResponseApiModel {

        /// <summary>
        /// All results of attribute writes
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<AttributeWriteResponseApiModel> Results { set; get; }
    }
}
