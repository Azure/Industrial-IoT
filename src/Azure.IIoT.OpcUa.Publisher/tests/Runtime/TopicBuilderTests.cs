// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using Xunit;

    public class TopicBuilderTests
    {
        [Fact]
        public void TestRootTopicBuilding()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options).RootTopic.Should().Be(options.Value.PublisherId);
        }

        [Fact]
        public void TestMethodTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options).MethodTopic.Should().Be($"{options.Value.PublisherId}/methods");
        }

        [Fact]
        public void TestMethodTopicBuilding2()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.MethodTopicTemplate = null;
            new TopicBuilder(options).MethodTopic.Should().BeEmpty();
        }

        [Fact]
        public void TestEventsTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options).EventsTopic.Should().Be($"{options.Value.PublisherId}/events");
        }

        [Fact]
        public void TestTelemetryTopicBuildingWithDefault()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/messages/Bar");
        }

        [Fact]
        public void TestTelemetryTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TelemetryTopicTemplate = "{RootTopic}/{DataSetWriterGroup}/{DataSetWriterName}";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/Bar/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding2()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TelemetryTopicTemplate = "{RootTopic}/{Unknown}/{DataSetWriterName}";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/Unknown/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding3()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TelemetryTopicTemplate = "{RootTopic}/{TelemetryTopic}/{DataSetWriterName}";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/TelemetryTopic/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding4()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.RootTopicTemplate = "{PublisherId}";
            options.Value.TelemetryTopicTemplate = "{PublisherId}/{RootTopic}";
            new TopicBuilder(options).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/PublisherId");
        }

        [Fact]
        public void TestTelemetryTopicBuilding5()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.RootTopicTemplate = "{TelemetryTopic}";
            options.Value.TelemetryTopicTemplate = "{RootTopic}";
            new TopicBuilder(options).TelemetryTopic.Should().Be("TelemetryTopic");
        }

        [Fact]
        public void TestTelemetryTopicBuilding6()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.TelemetryTopicTemplate = "{TelemetryTopic}";
            new TopicBuilder(options).TelemetryTopic.Should().Be("TelemetryTopic");
        }

        [Fact]
        public void TestTelemetryTopicBuilding7()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TelemetryTopicTemplate =
                "{RootTopic}/writer/{DataSetWriterName}/group/{DataSetWriterGroup}/messages";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/writer/Foo/group/Bar/messages");
        }

        [Fact]
        public void TestTelemetryTopicBuilding8()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
#pragma warning disable CA1308 // Normalize strings to uppercase
            options.Value.TelemetryTopicTemplate =
                "{RootTopic}/writer/{DataSetWriterName}/group/{DataSetWriterGroup}/messages".ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/writer/Foo/group/Bar/messages");
        }

        [Fact]
        public void TestMetadataTopicBuildingWithDefaultIsNull()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).DataSetMetaDataTopic.Should().BeEmpty();
        }

        [Fact]
        public void TestMetadataTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.DataSetMetaDataTopicTemplate = "{TelemetryTopic}/$metadata";
            new TopicBuilder(options, new Dictionary<string, string>
            {
                ["DataSetWriterName"] = "Foo",
                ["DataSetWriterGroup"] = "Bar"
            }).DataSetMetaDataTopic.Should().Be($"{options.Value.PublisherId}/messages/Bar/$metadata");
        }
    }
}
