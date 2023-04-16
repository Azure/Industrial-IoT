// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Diagnostics;

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
            var monitoredItems = dataSetWriter.DataSet.DataSetSource.ToMonitoredItems(configuration);
            if (monitoredItems.Count == 0)
            {
                throw new ArgumentException("DataSet source empty.", nameof(dataSetWriter));
            }
            return new SubscriptionModel
            {
                Id = ToSubscriptionId(dataSetWriter, writerGroupId),
                MonitoredItems = monitoredItems,
                ExtensionFields = dataSetWriter.DataSet?.ExtensionFields,
                Configuration = dataSetWriter.DataSet?.DataSetSource.ToSubscriptionConfigurationModel(
                    dataSetWriter.DataSet.DataSetMetaData, configuration)
            };
        }

        /// <summary>
        /// Create subscription id.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="writerGroupId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter,
            string? writerGroupId = null)
        {
            if (dataSetWriter == null)
            {
                throw new ArgumentNullException(nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSetWriterName == null)
            {
                throw new ArgumentException("DataSetWriterName missing.", nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException("Connection missing from data source", nameof(dataSetWriter));
            }

            var connection = dataSetWriter.DataSet.DataSetSource.Connection;

            // TODO: We should get rid of this in connection model, it is redundant in the model tree
            if (connection.Group == null)
            {
                connection = connection.Clone();
                Debug.Assert(connection != null);
                connection.Group = writerGroupId;
            }
            return new SubscriptionIdentifier(connection, dataSetWriter.DataSetWriterName);
        }
    }
}
