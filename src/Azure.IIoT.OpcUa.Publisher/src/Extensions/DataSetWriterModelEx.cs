﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;

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
        /// <param name="writerGroupId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter, OpcUaSubscriptionOptions configuration,
            string? writerGroupId = null)
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
                configuration, dataSetWriter.DataSet.ExtensionFields);
            if (monitoredItems.Count == 0)
            {
                throw new ArgumentException("DataSet source empty.", nameof(dataSetWriter));
            }
            return new SubscriptionModel
            {
                Id = ToSubscriptionId(dataSetWriter, writerGroupId, configuration),
                MonitoredItems = monitoredItems,
                Configuration = dataSetWriter.DataSet?.DataSetSource.ToSubscriptionConfigurationModel(
                    dataSetWriter.DataSet.DataSetMetaData, configuration)
            };
        }

        /// <summary>
        /// Create subscription id.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="writerGroupId"></param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter,
            string? writerGroupId, OpcUaSubscriptionOptions options)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriter);
            if (dataSetWriter.DataSetWriterName == null)
            {
                throw new ArgumentException("DataSetWriterName missing.", nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException("Connection missing from data source", nameof(dataSetWriter));
            }

            var connection = dataSetWriter.DataSet.DataSetSource.Connection;

            if (connection.Group == null && options.DisableSessionPerWriterGroup != true)
            {
                connection = connection with
                {
                    Group = writerGroupId
                };
            }

            if (connection.IsReverse == null && options.DefaultUseReverseConnect == true)
            {
                connection = connection with
                {
                    IsReverse = options.DefaultUseReverseConnect
                };
            }
            return new SubscriptionIdentifier(connection, dataSetWriter.DataSetWriterName);
        }
    }
}
