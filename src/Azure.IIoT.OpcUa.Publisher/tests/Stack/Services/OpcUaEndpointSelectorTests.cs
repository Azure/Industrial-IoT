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
