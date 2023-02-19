// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using System;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetWriterModelEx {

        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter, ISubscriptionConfig configuration,
            string writerGroupId = null) {
            if (dataSetWriter == null) {
                return null;
            }
            if (dataSetWriter.DataSet == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSet));
            }
            if (dataSetWriter.DataSet.DataSetSource == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSet.DataSetSource));
            }
            var monitoredItems = dataSetWriter.DataSet.DataSetSource.ToMonitoredItems(configuration);
            if (monitoredItems.Count == 0) {
                throw new ArgumentException(nameof(dataSetWriter.DataSet.DataSetSource));
            }
            var model = new SubscriptionModel {
                Id = ToSubscriptionId(dataSetWriter, writerGroupId),
                MonitoredItems = monitoredItems,
                ExtensionFields = dataSetWriter.DataSet.ExtensionFields,
                Configuration = dataSetWriter.DataSet.DataSetSource.ToSubscriptionConfigurationModel(
                    dataSetWriter.DataSet.DataSetMetaData, configuration)
            };
            return model;
        }


        /// <summary>
        /// Create subscription id.
        /// </summary>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter,
            string writerGroupId = null) {
            if (dataSetWriter == null) {
                return null;
            }
            if (dataSetWriter.DataSetWriterName == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSetWriterName));
            }
            if (dataSetWriter.DataSet.DataSetSource.Connection == null) {
                throw new ArgumentNullException(
                    nameof(dataSetWriter.DataSet.DataSetSource.Connection));
            }

            var connection = dataSetWriter.DataSet.DataSetSource.Connection;

            // TODO: We should get rid of this in connection model, it is redundant in the model tree
            if (connection.Group == null) {
                connection = dataSetWriter.DataSet.DataSetSource.Connection.Clone();
                connection.Group = writerGroupId;
            }
            return new SubscriptionIdentifier(connection, dataSetWriter.DataSetWriterName);
        }
    }
}