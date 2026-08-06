// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Core.Logging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using OpcUaClientOptions = Azure.IIoT.OpcUa.Publisher.Stack.OpcUaClientOptions;
    using SecurityMode = Azure.IIoT.OpcUa.Publisher.Models.SecurityMode;

    public sealed class OpcUaEndpointSelectorTests
    {
        [Fact]
        public void SelectEndpointPrefersMatchingPathAndRewritesDiscoveryHost()
        {
            var pathMatch = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal:4840/target",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };
            var higherSecurityLevel = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal:4840/other",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = byte.MaxValue
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [higherSecurityLevel, pathMatch],
                new Uri("opc.tcp://public.example:5000/target"),
                new Uri("opc.tcp://public.example:5000/discovery"),
                reverseConnect: false, SecurityMode.SignAndEncrypt,
                "Basic256Sha256", NullLogger.Instance, "test");

            Assert.Same(pathMatch, selected);
            Assert.Equal("opc.tcp://public.example:5000/target",
                selected!.EndpointUrl);
        }

        [Fact]
        public async Task ClientManagerDelegatesTestConnectionSelectionAsync()
        {
            var application = CreateApplicationConfiguration();
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(application);
            var selector = new CapturingEndpointSelector
            {
                Exception = new InvalidOperationException("selection failed")
            };
            var endpoint = new EndpointModel
            {
                Url = "opc.tcp://localhost:4840"
            };
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()),
                endpointSelector: selector);

            var response = await manager.TestConnectionAsync(new ConnectionModel
            {
                Endpoint = endpoint
            }, new TestConnectionRequestModel(), default);

            Assert.NotNull(response.ErrorInfo);
            Assert.Equal(1, selector.CallCount);
            Assert.Same(application, selector.Configuration);
            Assert.Equal(new Uri(endpoint.Url), selector.DiscoveryUrl);
            Assert.Equal(SecurityMode.NotNone, selector.SecurityMode);
            Assert.Null(selector.SecurityPolicy);
            Assert.Same(endpoint, selector.Context);
        }

        [Fact]
        public async Task ManagedRequestFactoryDelegatesEndpointSelectionAsync()
        {
            var application = CreateApplicationConfiguration();
            var selected = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://selected:4840",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                Server = new ApplicationDescription
                {
                    ApplicationUri = "urn:selected"
                }
            };
            var selector = new CapturingEndpointSelector
            {
                SelectedEndpoint = selected
            };
            var factory = new DefaultManagedSessionRequestFactory(selector);
            using var reverseConnectManager = new ReverseConnectManager(
                DefaultTelemetry.Create(_ => { }));
            var connection = new ConnectionIdentifier(new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                }
            });

            var request = await factory.CreateAsync(new ManagedSessionClientContext
            {
                Configuration = application,
                Connection = connection,
                Logger = NullLogger.Instance,
                Options = Options.Create(new OpcUaClientOptions()),
                ReverseConnectManager = reverseConnectManager,
                TimeProvider = TimeProvider.System
            }, default);

            Assert.Same(selected, request.Endpoint.Description);
            Assert.Equal(1, selector.CallCount);
            Assert.Same(application, selector.Configuration);
            Assert.Equal(new Uri("opc.tcp://localhost:4840"),
                selector.DiscoveryUrl);
            Assert.Equal(SecurityMode.NotNone, selector.SecurityMode);
            Assert.Null(selector.SecurityPolicy);
            Assert.Same(connection, selector.Context);
        }

        // ── Additional SelectEndpoint branch tests ────────────────────────────

        [Fact]
        public void SelectEndpoint_EmptyEndpoints_ReturnsNull()
        {
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [],
                new Uri("opc.tcp://host:4840/path"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.None,
                null, NullLogger.Instance, null);

            Assert.Null(selected);
        }

        [Fact]
        public void SelectEndpoint_NoMatchingSecurityMode_ReturnsNull()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };

            // Requesting None but endpoint uses Sign
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://host:4840/"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.None,
                null, NullLogger.Instance, null);

            Assert.Null(selected);
        }

        [Fact]
        public void SelectEndpoint_SecurityPolicyFilter_FiltersToMatchingPolicy()
        {
            var wrongPolicy = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Aes256_Sha256_RsaPss,
                SecurityLevel = 10
            };
            var rightPolicy = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [wrongPolicy, rightPolicy],
                new Uri("opc.tcp://host:4840/"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.SignAndEncrypt,
                "Basic256Sha256", NullLogger.Instance, null);

            Assert.Same(rightPolicy, selected);
        }

        [Fact]
        public void SelectEndpoint_PicksHighestSecurityLevelWhenPathMatches()
        {
            var lower = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/path",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };
            var higher = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/path",
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 5
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [lower, higher],
                new Uri("opc.tcp://host:4840/path"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.SignAndEncrypt,
                "Basic256Sha256", NullLogger.Instance, null);

            Assert.Same(higher, selected);
        }

        [Fact]
        public void SelectEndpoint_ReverseConnect_WithSchemeMatch_ReturnsWithoutRewrite()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal:4840/",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://internal:4840/"),
                new Uri("opc.tcp://internal:4840/discovery"),
                reverseConnect: true, SecurityMode.None,
                null, NullLogger.Instance, null);

            // Reverse connect returns immediately without host rewrite
            Assert.Same(endpoint, selected);
            Assert.Equal("opc.tcp://internal:4840/", selected!.EndpointUrl);
        }

        [Fact]
        public void SelectEndpoint_ReverseConnect_NoSchemeMatch_ReturnsNull()
        {
            // HTTP endpoint does not match an opc.tcp request
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "http://internal:4840/",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://internal:4840/"),
                new Uri("opc.tcp://internal:4840/discovery"),
                reverseConnect: true, SecurityMode.None,
                null, NullLogger.Instance, null);

            Assert.Null(selected);
        }

        [Fact]
        public void SelectEndpoint_FallbackToPathOnlyMatch_WhenSchemesDiffer()
        {
            // Endpoint uses http but we look by opc.tcp path — path-only fallback
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal:4840/mypath",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };

            // Endpoint path matches but let's ask for a different path — test AnyMatch fallback
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://external:5000/otherpath"),
                new Uri("opc.tcp://external:5000/discovery"),
                reverseConnect: false, SecurityMode.None,
                null, NullLogger.Instance, null);

            // No scheme+path match, no scheme-only match (paths differ), no path-only match,
            // final fallback matches any endpoint → should return the only available endpoint
            Assert.Same(endpoint, selected);
        }

        [Fact]
        public void SelectEndpoint_RewritesDiscoveryHostWhenSchemesMatch()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal-host:4840/server",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };

            // Discovery URL has a different host/port but same scheme
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://internal-host:4840/server"),
                new Uri("opc.tcp://public-host:9999/discovery"),
                reverseConnect: false, SecurityMode.None,
                null, NullLogger.Instance, null);

            Assert.NotNull(selected);
            // Host and port are rewritten to the discovery URL's host:port
            Assert.Contains("public-host", selected!.EndpointUrl,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains("9999", selected.EndpointUrl, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SelectEndpoint_NoRewriteWhenSchemesDiffer()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://internal:4840/",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };

            // Discovery URL uses http scheme — no rewrite should happen
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://internal:4840/"),
                new Uri("http://public.example:8080/discovery"),
                reverseConnect: false, SecurityMode.None,
                null, NullLogger.Instance, null);

            Assert.NotNull(selected);
            // No rewrite: original endpoint URL preserved
            Assert.Equal("opc.tcp://internal:4840/", selected!.EndpointUrl);
        }

        [Fact]
        public void SelectEndpoint_SecurityPolicyCanBeFullUri()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };

            // Provide the full URI as policy
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://host:4840/"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.Sign,
                SecurityPolicies.Basic256Sha256, NullLogger.Instance, null);

            Assert.Same(endpoint, selected);
        }

        [Fact]
        public void SelectEndpoint_NullSecurityPolicy_DoesNotFilterByPolicy()
        {
            var endpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };

            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [endpoint],
                new Uri("opc.tcp://host:4840/"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.Sign,
                null, NullLogger.Instance, null);

            Assert.Same(endpoint, selected);
        }

        [Fact]
        public void SelectEndpoint_NotNone_MatchesNoneMode()
        {
            var noneEndpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
                SecurityLevel = 0
            };
            var signEndpoint = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://host:4840/",
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                SecurityLevel = 1
            };

            // NotNone should accept Sign but not None
            var selected = OpcUaEndpointSelector.SelectEndpoint(
                [noneEndpoint, signEndpoint],
                new Uri("opc.tcp://host:4840/"),
                new Uri("opc.tcp://host:4840/discovery"),
                reverseConnect: false, SecurityMode.NotNone,
                null, NullLogger.Instance, null);

            Assert.Same(signEndpoint, selected);
        }

        private static ApplicationConfiguration CreateApplicationConfiguration()
        {
            return new ApplicationConfiguration
            {
                ApplicationName = "endpoint-selector-tests",
                ApplicationUri = "urn:endpoint-selector-tests",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ClientConfiguration = new ClientConfiguration()
            };
        }

        private sealed class CapturingEndpointSelector : IOpcUaEndpointSelector
        {
            public int CallCount { get; private set; }
            public ApplicationConfiguration? Configuration { get; private set; }
            public Uri? DiscoveryUrl { get; private set; }
            public SecurityMode SecurityMode { get; private set; }
            public string? SecurityPolicy { get; private set; }
            public object? Context { get; private set; }
            public EndpointDescription? SelectedEndpoint { get; init; }
            public Exception? Exception { get; init; }

            public Task<EndpointDescription?> SelectAsync(
                ApplicationConfiguration configuration, Uri? discoveryUrl,
                ITransportWaitingConnection? connection, SecurityMode securityMode,
                string? securityPolicy, ILogger logger, object? context,
                string? endpointUrl = null, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                CallCount++;
                Configuration = configuration;
                DiscoveryUrl = discoveryUrl;
                SecurityMode = securityMode;
                SecurityPolicy = securityPolicy;
                Context = context;
                return Exception == null ?
                    Task.FromResult(SelectedEndpoint) :
                    Task.FromException<EndpointDescription?>(Exception);
            }
        }
    }
}
