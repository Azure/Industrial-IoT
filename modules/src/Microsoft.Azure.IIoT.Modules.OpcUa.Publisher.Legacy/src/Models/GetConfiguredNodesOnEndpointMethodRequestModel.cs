// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Model for a get configured nodes on endpoint request.
    /// </summary>
    public class GetConfiguredNodesOnEndpointMethodRequestModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }

        // Optional - since 2.6

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }
    }
}
