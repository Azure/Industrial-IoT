// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DataSetWriterModelEx.HasDataToPublish"/> and
    /// <see cref="DataSetWriterModelEx.GetConnection"/>.
    /// </summary>
    public sealed class DataSetWriterModelExTests
    {
        private static PublisherOptions CreateOptions() =>
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions().Value;

        private static ConnectionModel CreateConnection(string url = "opc.tcp://localhost:4840") =>
            new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = url }
            };

        private static DataSetWriterModel CreateVariableWriter(
            string id = "w1",
            ConnectionModel? connection = null) =>
            new DataSetWriterModel
            {
                Id = id,
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        Connection = connection ?? CreateConnection(),
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = [new PublishedDataSetVariableModel
                            {
                                Id = "field1",
                                PublishedVariableNodeId = "ns=1;i=1000"
                            }]
                        }
                    }
                }
            };

        // ── HasDataToPublish ───────────────────────────────────────────────────

        [Fact]
        public void HasDataToPublish_NullWriter_ReturnsFalse()
        {
            DataSetWriterModel? writer = null;
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithNullDataSet_ReturnsFalse()
        {
            var writer = new DataSetWriterModel { Id = "w1" };
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithNullDataSetSource_ReturnsFalse()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel()
            };
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithNullPublishedLists_ReturnsFalse()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel()
                }
            };
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithEmptyVariables_ReturnsFalse()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = []
                        }
                    }
                }
            };
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithEmptyEvents_ReturnsFalse()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData = []
                        }
                    }
                }
            };
            Assert.False(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithOneVariable_ReturnsTrue()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData =
                            [
                                new PublishedDataSetVariableModel { Id = "v1" }
                            ]
                        }
                    }
                }
            };
            Assert.True(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithOneEvent_ReturnsTrue()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData =
                            [
                                new PublishedDataSetEventModel { Id = "e1" }
                            ]
                        }
                    }
                }
            };
            Assert.True(writer.HasDataToPublish());
        }

        [Fact]
        public void HasDataToPublish_WriterWithBothVariablesAndEvents_ReturnsTrue()
        {
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = [new PublishedDataSetVariableModel { Id = "v1" }]
                        },
                        PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData = [new PublishedDataSetEventModel { Id = "e1" }]
                        }
                    }
                }
            };
            Assert.True(writer.HasDataToPublish());
        }

        // ── GetConnection ──────────────────────────────────────────────────────

        [Fact]
        public void GetConnection_NullWriter_ThrowsArgumentNullException()
        {
            var options = CreateOptions();
            DataSetWriterModel? nullWriter = null;
            Assert.Throws<ArgumentNullException>(() =>
                nullWriter!.GetConnection("g1", options));
        }

        [Fact]
        public void GetConnection_NullDataSetSource_ThrowsArgumentException()
        {
            var options = CreateOptions();
            var writer = new DataSetWriterModel { Id = "w1" };
            Assert.Throws<ArgumentException>(() =>
                writer.GetConnection("g1", options));
        }

        [Fact]
        public void GetConnection_NullConnection_ThrowsArgumentException()
        {
            var options = CreateOptions();
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel()
                }
            };
            Assert.Throws<ArgumentException>(() =>
                writer.GetConnection("g1", options));
        }

        [Fact]
        public void GetConnection_DisableSessionPerWriterGroupFalse_SetsGroup()
        {
            var options = CreateOptions();
            options.DisableSessionPerWriterGroup = false;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("my-group", options);
            Assert.Equal("my-group", result.Connection.Group);
        }

        [Fact]
        public void GetConnection_DisableSessionPerWriterGroupTrue_DoesNotSetGroup()
        {
            var options = CreateOptions();
            options.DisableSessionPerWriterGroup = true;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("my-group", options);
            Assert.Null(result.Connection.Group);
        }

        [Fact]
        public void GetConnection_GroupAlreadySet_PreservesExistingGroup()
        {
            var options = CreateOptions();
            options.DisableSessionPerWriterGroup = false;
            var conn = CreateConnection() with { Group = "existing-group" };
            var writer = CreateVariableWriter(connection: conn);

            var result = writer.GetConnection("new-group", options);
            Assert.Equal("existing-group", result.Connection.Group);
        }

        [Fact]
        public void GetConnection_DefaultUseReverseConnectTrue_SetsFlag()
        {
            var options = CreateOptions();
            options.DefaultUseReverseConnect = true;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("g1", options);
            Assert.True(result.Connection.Options.HasFlag(ConnectionOptions.UseReverseConnect));
        }

        [Fact]
        public void GetConnection_DefaultUseReverseConnectFalse_DoesNotSetFlag()
        {
            var options = CreateOptions();
            options.DefaultUseReverseConnect = false;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("g1", options);
            Assert.False(result.Connection.Options.HasFlag(ConnectionOptions.UseReverseConnect));
        }

        [Fact]
        public void GetConnection_FlagAlreadySet_NotDuplicated()
        {
            var options = CreateOptions();
            options.DefaultUseReverseConnect = true;
            var conn = CreateConnection() with
            {
                Options = ConnectionOptions.UseReverseConnect
            };
            var writer = CreateVariableWriter(connection: conn);

            var result = writer.GetConnection("g1", options);
            Assert.True(result.Connection.Options.HasFlag(ConnectionOptions.UseReverseConnect));
        }

        [Fact]
        public void GetConnection_DisableComplexTypeSystemTrue_SetsFlag()
        {
            var options = CreateOptions();
            options.DisableComplexTypeSystem = true;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("g1", options);
            Assert.True(result.Connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem));
        }

        [Fact]
        public void GetConnection_DisableSubscriptionTransferTrue_SetsFlag()
        {
            var options = CreateOptions();
            options.DisableSubscriptionTransfer = true;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("g1", options);
            Assert.True(result.Connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer));
        }

        [Fact]
        public void GetConnection_AllOptionsFalse_NoFlagsSet()
        {
            var options = CreateOptions();
            options.DefaultUseReverseConnect = false;
            options.DisableComplexTypeSystem = false;
            options.DisableSubscriptionTransfer = false;
            var writer = CreateVariableWriter();

            var result = writer.GetConnection("g1", options);
            Assert.Equal(ConnectionOptions.None, result.Connection.Options);
        }
    }
}
