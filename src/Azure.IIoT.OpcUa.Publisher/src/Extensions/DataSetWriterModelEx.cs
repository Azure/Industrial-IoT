// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetWriterModelEx
    {
        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="configuration"></param>
        /// <param name="configure"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IReadOnlyList<BaseMonitoredItemModel> GetMonitoredItems(
            this DataSetWriterModel dataSetWriter, OpcUaSubscriptionOptions configuration,
            Func<PublishingQueueSettingsModel?, object?> configure)
        {
            if (dataSetWriter.DataSet == null)
            {
                throw new ArgumentException("DataSet missing,", nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSet.DataSetSource == null)
            {
                throw new ArgumentException("DataSet source missing,", nameof(dataSetWriter));
            }
            var monitoredItems = dataSetWriter.DataSet.DataSetSource.ToMonitoredItems(
                configuration, configure, dataSetWriter.DataSet.ExtensionFields);
            if (monitoredItems.Count == 0)
            {
                throw new ArgumentException("DataSet source empty.", nameof(dataSetWriter));
            }
            return monitoredItems;
        }

        /// <summary>
        /// Check whether there is anything the writer can publish
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static bool HasDataToPublish(this DataSetWriterModel? writer)
        {
            var source = writer?.DataSet?.DataSetSource;
            if (source != null)
            {
                if (source.PublishedEvents?.PublishedData?.Count > 0 ||
                    source.PublishedVariables?.PublishedData?.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Create subscription id.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="writerGroupName"></param>
        /// <param name="options"></param>
        /// <param name="publisher"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter,
            string? writerGroupName, OpcUaSubscriptionOptions options, PublisherOptions publisher)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriter);
            if (dataSetWriter.Id == null)
            {
                throw new ArgumentException("DataSetWriter Id missing.", nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException("Connection missing from data source", nameof(dataSetWriter));
            }

            var connection = dataSetWriter.DataSet.DataSetSource.Connection;

            if (connection.Group == null && options.EnableSessionPerDataSetWriterId == true)
            {
                connection = connection with
                {
                    Group = $"{writerGroupName}_{dataSetWriter.Id}"
                };
            }

            if (connection.Group == null && options.DisableSessionPerWriterGroup != true)
            {
                connection = connection with
                {
                    Group = writerGroupName
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.UseReverseConnect) &&
                options.DefaultUseReverseConnect == true)
            {
                connection = connection with
                {
                    Options = connection.Options | ConnectionOptions.UseReverseConnect
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem) &&
                options.DisableComplexTypeSystem == true)
            {
                connection = connection with
                {
                    Options = connection.Options | ConnectionOptions.NoComplexTypeSystem
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer) &&
                options.DisableSubscriptionTransfer == true)
            {
                connection = connection with
                {
                    Options = connection.Options | ConnectionOptions.NoSubscriptionTransfer
                };
            }

            var routing = dataSetWriter.DataSet.Routing ??
                publisher.DefaultDataSetRouting ?? DataSetRoutingMode.None;

            return new SubscriptionIdentifier(connection,
                dataSetWriter.DataSet.DataSetSource.ToSubscriptionModel(
                    dataSetWriter.DataSet.DataSetMetaData, options, routing != DataSetRoutingMode.None,
                    publisher.IgnoreConfiguredPublishingIntervals));
        }
    }
}
