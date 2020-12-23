// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Configuration
    /// </summary>
    [DataContract]
    public class ConfigurationContentModel {

        /// <summary>
        /// Gets or sets modules configurations
        /// </summary>
        [DataMember(Name = "modulesContent")]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

        /// <summary>
        /// Gets or sets device configuration
        /// </summary>
        [DataMember(Name = "deviceContent")]
        public IDictionary<string, object> DeviceContent { get; set; }
    }
}
