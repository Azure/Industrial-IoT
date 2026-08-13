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
    using System.Globalization;
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

        /// <summary>
        /// The MCP tools can extract secure channel keys and read capture
        /// artifacts, and the unsecure listener carries the api key in
        /// cleartext. Reaching /mcp over that listener would hand both to
        /// anyone on the network path, so it must not be served there even when
        /// the operator has deliberately turned the plaintext port on.
        /// </summary>
        [Fact]
        public async Task McpEndpointIsNotServedOnTheUnsecureListenerAsync()
        {
            var (app, _) = BuildApplication(mcpEnabled: true, unsecureHttp: true);
            await using (app)
            {
                await app.StartAsync();

                var unsecurePort = Module.Runtime.Configuration.Kestrel.GetListenPorts(
                    app.Services.GetRequiredService<
                        IOptions<PublisherOptions>>().Value).UnsecurePort;
                Assert.NotNull(unsecurePort);

                var apiKey = app.Services.GetRequiredService<IApiKeyProvider>().ApiKey;
                Assert.False(string.IsNullOrEmpty(apiKey));

                var server = app.GetTestServer();

                var blocked = await server.SendAsync(context =>
                {
                    context.Request.Scheme = "https";

                    context.Request.Host = new HostString("localhost");

                    context.Request.Method = HttpMethods.Post;
                    context.Request.Path = "/mcp";

                    context.Request.Headers.Authorization = "ApiKey " + apiKey;
                    context.Connection.LocalPort = unsecurePort.Value;
                });
                Assert.Equal(403, blocked.Response.StatusCode);

                // A different local port must still reach the endpoint, where
                // authentication -- not the guard -- is what rejects it.
                var reachable = await server.SendAsync(context =>
                {
                    context.Request.Scheme = "https";

                    context.Request.Host = new HostString("localhost");

                    context.Request.Method = HttpMethods.Post;
                    context.Request.Path = "/mcp";

                    context.Request.Headers.Authorization = "ApiKey " + apiKey;
                    context.Connection.LocalPort = unsecurePort.Value + 1;
                });
                Assert.NotEqual(403, reachable.Response.StatusCode);

                await app.StopAsync();
            }
        }

        /// <summary>
        /// The guard must not affect the REST api, whose exposure on the
        /// unsecure listener is a separate, pre-existing decision.
        /// </summary>
        [Fact]
        public async Task RestApiIsStillServedOnTheUnsecureListenerAsync()
        {
            var (app, _) = BuildApplication(mcpEnabled: true, unsecureHttp: true);
            await using (app)
            {
                await app.StartAsync();

                var unsecurePort = Module.Runtime.Configuration.Kestrel.GetListenPorts(
                    app.Services.GetRequiredService<
                        IOptions<PublisherOptions>>().Value).UnsecurePort;
                Assert.NotNull(unsecurePort);

                var apiKey = app.Services.GetRequiredService<IApiKeyProvider>().ApiKey;
                Assert.False(string.IsNullOrEmpty(apiKey));

                var response = await app.GetTestServer().SendAsync(context =>
                {
                    context.Request.Scheme = "https";

                    context.Request.Host = new HostString("localhost");

                    context.Request.Method = HttpMethods.Get;
                    context.Request.Path = "/v2/configuration";

                    context.Request.Headers.Authorization = "ApiKey " + apiKey;
                    context.Connection.LocalPort = unsecurePort.Value;
                });

                Assert.NotEqual(403, response.Response.StatusCode);

                await app.StopAsync();
            }
        }

        /// <summary>
        /// The plaintext listener is opt in, so the default configuration must
        /// not produce one at all. This is the Startup-level counterpart to the
        /// binding test in PublisherConfigTests.
        /// </summary>
        [Fact]
        public void UnsecureListenerIsAbsentUnlessAskedFor()
        {
            var (app, _) = BuildApplication(mcpEnabled: true);
            using (app)
            {
                var ports = Module.Runtime.Configuration.Kestrel.GetListenPorts(
                    app.Services.GetRequiredService<
                        IOptions<PublisherOptions>>().Value);

                Assert.Null(ports.UnsecurePort);
                Assert.NotNull(ports.SecurePort);
            }
        }

        private static (WebApplication App, Startup Startup) BuildApplication(
            bool mcpEnabled, bool unsecureHttp = false)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(
                McpSettings(mcpEnabled, unsecureHttp));

            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();
            startup.Configure(app);
            return (app, startup);
        }

        private static Dictionary<string, string?> McpSettings(
            bool mcpEnabled, bool unsecureHttp = false)
        {
            var settings = new Dictionary<string, string?>();
            if (mcpEnabled)
            {
                settings[PublisherConfig.EnableMcpServerKey] = "true";
                settings[PublisherConfig.ApiKeyOverrideKey] = kTestApiKey;
            }
            if (unsecureHttp)
            {
                // The plaintext listener is opt in, so a test that is about
                // what happens on it has to ask for it explicitly.
                settings[PublisherConfig.UnsecureHttpServerPortKey] =
                    PublisherConfig.UnsecureHttpServerPortDefault
                        .ToString(CultureInfo.InvariantCulture);
            }
            return settings;
        }

        private const string kTestApiKey = "test-api-key-for-startup-tests";

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
