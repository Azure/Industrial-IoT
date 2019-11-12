// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Model for a get configured endpoints response.
    /// </summary>
    public class GetConfiguredEndpointsMethodResponseModel
    {
        public GetConfiguredEndpointsMethodResponseModel()
        {
            Endpoints = new List<ConfiguredEndpointModel>();
        }

        public List<ConfiguredEndpointModel> Endpoints { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken { get; set; }
    }
}
