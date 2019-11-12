// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Model for a get configured endpoints request.
    /// </summary>
    public class GetConfiguredEndpointsMethodRequestModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }
}
