// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using System;
    using System.Linq;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetModelEx {

        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="messageTriggerConfig"></param>
        /// <returns></returns>
        public static SubscriptionInfoModel ToSubscriptionInfoModel(
            this DataSetModel dataSet, IPubSubMessageTriggerConfig messageTriggerConfig) {
            if (messageTriggerConfig == null) {
                throw new ArgumentNullException(nameof(messageTriggerConfig));
            }
            return new SubscriptionInfoModel {
                MessageMode = MessageModes.Subscription,
                Connection = messageTriggerConfig.Connection.Clone(),
                ExtraFields = null, // dataSet.ExtraFields,
                Subscription = new SubscriptionModel {
                    MonitoredItems = dataSet.Fields
                        .Select(f => new MonitoredItemModel {
                            NodeId = f.NodeId,
                            DataChangeFilter = f.Configuration?.DataChangeFilter,
                            DeadBandType = f.Configuration?.DeadBandType,
                            DeadBandValue = f.Configuration?.DeadBandValue,
                            DiscardNew = f.Configuration?.DiscardNew,
                            QueueSize = f.Configuration?.QueueSize,
                            SamplingInterval = f.Configuration?.SamplingInterval,
                            HeartbeatInterval = null,
                            SkipFirst = null
                        })
                        .ToList(),
                    Id = dataSet.Name,
                    Priority = null,  // TODO: We should also forward these from pub/sub
                    LifeTimeCount = null,
                    MaxKeepAliveCount = null,
                    MaxNotificationsPerPublish = null,
                    PublishingDisabled = null,
                    PublishingInterval = (int?)messageTriggerConfig
                        .PublishingInterval?.TotalMilliseconds ?? 0
                }
            };
        }
    }
}