// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Json;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using ModelContextProtocol.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Verifies the web host startup wiring, in particular that the minimal API
    /// JSON options are configured with the shared serializer settings so the
    /// REST surface keeps the exact same wire format as the rest of the pipeline
    /// (and the direct method / SDK path).
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed class StartupTests
    {
        [Fact]
        public void ConfigureServicesAppliesSharedJsonOptionsToHttp()
        {
            using var provider = BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value;

            Assert.Same(JsonNamingPolicy.CamelCase,
                options.SerializerOptions.PropertyNamingPolicy);
            Assert.True(options.SerializerOptions.PropertyNameCaseInsensitive);
            Assert.Equal(
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals,
                options.SerializerOptions.NumberHandling);
        }

        [Fact]
        public void ConfigureServicesRegistersHttpJsonConverters()
        {
            using var provider = BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value;

            // The shared converter set (matrix, byte array, enum, ...) is applied
            // so the minimal API endpoints (de)serialize identically to the SDK.
            Assert.NotEmpty(options.SerializerOptions.Converters);
        }

        [Fact]
        public async Task MinimalApiUsesDataContractWireFormatAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.ConfigureHttpJsonOptions(options =>
                Json.ApplyTo(options.SerializerOptions));
            await using var app = builder.Build();
            app.MapPost("/published-nodes", (PublishedNodesEntryModel model) => model);
            await app.StartAsync();
            using var client = app.GetTestClient();

            var publishedNodesUri = new Uri("/published-nodes", UriKind.Relative);
            var dataSetClassId = Guid.NewGuid();
            using var content = new StringContent(
                $"{{\"DataSetClassId\":\"{dataSetClassId}\"}}",
                Encoding.UTF8, Json.MimeType);
            using var response = await client.PostAsync(publishedNodesUri, content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            Assert.Equal(dataSetClassId,
                document.RootElement.GetProperty("DataSetClassId").GetGuid());
            Assert.False(document.RootElement.TryGetProperty("dataSetClassId", out _));

            using var emptyContent = new StringContent("{}", Encoding.UTF8, Json.MimeType);
            using var emptyResponse = await client.PostAsync(publishedNodesUri, emptyContent);
            emptyResponse.EnsureSuccessStatusCode();
            using var emptyDocument = JsonDocument.Parse(
                await emptyResponse.Content.ReadAsStringAsync());
            Assert.False(emptyDocument.RootElement.TryGetProperty("DataSetClassId", out _));
        }

        [Fact]
        public void McpToolsAreNotRegisteredUnlessEnabled()
        {
            using var provider = BuildServiceProvider();

            Assert.Empty(provider.GetServices<McpServerTool>());
        }

        [Fact]
        public void McpToolsAreRegisteredWhenEnabled()
        {
            using var provider = BuildServiceProvider(mcpEnabled: true);

            var tools = provider.GetServices<McpServerTool>()
                .Select(tool => tool.ProtocolTool.Name)
                .ToHashSet(StringComparer.Ordinal);

            // The core service tools and the diagnostics tools, which follow
            // --mcp rather than being separately opted into.
            Assert.Contains("Connect", tools);
            Assert.Contains("Browse", tools);
            Assert.Contains("start_capture", tools);
        }

        /// <summary>
        /// The MCP endpoint must be protected exactly like the REST api. Mapping it
        /// inside the authenticated pipeline is necessary but not sufficient, so this
        /// asserts the authorization metadata is actually present on the endpoint the
        /// production Startup produces.
        /// </summary>
        [Fact]
        public void McpEndpointRequiresAuthorization()
        {
            var (app, _) = BuildApplication(mcpEnabled: true);
            using (app)
            {
                var mcpEndpoints = app.Services.GetRequiredService<EndpointDataSource>()
                    .Endpoints
                    .OfType<RouteEndpoint>()
                    .Where(e => e.RoutePattern.RawText?.StartsWith("/mcp",
                        StringComparison.Ordinal) == true)
                    .ToList();

                Assert.NotEmpty(mcpEndpoints);
                Assert.All(mcpEndpoints, endpoint =>
                    Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()));
            }
        }

        [Fact]
        public void McpEndpointIsAbsentUnlessEnabled()
        {
            var (app, _) = BuildApplication(mcpEnabled: false);
            using (app)
            {
                var mcpEndpoints = app.Services.GetRequiredService<EndpointDataSource>()
                    .Endpoints
                    .OfType<RouteEndpoint>()
                    .Where(e => e.RoutePattern.RawText?.StartsWith("/mcp",
                        StringComparison.Ordinal) == true);

                Assert.Empty(mcpEndpoints);
            }
        }

        private static (WebApplication App, Startup Startup) BuildApplication(bool mcpEnabled)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(McpSettings(mcpEnabled));

            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();
            startup.Configure(app);
            return (app, startup);
        }

        private static Dictionary<string, string?> McpSettings(bool mcpEnabled)
        {
            return mcpEnabled
                ? new Dictionary<string, string?>
                {
                    [PublisherConfig.EnableMcpServerKey] = "true"
                }
                : [];
        }

        private static ServiceProvider BuildServiceProvider(bool mcpEnabled = false)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(McpSettings(mcpEnabled))
                .Build();
            var services = new ServiceCollection();
            new Startup(configuration).ConfigureServices(services);
            return services.BuildServiceProvider();
        }
    }
}
