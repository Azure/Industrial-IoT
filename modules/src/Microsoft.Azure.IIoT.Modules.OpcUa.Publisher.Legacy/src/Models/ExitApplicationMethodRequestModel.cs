// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Model for an exit application request.
    /// </summary>
    public class ExitApplicationMethodRequestModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int SecondsTillExit { get; set; }
    }
}
