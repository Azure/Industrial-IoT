// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetWriterModelEx {

        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter, IWriterGroupConfig groupConfig) {
            if (dataSetWriter == null) {
                return null;
            }
            if (dataSetWriter.DataSetWriterName == null) {
                throw new ArgumentNullException(nameof(dataSetWriter.DataSetWriterName));
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
            var model = new SubscriptionModel {
                Connection = dataSetWriter.DataSet.DataSetSource.Connection.Clone(),
                Id = dataSetWriter.DataSetWriterName,
                MonitoredItems = monitoredItems,
                ExtensionFields = dataSetWriter.DataSet.ExtensionFields,
                Configuration = dataSetWriter.DataSet.DataSetSource
                    .ToSubscriptionConfigurationModel(dataSetWriter.DataSet.DataSetMetaData)
            };

            model.Connection.Group ??= groupConfig?.WriterGroup?.WriterGroupId;
            return model;
        }
    }
}