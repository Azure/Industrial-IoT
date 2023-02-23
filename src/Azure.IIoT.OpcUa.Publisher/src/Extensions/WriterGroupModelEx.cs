// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using Azure.IIoT.OpcUa.Publisher;

    /// <summary>
    /// Extensions for data set writer
    /// </summary>
    public static class WriterGroupModelEx
    {
        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <param name="publisherId"></param>
        /// <returns></returns>
        public static IWriterGroupConfig ToWriterGroupJobConfiguration(
            this WriterGroupJobModel model, string publisherId)
        {
            var writerGroupJobConfig = new WriterGroupJobConfig
            {
                BatchSize = model.Engine?.BatchSize,
                BatchTriggerInterval = model.Engine?.BatchTriggerInterval,
                PublisherId = publisherId,
                WriterGroup = model.WriterGroup.Clone(),
                MaxMessageSize = model.Engine?.MaxMessageSize,
                DefaultMaxMessagesPerPublish = model.Engine?.DefaultMaxMessagesPerPublish,
                DefaultMetaDataQueueName = model.Engine?.DefaultMetaDataQueueName,
                MaxOutgressMessages = model.Engine?.MaxOutgressMessages,
                UseStandardsCompliantEncoding = model.Engine?.UseStandardsCompliantEncoding ?? false,
            };

            if (writerGroupJobConfig.WriterGroup != null)
            {
                if (writerGroupJobConfig.WriterGroup.MessageSettings == null)
                {
                    writerGroupJobConfig.WriterGroup.MessageSettings = new WriterGroupMessageSettingsModel();
                }
                //
                // Adjust network message content mask based on samples messaging mode for
                // backwards compatibility with existing database stored writer group models.
                //
                if (model.MessagingMode == MessagingMode.Samples ||
                    model.MessagingMode == MessagingMode.FullSamples)
                {
                    writerGroupJobConfig.WriterGroup.MessageSettings.NetworkMessageContentMask
                        |= NetworkMessageContentMask.MonitoredItemMessage;
                    writerGroupJobConfig.WriterGroup.MessageSettings.NetworkMessageContentMask
                        |= NetworkMessageContentMask.DataSetMessageHeader;
                    writerGroupJobConfig.WriterGroup.MessageSettings.NetworkMessageContentMask
                        &= ~NetworkMessageContentMask.NetworkMessageHeader;
                }
                else if (model.MessagingMode != null)
                {
                    writerGroupJobConfig.WriterGroup.MessageSettings.NetworkMessageContentMask
                        &= ~NetworkMessageContentMask.MonitoredItemMessage;
                }
            }
            return writerGroupJobConfig;
        }
    }
}
