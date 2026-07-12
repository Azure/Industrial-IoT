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

            Assert.All(routes, endpoint => Assert.NotEmpty(
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()));
            var actual = routes
                .SelectMany(endpoint => endpoint.Metadata
                    .GetMetadata<HttpMethodMetadata>()!.HttpMethods
                    .Select(method => method + " " + endpoint.RoutePattern.RawText))
                .ToHashSet(StringComparer.Ordinal);
            Assert.True(ContractAuthenticationHandler.kExpectedRoutes.SetEquals(actual),
                "Missing: " + string.Join(", ", ContractAuthenticationHandler.kExpectedRoutes.Except(actual).Order()) +
                Environment.NewLine + "Unexpected: " +
                string.Join(", ", actual.Except(ContractAuthenticationHandler.kExpectedRoutes).Order()));

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

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

        private sealed class ContractAuthenticationHandler
            : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public const string Scheme = "Contract";

            public ContractAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            private static readonly IReadOnlySet<string> kExpectedRoutes =
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "GET v2/pki/{store}/certs",
                    "GET v2/pki/{store}/crls",
                    "PATCH v2/pki/{store}/certs",
                    "PATCH v2/pki/{store}/crls",
                    "POST v2/pki/trusted/certs",
                    "POST v2/pki/rejected/certs/{thumbprint}/approve",
                    "POST v2/pki/https/certs",
                    "DELETE v2/pki/{store}/certs/{thumbprint}",
                    "DELETE v2/pki/{store}/crls",
                    "DELETE v2/pki/{store}",

                    "POST v2/configuration/start",
                    "POST v2/configuration/stop",
                    "POST v2/configuration/bulk",
                    "POST v2/configuration/list",
                    "POST v2/configuration/nodes",
                    "POST v2/configuration/nodes/unpublish",
                    "POST v2/configuration/nodes/unpublish/all",
                    "PATCH v2/configuration/",
                    "GET v2/configuration/",
                    "PUT v2/configuration/",
                    "POST v2/configuration/endpoints/list/nodes",
                    "POST v2/configuration/diagnostics",

                    "GET v2/reset",
                    "GET v2/connections",
                    "GET v2/diagnostics/writergroups/{dataSetWriterGroup}",
                    "GET v2/diagnostics/writergroups",
                    "POST v2/diagnostics/writergroups/{dataSetWriterGroup}/keyframe",
                    "POST v2/diagnostics/writergroups/{dataSetWriterGroup}/writers/{dataSetWriterId}/keyframe",
                    "GET v2/diagnostics/connections",
                    "GET v2/diagnostics/channels",
                    "GET v2/diagnostics/channels/watch",

                    "POST v2/discovery/findserver",
                    "POST v2/discovery/register",
                    "POST v2/discovery/",
                    "POST v2/discovery/cancel",

                    "POST v2/filesystem/list",
                    "POST v2/filesystem/list/directories",
                    "POST v2/filesystem/list/files",
                    "POST v2/filesystem/parent",
                    "POST v2/filesystem/info/file",
                    "POST v2/filesystem/create/file/{name}",
                    "POST v2/filesystem/create/directory/{name}",
                    "POST v2/filesystem/delete",
                    "POST v2/filesystem/delete/{fileOrDirectoryNodeId}",
                    "GET v2/filesystem/download",
                    "POST v2/filesystem/upload",

                    "POST v2/capabilities",
                    "POST v2/browse/first",
                    "POST v2/browse/next",
                    "POST v2/browse",
                    "POST v2/browse/path",
                    "POST v2/read",
                    "POST v2/write",
                    "POST v2/metadata",
                    "POST v2/query/compile",
                    "POST v2/call/$metadata",
                    "POST v2/call",
                    "POST v2/read/attributes",
                    "POST v2/write/attributes",
                    "POST v2/historyread/first",
                    "POST v2/historyread/next",
                    "POST v2/historyupdate",
                    "POST v2/certificate",
                    "POST v2/history/capabilities",
                    "POST v2/history/configuration",
                    "POST v2/test",

                    "POST v2/history/events/replace",
                    "POST v2/history/events/insert",
                    "POST v2/history/events/upsert",
                    "POST v2/history/events/delete",
                    "POST v2/history/values/delete/attimes",
                    "POST v2/history/values/delete/modified",
                    "POST v2/history/values/delete",
                    "POST v2/history/values/replace",
                    "POST v2/history/values/insert",
                    "POST v2/history/values/upsert",
                    "POST v2/history/events/read/first",
                    "POST v2/history/events/read/next",
                    "POST v2/history/values/read/first",
                    "POST v2/history/values/read/first/attimes",
                    "POST v2/history/values/read/first/processed",
                    "POST v2/history/values/read/first/modified",
                    "POST v2/history/values/read/next",
                    "POST v2/history/values/read",
                    "POST v2/history/values/read/modified",
                    "POST v2/history/values/read/attimes",
                    "POST v2/history/values/read/processed",
                    "POST v2/history/events/read",

                    "PUT v2/writer/",
                    "GET v2/writer/{dataSetWriterGroup}/{dataSetWriterId}",
                    "POST v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/add",
                    "PUT v2/writer/{dataSetWriterGroup}/{dataSetWriterId}",
                    "POST v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/remove",
                    "DELETE v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}",
                    "GET v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}",
                    "GET v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes",
                    "DELETE v2/writer/{dataSetWriterGroup}/{dataSetWriterId}",
                    "POST v2/writer/expand",
                    "POST v2/writer/",
                    "POST v2/writer/assets/create",
                    "POST v2/writer/assets",
                    "POST v2/writer/assets/list",
                    "POST v2/writer/assets/delete"
                };

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }
    }
}
