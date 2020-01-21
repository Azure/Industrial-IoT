// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Configuration
    /// </summary>
    public class ConfigurationContentModel {

        /// <summary>
        /// Gets or sets modules configurations
        /// </summary>
        [JsonProperty(PropertyName = "modulesContent")]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

        /// <summary>
        /// Gets or sets device configuration
        /// </summary>
        [JsonProperty(PropertyName = "deviceContent")]
        public IDictionary<string, object> DeviceContent { get; set; }
    }
}
