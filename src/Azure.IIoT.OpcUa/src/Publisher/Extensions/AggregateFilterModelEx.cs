// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Aggregate filter model extensions
    /// </summary>
    public static class AggregateFilterModelEx
    {
        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this AggregateFilterModel? model,
            AggregateFilterModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.AggregateTypeId != other.AggregateTypeId)
            {
                return false;
            }
            if (model.ProcessingInterval != other.ProcessingInterval)
            {
                return false;
            }
            if (model.StartTime != other.StartTime)
            {
                return false;
            }
            if (!model.AggregateConfiguration.IsSameAs(other.AggregateConfiguration))
            {
                return false;
            }
            return true;
        }
    }
}
