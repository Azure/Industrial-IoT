// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Condition options extensions
    /// </summary>
    public static class ConditionHandlingOptionsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ConditionHandlingOptionsModel Clone(this ConditionHandlingOptionsModel model) {
            if (model == null) {
                return null;
            }
            return new ConditionHandlingOptionsModel {
                UpdateInterval = model.UpdateInterval,
                SnapshotInterval = model.SnapshotInterval,
            };
        }

        /// <summary>
        /// Compare options
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsDisabled(this ConditionHandlingOptionsModel model) {
            if (model == null) {
                return true;
            }
            return
                model.UpdateInterval == null &&
                model.SnapshotInterval == null;
        }

        /// <summary>
        /// Compare options
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ConditionHandlingOptionsModel model, ConditionHandlingOptionsModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            return
                other.UpdateInterval == model.UpdateInterval &&
                other.SnapshotInterval == model.SnapshotInterval;
        }
    }
}