// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Model for a diagnostic log response.
    /// </summary>
    public class DiagnosticLogMethodResponseModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int MissedMessageCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int LogMessageCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public List<string> Log { get; } = new List<string>();
    }
}
