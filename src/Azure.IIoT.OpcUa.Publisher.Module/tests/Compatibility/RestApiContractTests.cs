// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Compatibility
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Characterizes the server-independent REST contract. Fixture-backed endpoint
    /// behavior remains deliberately covered by the provisional integration tests.
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed class RestApiContractTests
    {
        [Fact]
        public async Task V2RoutesAreCompleteProtectedAndChallengeUnauthenticatedCallsAsync()
        {
            await using var app = CreateApp();
            var routes = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("v2/", StringComparison.Ordinal) == true)
                .ToArray();

            Assert.Equal(103, routes.Length);
            Assert.All(routes, endpoint => Assert.NotEmpty(
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()));

            AssertRoute(routes, "v2/pki/{store}/certs", HttpMethods.Get, HttpMethods.Patch);
            AssertRoute(routes, "v2/configuration/", HttpMethods.Get, HttpMethods.Put, HttpMethods.Patch);
            AssertRoute(routes, "v2/configuration/nodes/unpublish/all", HttpMethods.Post);
            AssertRoute(routes, "v2/diagnostics/channels/watch", HttpMethods.Get);
            AssertRoute(routes, "v2/discovery/", HttpMethods.Post);
            AssertRoute(routes, "v2/filesystem/download", HttpMethods.Get);
            AssertRoute(routes, "v2/filesystem/upload", HttpMethods.Post);
            AssertRoute(routes, "v2/browse", HttpMethods.Post);
            AssertRoute(routes, "v2/read", HttpMethods.Post);
            AssertRoute(routes, "v2/write", HttpMethods.Post);
            AssertRoute(routes, "v2/call", HttpMethods.Post);
            AssertRoute(routes, "v2/historyread/first", HttpMethods.Post);
            AssertRoute(routes, "v2/history/values/read", HttpMethods.Post);
            AssertRoute(routes, "v2/writer/", HttpMethods.Post, HttpMethods.Put);
            AssertRoute(routes, "v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes",
                HttpMethods.Get);

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/v2/connections");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MethodStatusExceptionKeepsProblemDetailsBodyAndJsonContentTypeAsync()
        {
            await using var app = CreateApp();
            app.MapGet("/contract/method-status", ThrowMethodStatus)
                .AddEndpointFilter<RestExceptionFilter>();

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/contract/method-status");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(Json.MimeType, response.Content.Headers.ContentType?.MediaType);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(409, document.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Conflict", document.RootElement.GetProperty("title").GetString());
            Assert.Equal("already exists", document.RootElement.GetProperty("detail").GetString());
        }

        private static WebApplication CreateApp()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthentication(ContractAuthenticationHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, ContractAuthenticationHandler>(
                    ContractAuthenticationHandler.Scheme, static _ => { });
            builder.Services.AddAuthorization();
            builder.Services.ConfigureHttpJsonOptions(options =>
                Json.ApplyTo(options.SerializerOptions));

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapPublisherApi();
            return app;
        }

        private static IResult ThrowMethodStatus()
        {
            throw new MethodCallStatusException(409, "already exists", "Conflict");
        }

        private static void AssertRoute(IEnumerable<RouteEndpoint> endpoints, string route,
            params string[] methods)
        {
            var metadata = endpoints
                .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText, route,
                    StringComparison.Ordinal))
                .Select(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>())
                .Where(metadata => metadata != null)
                .ToArray();
            Assert.NotEmpty(metadata);
            Assert.Equal(methods.OrderBy(method => method, StringComparer.Ordinal),
                metadata.SelectMany(metadata => metadata!.HttpMethods)
                    .OrderBy(method => method, StringComparer.Ordinal));
        }

        private sealed class ContractAuthenticationHandler
            : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public const string Scheme = "Contract";

            public ContractAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }
    }
}
