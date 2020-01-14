// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Content filter
    /// </summary>
    public class ContentFilterApiModel {

        /// <summary>
        /// The flat list of elements in the filter AST
        /// </summary>
        [JsonProperty(PropertyName = "elements")]
        public List<ContentFilterElementApiModel> Elements { get; set; }
    }
}