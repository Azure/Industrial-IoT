// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    [DataContract]
    public class AggregateConfigurationApiModel {

        /// <summary>
        /// Whether to use the default server caps
        /// </summary>
        [DataMember(Name = "useServerCapabilitiesDefaults",
            EmitDefaultValue = false)]
        public bool? UseServerCapabilitiesDefaults { get; set; }

        /// <summary>
        /// Whether to treat uncertain as bad
        /// </summary>
        [DataMember(Name = "treatUncertainAsBad",
            EmitDefaultValue = false)]
        public bool? TreatUncertainAsBad { get; set; }

        /// <summary>
        /// Percent of data that is bad
        /// </summary>
        [DataMember(Name = "percentDataBad",
            EmitDefaultValue = false)]
        public byte? PercentDataBad { get; set; }

        /// <summary>
        /// Percent of data that is good
        /// </summary>
        [DataMember(Name = "percentDataGood",
            EmitDefaultValue = false)]
        public byte? PercentDataGood { get; set; }

        /// <summary>
        /// Whether to use sloped extrapolation.
        /// </summary>
        [DataMember(Name = "useSlopedExtrapolation",
            EmitDefaultValue = false)]
        public bool? UseSlopedExtrapolation { get; set; }
    }
}
