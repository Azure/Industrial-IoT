// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Publisher.Tests.Utils;
    using FluentAssertions;
    using Furly.Extensions.Rpc;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel.Router.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Exercises the configuration direct methods through the real Furly
    /// <see cref="MethodRouter"/> dispatch path (the layer that deserializes the
    /// raw IoT Edge method payload and invokes the controller). This guards
    /// against the customer-reported case where invoking UnpublishAllNodes_V1
    /// with <c>{ "EndpointUrl": null }</c> returned a bodyless 500.
    /// </summary>
    public sealed class ConfigurationMethodRouterTests : TempFileProviderBase
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly NewtonsoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly DefaultJsonSerializer _defaultJsonSerializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly PublishedNodesConverter _publishedNodesJobConverter;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly PublisherService _publisher;
        private readonly Mock<IWriterGroupControl> _triggerMock;
        private readonly Mock<IDiagnosticCollector> _diagnostic;

        public ConfigurationMethodRouterTests(ITestOutputHelper output)
        {
            _newtonSoftJsonSerializer = new NewtonsoftJsonSerializer();
            _defaultJsonSerializer = new DefaultJsonSerializer();
            _loggerFactory = LogFactory.Create(output, Logging.Config);

            _options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            _options.Value.PublishedNodesFile = _tempFile;
            _options.Value.UseFileChangePolling = true;
            _options.Value.MessagingProfile = MessagingProfile.Get(
                MessagingMode.PubSub, MessageEncoding.Json);

            _publishedNodesJobConverter = new PublishedNodesConverter(
                _loggerFactory.CreateLogger<PublishedNodesConverter>(), _newtonSoftJsonSerializer, _options);

            CopyContent("Resources/empty_pn.json", _tempFile);

            using var factory = new PhysicalFileProviderFactory(_options,
                _loggerFactory.CreateLogger<PhysicalFileProviderFactory>());
            _publishedNodesProvider = new PublishedNodesProvider(factory, _options,
                _loggerFactory.CreateLogger<PublishedNodesProvider>());
            _triggerMock = new Mock<IWriterGroupControl>();
            var factoryMock = new Mock<IWriterGroupScopeFactory>();
            var lifetime = new Mock<IWriterGroupScope>();
            lifetime.SetupGet(l => l.WriterGroup).Returns(_triggerMock.Object);
            factoryMock
                .Setup(f => f.Create(It.IsAny<WriterGroupModel>()))
                .Returns(lifetime.Object);
            _publisher = new PublisherService(factoryMock.Object, _options,
                _loggerFactory.CreateLogger<PublisherService>());
            _diagnostic = new Mock<IDiagnosticCollector>();
            var mockDiag = new WriterGroupDiagnosticModel();
            _diagnostic.Setup(m => m.TryGetDiagnosticsForWriterGroup(It.IsAny<string>(), out mockDiag)).Returns(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loggerFactory.Dispose();
                _publishedNodesProvider.Dispose();
            }
            base.Dispose(disposing);
        }

        public static TheoryData<string> BlankEndpointPayloads => new()
        {
            "{ \"EndpointUrl\": null }",
            "{ \"EndpointUrl\": \"\" }",
            "{ \"EndpointUrl\": \"   \" }",
            "{}",
            "null"
        };

        [Theory]
        [MemberData(nameof(BlankEndpointPayloads))]
        public Task UnpublishAllNodesWithBlankEndpointReturnsResponseNewtonsoftAsync(string payload)
        {
            return UnpublishAllNodesWithBlankEndpointReturnsResponseAsync(
                payload, _newtonSoftJsonSerializer);
        }

        [Theory]
        [MemberData(nameof(BlankEndpointPayloads))]
        public Task UnpublishAllNodesWithBlankEndpointReturnsResponseDefaultAsync(string payload)
        {
            return UnpublishAllNodesWithBlankEndpointReturnsResponseAsync(
                payload, _defaultJsonSerializer);
        }

        private async Task UnpublishAllNodesWithBlankEndpointReturnsResponseAsync(
            string payload, IJsonSerializer serializer)
        {
            CopyContent("Resources/empty_pn.json", _tempFile);
            await using var configService = InitPublisherConfigService();
            var controller = new ConfigurationController(configService);

            await using var router = NewRouter(serializer, controller);

            // Invoke exactly as the IoT Edge direct method dispatch would.
            var response = await InvokeAsync(router, "UnpublishAllNodes_V1", payload);

            // Must return a real serialized PublishedNodesResponseModel body,
            // never a bodyless/unhandled failure.
            response.Should().NotBeNull();
            response.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UnpublishAllNodesWithNullEndpointPurgesConfigurationAsync()
        {
            CopyContent("Resources/empty_pn.json", _tempFile);
            await using var configService = InitPublisherConfigService();
            var controller = new ConfigurationController(configService);

            await using var router = NewRouter(_newtonSoftJsonSerializer, controller);

            // Publish a node so there is something to purge.
            var publishPayload = _newtonSoftJsonSerializer.SerializeToString(
                new PublishedNodesEntryModel
                {
                    EndpointUrl = "opc.tcp://localhost:50000",
                    OpcNodes =
                    [
                        new OpcNodeModel
                        {
                            Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1"
                        }
                    ]
                });
            await InvokeAsync(router, "PublishNodes_V1", publishPayload);
            _publisher.WriterGroups.Should().NotBeEmpty();

            // The customer payload: purge all using a null endpoint url.
            await InvokeAsync(router, "UnpublishAllNodes_V1", "{ \"EndpointUrl\": null }");

            _publisher.WriterGroups.Should().BeEmpty();
        }

        private MethodRouter NewRouter(IJsonSerializer serializer, ConfigurationController controller)
        {
            var router = new MethodRouter(Array.Empty<IRpcServer>(),
                serializer, _loggerFactory.CreateLogger<MethodRouter>())
            {
                Controllers = new[] { controller }
            };
            router.GetAwaiter().GetResult();
            return router;
        }

        private static async Task<ReadOnlyMemory<byte>> InvokeAsync(
            MethodRouter router, string method, string payload)
        {
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));
            var result = await router.InvokeAsync(method, buffer,
                "application/json", CancellationToken.None);
            return result.IsSingleSegment ? result.First : result.ToArray();
        }

        private PublishedNodesJsonServices InitPublisherConfigService()
        {
            var configService = new PublishedNodesJsonServices(
                _publishedNodesJobConverter,
                _publisher,
                _loggerFactory.CreateLogger<PublishedNodesJsonServices>(),
                _publishedNodesProvider,
                _newtonSoftJsonSerializer,
                _diagnostic.Object);
            configService.GetAwaiter().GetResult();
            return configService;
        }

        private static void CopyContent(string sourcePath, string destinationPath)
        {
            File.WriteAllText(destinationPath, File.ReadAllText(sourcePath));
        }
    }
}
