// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
{
    /// <summary>
    /// Message setting extensions
    /// </summary>
    public static class WriterGroupMessageSettingsModelEx
    {
        /// <summary>
        /// Check if same message setting configuration is the same.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this WriterGroupMessageSettingsModel model,
            WriterGroupMessageSettingsModel other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.SamplingOffset != other.SamplingOffset)
            {
                return false;
            }
            if (model.MaxMessagesPerPublish != other.MaxMessagesPerPublish)
            {
                return false;
            }
            if (model.PublishingOffset != other.PublishingOffset)
            {
                return false;
            }
            if (model.NetworkMessageContentMask != other.NetworkMessageContentMask)
            {
                return false;
            }
            if (model.GroupVersion != other.GroupVersion)
            {
                return false;
            }
            if (model.DataSetOrdering != other.DataSetOrdering)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriterGroupMessageSettingsModel Clone(this WriterGroupMessageSettingsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WriterGroupMessageSettingsModel
            {
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
