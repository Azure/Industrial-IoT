// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    public class AggregateConfigurationModel {

        /// <summary>
        /// Whether to use the default server caps
        /// </summary>
        public bool? UseServerCapabilitiesDefaults { get; set; }

        /// <summary>
        /// Whether to treat uncertain as bad
        /// </summary>
        public bool? TreatUncertainAsBad { get; set; }

        /// <summary>
        /// Percent of data that is bad
        /// </summary>
        public byte? PercentDataBad { get; set; }

        /// <summary>
        /// Percent of data that is good
        /// </summary>
        public byte? PercentDataGood { get; set; }

        /// <summary>
        /// Whether to use sloped extrapolation.
        /// </summary>
        public bool? UseSlopedExtrapolation { get; set; }
    }
}
