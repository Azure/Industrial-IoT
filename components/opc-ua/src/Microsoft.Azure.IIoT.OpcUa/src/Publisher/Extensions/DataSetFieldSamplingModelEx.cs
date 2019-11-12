// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Monitored item model extensions
    /// </summary>
    public static class DataSetFieldSamplingModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetFieldSamplingModel Clone(this DataSetFieldSamplingModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetFieldSamplingModel {
                SamplingInterval = model.SamplingInterval,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DataChangeFilter = model.DataChangeFilter,
                DeadBandType = model.DeadBandType,
                DeadBandValue = model.DeadBandValue
            };
        }
    }
}