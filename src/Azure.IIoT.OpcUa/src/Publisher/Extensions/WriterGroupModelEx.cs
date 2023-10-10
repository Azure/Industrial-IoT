// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;

    /// <summary>
    /// Writer group Model extensions
    /// </summary>
    public static class WriterGroupModelEx
    {
        /// <summary>
        /// Check if same writer group configuration
        /// Excludes writers.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this WriterGroupModel? model,
            WriterGroupModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.PublishingInterval != other.PublishingInterval)
            {
                return false;
            }
            if (model.WriterGroupId != other.WriterGroupId)
            {
                return false;
            }
            if (model.KeepAliveTime != other.KeepAliveTime)
            {
                return false;
            }
            if (model.Priority != other.Priority)
            {
                return false;
            }
            if (model.SecurityMode != other.SecurityMode)
            {
                return false;
            }
            if (model.SecurityGroupId != other.SecurityGroupId)
            {
                return false;
            }
            if (model.HeaderLayoutUri != other.HeaderLayoutUri)
            {
                return false;
            }
            if (model.MessageType != other.MessageType)
            {
                return false;
            }
            if (!model.MessageSettings.IsSameAs(other.MessageSettings))
            {
                return false;
            }
            if (model.Name != other.Name)
            {
                return false;
            }
            if (model.MaxNetworkMessageSize != other.MaxNetworkMessageSize)
            {
                return false;
            }
            if (model.Transport != other.Transport)
            {
                return false;
            }
            if (model.QoS != other.QoS)
            {
                return false;
            }
            if (model.PublishQueueSize != other.PublishQueueSize)
            {
                return false;
            }
            if (model.NotificationPublishThreshold != other.NotificationPublishThreshold)
            {
                return false;
            }
            return true;
        }
    }
}
