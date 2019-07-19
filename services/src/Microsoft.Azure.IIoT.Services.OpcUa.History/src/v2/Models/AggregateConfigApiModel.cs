// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    public class AggregateConfigApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AggregateConfigApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public AggregateConfigApiModel(AggregateConfigModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults;
            TreatUncertainAsBad = model.TreatUncertainAsBad;
            PercentDataBad = model.PercentDataBad;
            PercentDataGood = model.PercentDataGood;
            UseSlopedExtrapolation = model.UseSlopedExtrapolation;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public AggregateConfigModel ToServiceModel() {
            return new AggregateConfigModel {
                UseServerCapabilitiesDefaults = UseServerCapabilitiesDefaults,
                TreatUncertainAsBad = TreatUncertainAsBad,
                PercentDataBad = PercentDataBad,
                PercentDataGood = PercentDataGood,
                UseSlopedExtrapolation = UseSlopedExtrapolation
            };
        }

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
