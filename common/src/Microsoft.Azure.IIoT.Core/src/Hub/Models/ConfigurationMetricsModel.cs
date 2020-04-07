// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Azure IOT Configuration Metrics
    /// </summary>
    [DataContract]
    public class ConfigurationMetricsModel {

        /// <summary>
        /// Results of the metrics collection queries
        /// </summary>
        [DataMember(Name = "results")]
        public IDictionary<string, long> Results { get; set; }

        /// <summary>
        /// Queries used for metrics collection
        /// </summary>
        [DataMember(Name = "queries")]
        public IDictionary<string, string> Queries { get; set; }
    }

}
