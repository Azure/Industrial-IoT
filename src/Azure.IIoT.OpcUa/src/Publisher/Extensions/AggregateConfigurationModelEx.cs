// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Aggregate configuration model extensions
    /// </summary>
    public static class AggregateConfigurationModelEx
    {
        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this AggregateConfigurationModel? model,
            AggregateConfigurationModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.PercentDataBad != other.PercentDataBad)
            {
                return false;
            }
            if (model.PercentDataGood != other.PercentDataGood)
            {
                return false;
            }
            if (model.TreatUncertainAsBad != other.TreatUncertainAsBad)
            {
                return false;
            }
            if (model.UseSlopedExtrapolation != other.UseSlopedExtrapolation)
            {
                return false;
            }
            return true;
        }
    }
}
