// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationQueryApiModel {

        /// <summary>
        /// Type of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationType",
            NullValueHandling = NullValueHandling.Ignore)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application capabilities
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Capabilities { get; set; }

        /// <summary>
        /// Supervisor or site the application belongs to.
        /// </summary>
        [JsonProperty(PropertyName = "siteOrSupervisorId",
           NullValueHandling = NullValueHandling.Ignore)]
        public string SiteOrSupervisorId { get; set; }
    }
}

