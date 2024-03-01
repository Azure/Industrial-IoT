// ------------------------------------------------------------
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
        /// <param name="configure"></param>
        /// <param name="fetchBrowsePathFromRootOverride"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter, OpcUaSubscriptionOptions configuration,
            Func<PublishingQueueSettingsModel?, object?> configure,
            bool? fetchBrowsePathFromRootOverride = null)
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
            return new SubscriptionModel
            {
                Id = ToSubscriptionId(dataSetWriter),
                MonitoredItems = monitoredItems,
                Configuration = dataSetWriter.DataSet?.DataSetSource.ToSubscriptionConfigurationModel(
                    configuration)
            };
        }

        /// <summary>
        /// Create subscription id.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter)
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
            return new SubscriptionIdentifier(connection, dataSetWriter.Id);
        }
    }
}
