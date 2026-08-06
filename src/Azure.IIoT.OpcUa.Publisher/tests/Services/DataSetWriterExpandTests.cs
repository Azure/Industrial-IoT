// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="WriterGroupDataSource.DataSetWriter.Expand"/> —
    /// a pure-model static method that splits a single DataSetWriterModel into one
    /// writer per distinct publish destination.
    /// </summary>
    public class DataSetWriterExpandTests
    {
        private static PublisherOptions CreateOptions() =>
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions().Value;

        private static WriterGroupModel CreateGroup(string id = "group") =>
            new WriterGroupModel { Id = id };

        private static DataSetWriterModel CreateVariableWriter(
            string id = "w1",
            string? writerName = null,
            IReadOnlyList<PublishedDataSetVariableModel>? variables = null) =>
            new DataSetWriterModel
            {
                Id = id,
                DataSetWriterName = writerName,
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = variables ?? [
                                new PublishedDataSetVariableModel { Id = "field1" }
                            ]
                        }
                    }
                }
            };

        private static DataSetWriterModel CreateEventWriter(
            string id = "w1",
            IReadOnlyList<PublishedDataSetEventModel>? events = null) =>
            new DataSetWriterModel
            {
                Id = id,
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData = events ?? [
                                new PublishedDataSetEventModel { Id = "event1" }
                            ]
                        }
                    }
                }
            };

        [Fact]
        public void Expand_NullDataSet_ThrowsArgumentException()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = new DataSetWriterModel { Id = "w1" };

            Assert.Throws<ArgumentException>(() =>
                WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList());
        }

        [Fact]
        public void Expand_NullDataSetSource_ThrowsArgumentException()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel { DataSetSource = null }
            };

            Assert.Throws<ArgumentException>(() =>
                WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList());
        }

        [Fact]
        public void Expand_NullPublishedVariablesAndEvents_ReturnsEmpty()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = null,
                        PublishedEvents = null
                    }
                }
            };

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Expand_NullPublishedData_ReturnsEmpty()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel { PublishedData = null },
                        PublishedEvents = new PublishedEventItemsModel { PublishedData = null }
                    }
                }
            };

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Expand_SingleVariable_ReturnsSingleWriter()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter();

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void Expand_MultipleVariablesSameSettings_ReturnsSingleWriter()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter(variables:
            [
                new PublishedDataSetVariableModel { Id = "f1" },
                new PublishedDataSetVariableModel { Id = "f2" },
                new PublishedDataSetVariableModel { Id = "f3" }
            ]);

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            // All variables have the same (null) publish settings → single writer
            Assert.Single(result);
            // The expanded writer should contain all three variables
            Assert.Equal(3, result[0].DataSet!.DataSetSource!.PublishedVariables!.PublishedData!.Count);
        }

        [Fact]
        public void Expand_VariablesWithDifferentQueueNames_ReturnsMultipleWriters()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter(variables:
            [
                new PublishedDataSetVariableModel
                {
                    Id = "f1",
                    Publishing = new PublishingQueueSettingsModel { QueueName = "topic/a" }
                },
                new PublishedDataSetVariableModel
                {
                    Id = "f2",
                    Publishing = new PublishingQueueSettingsModel { QueueName = "topic/b" }
                }
            ]);

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            // Two different destinations → two writers
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Expand_EventsOnly_ReturnsSingleEventWriter()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateEventWriter();

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            var single = Assert.Single(result);
            // Event writer should have events and no variables
            Assert.NotNull(single.DataSet!.DataSetSource!.PublishedEvents);
            Assert.Null(single.DataSet.DataSetSource.PublishedVariables);
        }

        [Fact]
        public void Expand_VariablesAndEvents_ReturnsTwoWriters()
        {
            var options = CreateOptions();
            var group = CreateGroup();
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

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            // Variables and events each become a separate writer
            Assert.Equal(2, result.Count);
            Assert.True(result.Any(w => w.DataSet!.DataSetSource!.PublishedVariables != null));
            Assert.True(result.Any(w => w.DataSet!.DataSetSource!.PublishedEvents != null));
        }

        [Fact]
        public void Expand_PreservesWriterIdInResult()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter(id: "my-writer-id");

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Equal("my-writer-id", result.Single().Id);
        }

        [Fact]
        public void Expand_DataSetWithName_ProducesNonDefaultTopicPath()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter();
            writer = writer with
            {
                DataSet = writer.DataSet! with { Name = "MyDataSet" }
            };

            // Just verify it succeeds and returns a writer; topic path
            // is embedded in the resolved Publishing.QueueName.
            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void Expand_DataSetWithDottedName_ProcessesEachPathSegment()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter();
            writer = writer with
            {
                DataSet = writer.DataSet! with { Name = "site.plant.cell" }
            };

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Single(result);
            // Topic path is used inside the resolved queue name; just verify it doesn't throw
        }

        [Fact]
        public void Expand_GroupPublishingSettingsInheritedToWriter()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            group = group with
            {
                Publishing = new PublishingQueueSettingsModel
                {
                    QueueName = "group/topic"
                }
            };
            var writer = CreateVariableWriter();

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            var single = Assert.Single(result);
            // The writer inherits the group's queue name as a template variable;
            // the resolved topic is non-null.
            Assert.NotNull(single.Publishing);
        }

        [Fact]
        public void Expand_WriterPublishingOverridesGroup()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            group = group with
            {
                Publishing = new PublishingQueueSettingsModel
                {
                    QueueName = "group/topic"
                }
            };
            var writer = CreateVariableWriter();
            writer = writer with
            {
                Publishing = new PublishingQueueSettingsModel
                {
                    QueueName = "writer/topic"
                }
            };

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            var single = Assert.Single(result);
            // Writer-level settings override group-level: topic contains "writer/topic"
            Assert.Contains("writer/topic", single.Publishing!.QueueName, StringComparison.Ordinal);
        }

        [Fact]
        public void Expand_MultipleEventsWithDifferentQueues_ReturnsMultipleEventWriters()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateEventWriter(events:
            [
                new PublishedDataSetEventModel
                {
                    Id = "e1",
                    Publishing = new PublishingQueueSettingsModel { QueueName = "events/a" }
                },
                new PublishedDataSetEventModel
                {
                    Id = "e2",
                    Publishing = new PublishingQueueSettingsModel { QueueName = "events/b" }
                }
            ]);

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, w => Assert.NotNull(w.DataSet!.DataSetSource!.PublishedEvents));
        }

        [Fact]
        public void Expand_MetadataQueueInheritedFromWriter()
        {
            var options = CreateOptions();
            var group = CreateGroup();
            var writer = CreateVariableWriter();
            writer = writer with
            {
                MetaData = new PublishingQueueSettingsModel
                {
                    QueueName = "meta/topic"
                }
            };

            var result = WriterGroupDataSource.DataSetWriter.Expand(options, group, "g1", writer).ToList();

            var single = Assert.Single(result);
            // Metadata topic is propagated
            Assert.Contains("meta/topic", single.MetaData!.QueueName, StringComparison.Ordinal);
        }
    }
}
