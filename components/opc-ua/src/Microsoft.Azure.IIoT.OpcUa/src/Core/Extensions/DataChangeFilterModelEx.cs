// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Data change filter model extensions
    /// </summary>
    public static class DataChangeFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataChangeFilterModel Clone(this DataChangeFilterModel model) {
            if (model == null) {
                return null;
            }

            // Validate deadband value and set a value that will work
            var value = model.DeadbandValue;
            if (value.HasValue) {
                if (model.DeadbandType == DeadbandType.Percent) {
                    if (value > 100.0) {
                        value = 100.0;
                    }
                    else if (value < 0.0) {
                        value = 0.0;
                    }
                }
                else if (model.DeadbandType == null || value < 0.0) {
                    value = null;
                }
            }

            return new DataChangeFilterModel {
                DataChangeTrigger = model.DataChangeTrigger,
                DeadbandType = model.DeadbandType,
                DeadbandValue = value
            };
        }

        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this DataChangeFilterModel model, DataChangeFilterModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            //
            // Null is default and equals to StatusValue, but we allow StatusValue == 1
            // to be set specifically to enable a user to force a data filter to be
            // applied (otherwise it is not if nothing else is set)
            //
            if (model.DataChangeTrigger != other.DataChangeTrigger) {
                return false;
            }
            // Null is None == no deadband
            if (model.DeadbandType != other.DeadbandType) {
                return false;
            }
            if (model.DeadbandValue != other.DeadbandValue) {
                return false;
            }
            return true;
        }
    }
}