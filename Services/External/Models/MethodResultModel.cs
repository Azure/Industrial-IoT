// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Twin service method results model
    /// </summary>
    public class MethodResultModel {

        [JsonProperty(PropertyName = "Status")]
        public int Status { get; set; }

        [JsonProperty(PropertyName = "JsonPayload")]
        public string JsonPayload { get; set; }
    }
}
