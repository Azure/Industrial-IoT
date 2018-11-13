// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute reads
    /// </summary>
    public class BatchReadResponseApiModel {

        /// <summary>
        /// All results of attribute reads
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<AttributeReadResponseApiModel> Results { set; get; }
    }
}
