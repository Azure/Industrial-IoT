// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Model for a get info response.
    /// </summary>
    public class GetInfoMethodResponseModel
    {
        public GetInfoMethodResponseModel()
        {
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionMajor { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionMinor { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionPatch { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string SemanticVersion { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string InformationalVersion { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string OS { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public Architecture OSArchitecture { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string FrameworkDescription { get; set; }
    }
}
