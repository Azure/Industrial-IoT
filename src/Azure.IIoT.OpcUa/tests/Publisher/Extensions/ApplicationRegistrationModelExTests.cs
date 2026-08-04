// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public sealed class ApplicationRegistrationModelExTests
    {
        [Fact]
        public void AddOrUpdate_NewApplication_AddsRegistration()
        {
            var discovered = new List<ApplicationRegistrationModel>();
            var server = CreateRegistration("urn:server");

            discovered.AddOrUpdate(server);

            Assert.Same(server, Assert.Single(discovered));
        }

        [Fact]
        public void AddOrUpdate_SameApplication_MergesRegistration()
        {
            var existing = CreateRegistration("urn:server", "opc.tcp://one");
            var discovered = new List<ApplicationRegistrationModel> { existing };
            var server = CreateRegistration("URN:SERVER", "opc.tcp://two");
            server.Application.Capabilities = new HashSet<string> { "DA" };
            server.Application.DiscoveryUrls = new HashSet<string> { "opc.tcp://discovery" };
            server.Application.HostAddresses = new HashSet<string> { "host" };

            discovered.AddOrUpdate(server);

            Assert.Same(existing, Assert.Single(discovered));
            Assert.Contains("DA", existing.Application.Capabilities!);
            Assert.Contains("opc.tcp://discovery", existing.Application.DiscoveryUrls!);
            Assert.Contains("host", existing.Application.HostAddresses!);
            Assert.Equal(2, existing.Endpoints?.Count);
        }

        [Fact]
        public void UnionWith_NullApplication_UsesServerApplication()
        {
            var application = CreateApplication("urn:server");
            var model = new ApplicationRegistrationModel
            {
                Application = null!
            };
            var server = new ApplicationRegistrationModel
            {
                Application = application
            };

            model.UnionWith(server);

            Assert.Same(application, model.Application);
        }

        [Fact]
        public void UnionWith_NullServer_DoesNotChangeEndpoints()
        {
            var model = CreateRegistration("urn:server", "opc.tcp://one");
            var originalEndpoints = model.Endpoints;

            model.UnionWith(null!);

            Assert.Same(originalEndpoints, model.Endpoints);
        }

        [Fact]
        public void UnionWith_ServerWithoutEndpoints_OnlyMergesApplicationSets()
        {
            var model = CreateRegistration("urn:server");
            var server = CreateRegistration("urn:server");
            server.Application.Capabilities = new HashSet<string> { "DA" };
            server.Endpoints = null;

            model.UnionWith(server);

            Assert.Contains("DA", model.Application.Capabilities!);
            Assert.Null(model.Endpoints);
        }

        [Fact]
        public void UnionWith_ModelWithoutEndpoints_UsesServerEndpoints()
        {
            var model = CreateRegistration("urn:server");
            var server = CreateRegistration("urn:server", "opc.tcp://one");

            model.UnionWith(server);

            Assert.Same(server.Endpoints, model.Endpoints);
        }

        [Fact]
        public void UnionWith_NewEndpoint_AppendsEndpoint()
        {
            var model = CreateRegistration("urn:server", "opc.tcp://one");
            var server = CreateRegistration("urn:server", "opc.tcp://two");

            model.UnionWith(server);

            Assert.Equal(2, model.Endpoints?.Count);
            Assert.Contains(model.Endpoints!, endpoint => endpoint.EndpointUrl == "opc.tcp://two");
        }

        [Fact]
        public void UnionWithAppendsWhenExistingEntryEndpointIsNull()
        {
            var model = CreateRegistration("urn:server", "opc.tcp://one");
            var existing = model.Endpoints!.Single();
            existing.Endpoint = null;
            var server = CreateRegistration("urn:server", "opc.tcp://one");

            model.UnionWith(server);

            Assert.Null(existing.Endpoint);
            Assert.Equal(2, model.Endpoints!.Count);
        }

        [Fact]
        public void UnionWith_MatchingEndpoint_MergesEndpointUrls()
        {
            var model = CreateRegistration("urn:server", "opc.tcp://one");
            var server = CreateRegistration("urn:server", "opc.tcp://one");
            server.Endpoints!.Single().Endpoint!.AlternativeUrls =
                new HashSet<string> { "opc.tcp://alternate" };

            model.UnionWith(server);

            var endpoint = model.Endpoints!.Single().Endpoint!;
            Assert.Contains("opc.tcp://alternate", endpoint.AlternativeUrls!);
        }

        private static ApplicationRegistrationModel CreateRegistration(
            string applicationUri, string? endpointUrl = null)
        {
            return new ApplicationRegistrationModel
            {
                Application = CreateApplication(applicationUri),
                Endpoints = endpointUrl == null ? null :
                [
                    new EndpointRegistrationModel
                    {
                        Id = endpointUrl,
                        EndpointUrl = endpointUrl,
                        Endpoint = new EndpointModel
                        {
                            Url = endpointUrl,
                            SecurityMode = SecurityMode.None,
                            SecurityPolicy = "policy"
                        },
                        AuthenticationMethods =
                        [
                            new AuthenticationMethodModel
                            {
                                Id = "anonymous",
                                CredentialType = CredentialType.None,
                                SecurityPolicy = "policy"
                            }
                        ]
                    }
                ]
            };
        }

        private static ApplicationInfoModel CreateApplication(string applicationUri)
        {
            return new ApplicationInfoModel
            {
                ApplicationId = applicationUri,
                ApplicationUri = applicationUri,
                ApplicationType = ApplicationType.Server
            };
        }
    }
}
