// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;

    /// <summary>
    /// Data set extensions
    /// </summary>
    internal static class DataSetWriterModelEx
    {
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
        /// Get connection to create subscription in
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="writerGroupId"></param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static ConnectionIdentifier GetConnection(this DataSetWriterModel dataSetWriter,
            string? writerGroupId, PublisherOptions options)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriter);

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
            return new ConnectionIdentifier(connection);
        }
    }
}
