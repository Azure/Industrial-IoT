// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Model for an unpublish node request.
    /// </summary>
    public class UnpublishNodesMethodRequestModel
    {
        public UnpublishNodesMethodRequestModel(string endpointUrl)
        {
            OpcNodes = new List<OpcNodeOnEndpointModel>();
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }

        public List<OpcNodeOnEndpointModel> OpcNodes { get; }

        // Optional - since 2.6

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }
    }
}
