// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// PendingAlarmsOptionsModel extensions
    /// </summary>
    public static class PendingAlarmsOptionsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PendingAlarmsOptionsModel Clone(this PendingAlarmsOptionsModel model) {
            if (model == null) {
                return null;
            }
            return new PendingAlarmsOptionsModel {
                IsEnabled = model.IsEnabled,
                UpdateInterval = model.UpdateInterval,
                SnapshotInterval = model.SnapshotInterval,
            };
        }

        /// <summary>
        /// Compare options
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this PendingAlarmsOptionsModel model, PendingAlarmsOptionsModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            return
                other.IsEnabled == model.IsEnabled &&
                other.UpdateInterval == model.UpdateInterval &&
                other.SnapshotInterval == model.SnapshotInterval;
        }
    }
}