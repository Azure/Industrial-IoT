// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    [DataContract]
    public sealed record class AggregateConfigurationModel
    {
        /// <summary>
        /// Whether to treat uncertain as bad
        /// </summary>
        [DataMember(Name = "treatUncertainAsBad", Order = 1,
            EmitDefaultValue = false)]
        public bool? TreatUncertainAsBad { get; set; }

        /// <summary>
        /// Percent of data that is bad
        /// </summary>
        [DataMember(Name = "percentDataBad", Order = 2,
            EmitDefaultValue = false)]
        public byte? PercentDataBad { get; set; }

        /// <summary>
        /// Percent of data that is good
        /// </summary>
        [DataMember(Name = "percentDataGood", Order = 3,
            EmitDefaultValue = false)]
        public byte? PercentDataGood { get; set; }

        /// <summary>
        /// Whether to use sloped extrapolation.
        /// </summary>
        [DataMember(Name = "useSlopedExtrapolation", Order = 4,
            EmitDefaultValue = false)]
        public bool? UseSlopedExtrapolation { get; set; }
    }
}
