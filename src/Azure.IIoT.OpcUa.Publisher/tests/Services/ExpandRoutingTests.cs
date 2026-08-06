// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="WriterGroupDataSource.ExpandRouting"/> — a static method
    /// that expands a writer group's writers according to their routing configuration.
    /// </summary>
    public sealed class ExpandRoutingTests
    {
        private static PublisherOptions CreateOptions() =>
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions().Value;

        private static DataSetWriterModel CreateVariableWriter(
            string id = "w1",
            IReadOnlyList<PublishedDataSetVariableModel>? variables = null) =>
            new DataSetWriterModel
            {
                Id = id,
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

        private static WriterGroupModel CreateGroup(
            string id = "group",
            IList<DataSetWriterModel>? writers = null) =>
            new WriterGroupModel
            {
                Id = id,
                DataSetWriters = writers?.ToList()
            };

        // ── Null guards ────────────────────────────────────────────────────────

        [Fact]
        public void ExpandRouting_NullGroup_ThrowsArgumentNullException()
        {
            var options = CreateOptions();
            Assert.Throws<ArgumentNullException>(() =>
                WriterGroupDataSource.ExpandRouting(null!, options));
        }

        [Fact]
        public void ExpandRouting_NullOptions_ThrowsArgumentNullException()
        {
            var group = CreateGroup();
            Assert.Throws<ArgumentNullException>(() =>
                WriterGroupDataSource.ExpandRouting(group, null!));
        }

        // ── No writers ────────────────────────────────────────────────────────

        [Fact]
        public void ExpandRouting_NullDataSetWriters_ReturnsSameInstance()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: null);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.Same(group, result);
        }

        [Fact]
        public void ExpandRouting_EmptyDataSetWriters_ReturnsSameInstance()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: []);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.Same(group, result);
        }

        // ── Writer without DataSetSource ───────────────────────────────────────

        [Fact]
        public void ExpandRouting_WriterWithNullDataSet_ReturnsOriginalWriter()
        {
            var options = CreateOptions();
            var nullDataSetWriter = new DataSetWriterModel { Id = "no-ds" };
            var group = CreateGroup(writers: [nullDataSetWriter]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            // No expansion happened - same group instance returned
            Assert.Same(group, result);
        }

        [Fact]
        public void ExpandRouting_WriterWithNullDataSetSource_ReturnsOriginalGroup()
        {
            var options = CreateOptions();
            var writer = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel { DataSetSource = null }
            };
            var group = CreateGroup(writers: [writer]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.Same(group, result);
        }

        // ── Writer that expands to itself ──────────────────────────────────────

        [Fact]
        public void ExpandRouting_SingleWriter_NoRouting_ReturnsSameGroupInstance()
        {
            var options = CreateOptions();
            var writer = CreateVariableWriter("w1");
            var group = CreateGroup(writers: [writer]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            // Writer with single partition that keeps its own id → no change, same instance
            Assert.Same(group, result);
        }

        [Fact]
        public void ExpandRouting_TwoWriters_NoRouting_ReturnsSameGroupInstance()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers:
            [
                CreateVariableWriter("w1"),
                CreateVariableWriter("w2")
            ]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.Same(group, result);
        }

        // ── Writer that expands to multiple partitions ─────────────────────────

        [Fact]
        public void ExpandRouting_WriterWithVariablesAndEvents_ReturnsExpandedGroup()
        {
            var options = CreateOptions();
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
            var group = CreateGroup(writers: [writer]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            // Variables + Events expand into at least 2 writers → changed → new instance
            Assert.NotSame(group, result);
            Assert.True(result.DataSetWriters!.Count >= 2);
        }

        [Fact]
        public void ExpandRouting_MixedWriters_OnlyExpandedWriterChanges()
        {
            var options = CreateOptions();
            var simpleWriter = CreateVariableWriter("simple");
            var complexWriter = new DataSetWriterModel
            {
                Id = "complex",
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
            var group = CreateGroup(writers: [simpleWriter, complexWriter]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.NotSame(group, result);
            // Total writers = simpleWriter (1) + complexWriter expanded (at least 2) ≥ 3
            Assert.True(result.DataSetWriters!.Count >= 3);
        }

        [Fact]
        public void ExpandRouting_ExpandedGroup_PreservesGroupId()
        {
            var options = CreateOptions();
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
            var group = CreateGroup("original-group-id", writers: [writer]);

            var result = WriterGroupDataSource.ExpandRouting(group, options);

            Assert.Equal("original-group-id", result.Id);
        }
    }
}
