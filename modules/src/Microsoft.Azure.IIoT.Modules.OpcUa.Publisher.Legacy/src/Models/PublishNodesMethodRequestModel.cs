// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;

    /// <summary>
    /// ´Model for a publish node request.
    /// </summary>
    public class PublishNodesMethodRequestModel
    {
        public PublishNodesMethodRequestModel(string endpointUrl, bool useSecurity = true, string userName = null, string password = null)
        {
            OpcNodes = new List<OpcNodeOnEndpointModel>();
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity;
            UserName = userName;
            Password = password;
        }

        public string EndpointUrl { get; set; }
        public List<OpcNodeOnEndpointModel> OpcNodes { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OpcAuthenticationMode? OpcAuthenticationMode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        // Optional - since 2.6

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityProfileUri { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityMode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }
    }
}
