// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Twin registration list
    /// </summary>
    public class TwinRegistrationListApiModel {

        /// <summary>
        /// Registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<TwinRegistrationApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
