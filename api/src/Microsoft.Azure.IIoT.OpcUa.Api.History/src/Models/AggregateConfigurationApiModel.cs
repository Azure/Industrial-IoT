// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    public class AggregateConfigurationApiModel {

        /// <summary>
        /// Whether to use the default server caps
        /// </summary>
        [JsonProperty(PropertyName = "useServerCapabilitiesDefaults",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseServerCapabilitiesDefaults { get; set; }

        /// <summary>
        /// Whether to treat uncertain as bad
        /// </summary>
        [JsonProperty(PropertyName = "treatUncertainAsBad",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? TreatUncertainAsBad { get; set; }

        /// <summary>
        /// Percent of data that is bad
        /// </summary>
        [JsonProperty(PropertyName = "percentDataBad",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte? PercentDataBad { get; set; }

        /// <summary>
        /// Percent of data that is good
        /// </summary>
        [JsonProperty(PropertyName = "percentDataGood",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte? PercentDataGood { get; set; }

        /// <summary>
        /// Whether to use sloped extrapolation.
        /// </summary>
        [JsonProperty(PropertyName = "useSlopedExtrapolation",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSlopedExtrapolation { get; set; }
    }
}
