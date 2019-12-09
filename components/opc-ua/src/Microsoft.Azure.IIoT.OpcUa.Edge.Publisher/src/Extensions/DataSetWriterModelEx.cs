// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetWriterModelEx {

        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <returns></returns>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter) {
            if (dataSetWriter == null) {
                return null;
            }
            if (dataSetWriter.DataSet == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSet));
            }
            if (dataSetWriter.DataSet.DataSetSource == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSet.DataSetSource));
            }
            if (dataSetWriter.DataSet.DataSetSource.Connection == null) {
                throw new ArgumentNullException(
                    nameof(dataSetWriter.DataSet.DataSetSource.Connection));
            }
            var monitoredItems = dataSetWriter.DataSet.DataSetSource.ToMonitoredItems();
            if (monitoredItems.Count == 0) {
                throw new ArgumentException(nameof(dataSetWriter.DataSet.DataSetSource));
            }
            return new SubscriptionModel {
                Connection = dataSetWriter.DataSet.DataSetSource.Connection.Clone(),
                Id = dataSetWriter.DataSetWriterId,
                MonitoredItems = monitoredItems,
                ExtensionFields = dataSetWriter.DataSet.ExtensionFields,
                Configuration = dataSetWriter.DataSet.DataSetSource
                    .ToSubscriptionConfigurationModel()
            };
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public static DataSetWriterModel ToDataSetWriterModel(this SubscriptionModel subscription) {
            return new DataSetWriterModel {
                DataSetWriterId = subscription.Id,
                DataSet = new PublishedDataSetModel {
                    Name = subscription.Id,
                    ExtensionFields = null,
                    DataSetMetaData = new DataSetMetaDataModel {
                        ConfigurationVersion = new ConfigurationVersionModel {
                            MajorVersion = 1,
                            MinorVersion = 0
                        },
                        DataSetClassId = Guid.NewGuid()
                    },
                    DataSetSource = new PublishedDataSetSourceModel {
                        Connection = subscription.Connection,
                        SubscriptionSettings = new PublishedDataSetSettingsModel {
                            PublishingInterval = subscription.Configuration.PublishingInterval,
                            LifeTimeCount = subscription.Configuration.LifetimeCount,
                            MaxKeepAliveCount = subscription.Configuration.KeepAliveCount,
                            MaxNotificationsPerPublish = subscription.Configuration.MaxNotificationsPerPublish,
                            Priority = subscription.Configuration.Priority
                        },
                        PublishedVariables = new PublishedDataItemsModel {
                            PublishedData = subscription.MonitoredItems.Select(m =>
                                new PublishedDataSetVariableModel {
                                    PublishedVariableNodeId = m.StartNodeId,
                                    MonitoringMode = m.MonitoringMode,
                                    BrowsePath = m.RelativePath,
                                    QueueSize = m.QueueSize,
                                    SamplingInterval = m.SamplingInterval,
                                  //  DataChangeFilter = m.DataChangeTrigger,
                                  //  DeadbandType = m.DeadBandType,
                                  //  DeadbandValue = m.DeadBandValue,
                                    DiscardNew = m.DiscardNew,
                                    IndexRange = m.IndexRange,
                                    Attribute = m.AttributeId,
                                    Id = m.Id,
                                    TriggerId = m.TriggerId,
                                    MetaDataProperties = null,
                                    SubstituteValue = null
                                }).ToList()
                        },
                        PublishedEvents = null
                    }
                }
            };
        }
    }
}