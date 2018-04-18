// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    public class ApplicationRegistrationApiModel {

        /// <summary>
        /// Server information
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// List of endpoint twins
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<TwinRegistrationApiModel> Endpoints { get; set; }
    }
}
