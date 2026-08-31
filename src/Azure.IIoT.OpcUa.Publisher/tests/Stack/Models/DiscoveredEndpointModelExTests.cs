// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DiscoveredEndpointModelEx.ToServiceModel"/>.
    /// </summary>
    public sealed class DiscoveredEndpointModelExTests
    {
        [Fact]
        public void ToServiceModel_BasicEndpoint_SetsApplicationInfo()
        {
            var endpoint = CreateEndpoint("urn:server1", "opc.tcp://127.0.0.1:4840");

            var result = endpoint.ToServiceModel("127.0.0.1", null, "discoverer1");

            Assert.NotNull(result.Application);
            Assert.Equal("urn:server1", result.Application.ApplicationUri);
            Assert.Equal("discoverer1", result.Application.SiteId);
            Assert.Equal("discoverer1", result.Application.DiscovererId);
        }

        [Fact]
        public void ToServiceModel_WithSiteId_UsesSiteIdForSiteAndDiscoverer()
        {
            var endpoint = CreateEndpoint("urn:server2", "opc.tcp://10.0.0.1:4840");

            var result = endpoint.ToServiceModel("10.0.0.1", "site42", "discoverer1");

            Assert.Equal("site42", result.Application.SiteId);
            Assert.Equal("discoverer1", result.Application.DiscovererId);
        }

        [Fact]
        public void ToServiceModel_BasicEndpoint_SetsEndpointRegistration()
        {
            const string url = "opc.tcp://127.0.0.1:4840";
            var endpoint = CreateEndpoint("urn:server1", url);

            var result = endpoint.ToServiceModel("127.0.0.1", null, "discoverer1");

            Assert.NotNull(result.Endpoints);
            var reg = Assert.Single(result.Endpoints);
            Assert.Equal(url, reg.Endpoint!.Url);
        }

        [Fact]
        public void ToServiceModel_WithCapabilities_SetsCapabilities()
        {
            var endpoint = CreateEndpoint("urn:server1", "opc.tcp://127.0.0.1:4840");

            var result = endpoint.ToServiceModel("127.0.0.1", null, "d1");

            Assert.Contains("LDS", result.Application.Capabilities);
        }

        [Fact]
        public void ToServiceModel_ServerApplicationType_MapsToServerType()
        {
            var endpoint = CreateEndpoint("urn:server1", "opc.tcp://127.0.0.1:4840");

            var result = endpoint.ToServiceModel("127.0.0.1", null, "d1");

            Assert.Equal(Publisher.Models.ApplicationType.Server, result.Application.ApplicationType);
        }

        [Fact]
        public void ToServiceModel_AnonymousToken_MapsToNoneCredential()
        {
            var endpoint = CreateEndpoint("urn:server1", "opc.tcp://127.0.0.1:4840");

            var result = endpoint.ToServiceModel("127.0.0.1", null, "d1");

            var reg = Assert.Single(result.Endpoints!);
            Assert.NotNull(reg.AuthenticationMethods);
            Assert.Contains(reg.AuthenticationMethods, m => m.CredentialType == CredentialType.None);
        }

        private static DiscoveredEndpointModel CreateEndpoint(string applicationUri, string endpointUrl)
        {
            return new DiscoveredEndpointModel
            {
                AccessibleEndpointUrl = endpointUrl,
                Capabilities = ["LDS"],
                Description = new EndpointDescription
                {
                    EndpointUrl = endpointUrl,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None,
                    SecurityLevel = 0,
                    Server = new ApplicationDescription
                    {
                        ApplicationUri = applicationUri,
                        ApplicationName = (LocalizedText)"Server",
                        ApplicationType = Opc.Ua.ApplicationType.Server,
                        ProductUri = "urn:product",
                        DiscoveryUrls = [endpointUrl]
                    },
                    UserIdentityTokens =
                    [
                        new UserTokenPolicy
                        {
                            TokenType = UserTokenType.Anonymous
                        }
                    ]
                }
            };
        }
    }
}
