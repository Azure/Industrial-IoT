// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Message setting extensions
    /// </summary>
    public static class WriterGroupMessageSettingsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriterGroupMessageSettingsModel Clone(this WriterGroupMessageSettingsModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupMessageSettingsModel {
                DataSetOrdering = model.DataSetOrdering,
                GroupVersion = model.GroupVersion,
                NetworkMessageContentMask = model.NetworkMessageContentMask,
                PublishingOffset = model.PublishingOffset,
                MaxMessagesPerPublish = model.MaxMessagesPerPublish,
                SamplingOffset = model.SamplingOffset
            };
        }
    }
}