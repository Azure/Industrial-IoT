// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Data change filter model extensions
    /// </summary>
    public static class DataChangeFilterModelEx
    {
        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this DataChangeFilterModel? model, DataChangeFilterModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            //
            // Null is default and equals to StatusValue, but we allow StatusValue == 1
            // to be set specifically to enable a user to force a data filter to be
            // applied (otherwise it is not if nothing else is set)
            //
            if (model.DataChangeTrigger != other.DataChangeTrigger)
            {
                return false;
            }
            // Null is None == no deadband
            if (model.DeadbandType != other.DeadbandType)
            {
                return false;
            }
            if (model.DeadbandValue != other.DeadbandValue)
            {
                return false;
            }
            return true;
        }
    }
}
