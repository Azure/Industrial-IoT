// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Model class for a get configured nodes on endpoint response.
    /// </summary>
    public class GetConfiguredNodesOnEndpointMethodResponseModel
    {
        public GetConfiguredNodesOnEndpointMethodResponseModel()
        {
            OpcNodes = new List<OpcNodeOnEndpointModel>();
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public List<OpcNodeOnEndpointModel> OpcNodes { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken { get; set; }
    }
}
