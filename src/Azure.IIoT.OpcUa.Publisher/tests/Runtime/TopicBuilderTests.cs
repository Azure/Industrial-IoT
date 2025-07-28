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
            new TopicBuilder(options.Value).RootTopic.Should().Be(options.Value.PublisherId);
        }

        [Fact]
        public void TestMethodTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options.Value).MethodTopic.Should().Be($"{options.Value.PublisherId}/methods");
        }

        [Fact]
        public void TestMethodTopicBuilding2()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Method = null;
            new TopicBuilder(options.Value).MethodTopic.Should().BeEmpty();
        }

        [Fact]
        public void TestMethodTopicBuilding3()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Method = null;
            new TopicBuilder(options.Value, null, new TopicTemplatesOptions
            {
                Method = "{RootTopic}/methods"
            }).MethodTopic.Should().Be($"{options.Value.PublisherId}/methods");
        }

        [Fact]
        public void TestEventsTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options.Value).EventsTopic.Should().Be($"{options.Value.PublisherId}/EventSource/EventName");
        }

        [Fact]
        public void TestTelemetryTopicBuildingWithDefault()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/messages/Bar");
        }

        [Fact]
        public void TestTelemetryTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Telemetry = "{RootTopic}/{WriterGroup}/{DataSetWriter}";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/Bar/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding2()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Telemetry = "{RootTopic}/{Unknown}/{DataSetWriter}";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/Unknown/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding3()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Telemetry = "{RootTopic}/{TelemetryTopic}/{DataSetWriter}";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/TelemetryTopic/Foo");
        }

        [Fact]
        public void TestTelemetryTopicBuilding4()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Root = "{PublisherId}";
            options.Value.TopicTemplates.Telemetry = "{PublisherId}/{RootTopic}";
            new TopicBuilder(options.Value).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/PublisherId");
        }

        [Fact]
        public void TestTelemetryTopicBuilding5()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Root = "{TelemetryTopic}";
            options.Value.TopicTemplates.Telemetry = "{RootTopic}";
            new TopicBuilder(options.Value).TelemetryTopic.Should().Be("TelemetryTopic");
        }

        [Fact]
        public void TestTelemetryTopicBuilding6()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.TopicTemplates.Telemetry = "{TelemetryTopic}";
            new TopicBuilder(options.Value).TelemetryTopic.Should().Be("TelemetryTopic");
        }

        [Fact]
        public void TestTelemetryTopicBuilding7()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.Telemetry =
                "{RootTopic}/writer/{DataSetWriter}/group/{WriterGroup}/messages";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/writer/Foo/group/Bar/messages");
        }

        [Fact]
        public void TestTelemetryTopicBuilding8()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options.Value, null, new TopicTemplatesOptions
            {
                Telemetry = "{RootTopic}/writer/{DataSetWriter}/group/{WriterGroup}/messages"
            }, new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/writer/Foo/group/Bar/messages");
        }

        [Fact]
        public void TestTelemetryTopicBuilding9()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
#pragma warning disable CA1308 // Normalize strings to uppercase
            options.Value.TopicTemplates.Telemetry =
                "{RootTopic}/writer/{DataSetWriter}/group/{WriterGroup}/messages".ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).TelemetryTopic.Should().Be($"{options.Value.PublisherId}/writer/Foo/group/Bar/messages");
        }

        [Fact]
        public void TestMetadataTopicBuildingWithDefaultIsNull()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).DataSetMetaDataTopic.Should().BeEmpty();
        }

        [Fact]
        public void TestMetadataTopicBuilding1()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.DataSetMetaData = "{TelemetryTopic}/metadata";
            new TopicBuilder(options.Value, variables: new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).DataSetMetaDataTopic.Should().Be($"{options.Value.PublisherId}/messages/Bar/metadata");
        }

        [Fact]
        public void TestMetadataTopicBuilding2()
        {
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.PublisherId = "MyPublisher";
            options.Value.TopicTemplates.DataSetMetaData = "{TelemetryTopic}/$someothername";
            new TopicBuilder(options.Value, null, new TopicTemplatesOptions
            {
                DataSetMetaData = "{TelemetryTopic}/metadata"
            }, new Dictionary<string, string>
            {
                ["DataSetWriter"] = "Foo",
                ["WriterGroup"] = "Bar"
            }).DataSetMetaDataTopic.Should().Be($"{options.Value.PublisherId}/messages/Bar/metadata");
        }
    }
}
