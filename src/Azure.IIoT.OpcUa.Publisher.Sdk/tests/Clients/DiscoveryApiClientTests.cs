// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// The discovery client's contract with the module's method router: the
    /// method names it calls and the payloads it sends.
    /// </summary>
    /// <remarks>
    /// The method names carry a _V2 suffix that nothing in the client's own
    /// signature mentions. Getting one wrong is not a compile error and not a
    /// local failure - it is a call the module rejects at run time, which is
    /// why each one is named here explicitly.
    /// </remarks>
    public sealed class DiscoveryApiClientTests : ApiClientTestBase
    {
        private DiscoveryApiClient Client => new(MethodClient.Object, Target);

        [Fact]
        public async Task GetEndpointCertificateCallsGetEndpointCertificateV2Async()
        {
            var expected = new X509CertificateChainModel();
            Returns(expected);

            var result = await Client.GetEndpointCertificateAsync(
                new EndpointModel { Url = "opc.tcp://server:4840" }, default);

            var payload = AssertCalled("GetEndpointCertificate_V2");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "url"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CancelCallsCancelV2Async()
        {
            await Client.CancelAsync(new DiscoveryCancelRequestModel { Id = "job" }, default);

            var payload = AssertCalled("Cancel_V2");
            Assert.Equal("job", StringOf(payload, "id"));
        }

        [Fact]
        public async Task DiscoverCallsDiscoverV2Async()
        {
            await Client.DiscoverAsync(
                new DiscoveryRequestModel { Id = "req", Discovery = DiscoveryMode.Fast }, default);

            var payload = AssertCalled("Discover_V2");
            Assert.Equal("req", StringOf(payload, "id"));
        }

        [Fact]
        public async Task RegisterCallsRegisterV2Async()
        {
            await Client.RegisterAsync(
                new ServerRegistrationRequestModel
                {
                    DiscoveryUrl = "opc.tcp://server:4840"
                }, default);

            var payload = AssertCalled("Register_V2");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "discoveryUrl"));
        }

        [Fact]
        public async Task FindServerCallsFindServerV2Async()
        {
            Returns(new ApplicationRegistrationModel
            {
                Application = new ApplicationInfoModel
                {
                    ApplicationId = "app",
                    ApplicationUri = "urn:server"
                }
            });

            var result = await Client.FindServerAsync(
                new ServerEndpointQueryModel { Url = "opc.tcp://server:4840" }, default);

            var payload = AssertCalled("FindServer_V2");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "url"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ARejectedArgumentNeverReachesTheTransportAsync()
        {
            //
            // Validation has to happen before the call, not after: a null
            // request that travelled would be a round trip and a remote error
            // for something knowable locally.
            //
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => Client.DiscoverAsync(null!, default));

            AssertNotCalled();
        }

        [Fact]
        public async Task AResponseThatDeserializesToNullIsAnErrorAsync()
        {
            //
            // The transport can only say the call succeeded. A body of "null"
            // means the module answered with nothing where the signature
            // promises a value, so the client must not hand back a null that
            // the caller's non-nullable return type says cannot happen.
            //
            ReturnsRaw("null");

            await Assert.ThrowsAsync<MethodCallException>(
                () => Client.FindServerAsync(new ServerEndpointQueryModel(), default));
        }

        [Fact]
        public void TheClientRequiresATransportAndATarget()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DiscoveryApiClient(null!, Target));
            Assert.Throws<ArgumentNullException>(
                () => new DiscoveryApiClient(MethodClient.Object, (string)null!));
            Assert.Throws<ArgumentNullException>(
                () => new DiscoveryApiClient(MethodClient.Object, string.Empty));
        }

        [Fact]
        public async Task TheOptionsConstructorUsesTheConfiguredTargetAndTimeoutAsync()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var client = new DiscoveryApiClient(MethodClient.Object,
                Options.Create(new SdkOptions { Target = Target, Timeout = timeout }));

            await client.CancelAsync(new DiscoveryCancelRequestModel(), default);

            AssertCalled("Cancel_V2");
            Assert.Equal(timeout, LastCall!.Timeout);
        }

        [Fact]
        public async Task TheCancellationTokenReachesTheTransportAsync()
        {
            using var cts = new CancellationTokenSource();

            await Client.CancelAsync(new DiscoveryCancelRequestModel(), cts.Token);

            Assert.Equal(cts.Token, LastCall!.Ct);
        }
    }
}
